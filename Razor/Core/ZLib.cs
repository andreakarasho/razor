using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Assistant
{
    public enum ZLibError
    {
        Z_OK = 0,
        Z_STREAM_END = 1,
        Z_NEED_DICT = 2,
        Z_ERRNO = -1,
        Z_STREAM_ERROR = -2,
        Z_DATA_ERROR = -3, // Data was corrupt
        Z_MEM_ERROR = -4, //  Not Enough Memory
        Z_BUF_ERROR = -5, // Not enough buffer space
        Z_VERSION_ERROR = -6
    }

    [Flags]
    public enum ZLibCompressionLevel
    {
        Z_NO_COMPRESSION = 0,
        Z_BEST_SPEED = 1,
        Z_BEST_COMPRESSION = 9,
        Z_DEFAULT_COMPRESSION = -1
    }

    public class ZLib
    {
        [DllImport("zlib")]
        internal static extern string zlibVersion();

        [DllImport("zlib")]
        internal static extern ZLibError compress(byte[] dest, ref int destLength, byte[] source, int sourceLength);

        [DllImport("zlib")]
        internal static extern ZLibError compress2(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibCompressionLevel level);

        [DllImport("zlib")]
        internal static extern ZLibError uncompress(byte[] dest, ref int destLen, byte[] source, int sourceLen);
    }

    // Be careful when writing raw data, as it may confuse the GZBlockIn if not accounted for when reading.
    // Seeking in the compressed stream is HIGHLY unrecommended
    // If you need to seek, use BufferAll to keep all data in the buffer, seek as much as you want, then 
    // turn off BufferAll and flush the data to disk.
    // Once the data is flushed, you CANNOT seek back to it!
    public class GZBlockOut : Stream
    {
        private static byte[] m_CompBuff;
        private bool m_IsCompressed;

        public GZBlockOut(string filename, int blockSize)
        {
            m_IsCompressed = true;

            Raw = new BinaryWriter(new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
            BlockSize = blockSize;
            Buffer = new MemoryStream(blockSize + 1024);

            Compressed = new BinaryWriter(this);
        }

        public override bool CanSeek => false;
        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override long Length => RawStream.Length;

        public override long Position
        {
            get => m_IsCompressed ? Buffer.Position : RawStream.Position;
            set { }
        }

        public Stream RawStream => Raw.BaseStream;
        public BinaryWriter Raw { get; }
        public BinaryWriter Compressed { get; private set; }
        public MemoryStream Buffer { get; }
        public int BlockSize { get; set; }

        public bool BufferAll { get; set; }

        public bool IsCompressed
        {
            get => m_IsCompressed;
            set
            {
                ForceFlush();
                m_IsCompressed = value;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (m_IsCompressed)
            {
                Buffer.Write(buffer, offset, count);

                if (Buffer.Position >= BlockSize)
                    FlushBuffer();
            }
            else
                RawStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (m_IsCompressed)
            {
                Buffer.WriteByte(value);

                if (Buffer.Position >= BlockSize)
                    FlushBuffer();
            }
            else
                RawStream.WriteByte(value);
        }

        public void FlushBuffer()
        {
            if (!m_IsCompressed || BufferAll || Buffer.Position <= 0)
                return;

            int outLen = (int) (Buffer.Position * 1.1);

            if (m_CompBuff == null || m_CompBuff.Length < outLen)
                m_CompBuff = new byte[outLen];
            else
                outLen = m_CompBuff.Length;

            ZLibError error = ZLib.compress2(m_CompBuff, ref outLen, Buffer.ToArray(), (int) Buffer.Position, ZLibCompressionLevel.Z_BEST_COMPRESSION);

            if (error != ZLibError.Z_OK)
                throw new Exception("ZLib error during copression: " + error);

            Raw.Write(outLen);
            Raw.Write((int) Buffer.Position);
            Raw.Write(m_CompBuff, 0, outLen);

            Buffer.Position = 0;
        }

        public override void Flush()
        {
            FlushBuffer();
            RawStream.Flush();
        }

        public void ForceFlush()
        {
            bool old = BufferAll;
            BufferAll = false;
            Flush();
            BufferAll = old;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (m_IsCompressed)
                return Buffer.Seek(offset, origin);

            return RawStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            RawStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override void Close()
        {
            ForceFlush();

            base.Close();
            Raw.Close();
            Buffer.Close();
            Compressed = null;
        }
    }

    // Represents a block compressed stream written by GZBlockOut
    // If there is uncompressed data in the stream, you may seek to 
    // it and read from is as you wish using Raw/RawStream.  If you have
    // not yet started reading compressed data, you must position rawstream 
    // at the begining of the compressed data.  If you've already read 
    // compressed data, you must reposition the file pointer back to its previous
    // position in the stream.  This is really important.
    //
    // Seeking in the compressed stream should be okay, DO NOT attempt to seek outside
    // of the compressed data.
    public class GZBlockIn : Stream
    {
        private static byte[] m_ReadBuff;
        private static byte[] m_CompBuff;
        private BinaryReader m_Self;
        private readonly MemoryStream m_Uncomp;

        public GZBlockIn(string filename)
        {
            IsCompressed = true;

            Raw = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
            m_Uncomp = new MemoryStream();
            m_Self = new BinaryReader(this);
        }

        public Stream RawStream => Raw.BaseStream;
        public BinaryReader Raw { get; }
        public BinaryReader Compressed => IsCompressed ? m_Self : Raw;
        public bool IsCompressed { get; set; }

        public override bool CanSeek => true;
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override long Length => IsCompressed ? RawStream.Position < RawStream.Length ? int.MaxValue : m_Uncomp.Length : RawStream.Length;

        public override long Position
        {
            get => IsCompressed ? m_Uncomp.Position : RawStream.Position;
            set
            {
                if (IsCompressed) m_Uncomp.Position = value;
                else RawStream.Position = value;
            }
        }

        public bool EndOfFile => (!IsCompressed || m_Uncomp.Position >= m_Uncomp.Length) && RawStream.Position >= RawStream.Length;

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override void Flush()
        {
            RawStream.Flush();
            m_Uncomp.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (IsCompressed)
            {
                long absPos = offset;

                if (origin == SeekOrigin.Current)
                    absPos += m_Uncomp.Position;

                if (absPos < 0)
                    throw new Exception("Cannot seek past the begining of the stream.");

                long pos = m_Uncomp.Position;
                m_Uncomp.Seek(0, SeekOrigin.End);

                while ((origin == SeekOrigin.End || absPos >= m_Uncomp.Length) && RawStream.Position < RawStream.Length)
                {
                    int block = Raw.ReadInt32();
                    int ucLen = Raw.ReadInt32();

                    if (m_ReadBuff == null || m_ReadBuff.Length < block)
                        m_ReadBuff = new byte[block];

                    if (m_CompBuff == null || m_CompBuff.Length < ucLen)
                        m_CompBuff = new byte[ucLen];
                    else
                        ucLen = m_CompBuff.Length;

                    Raw.Read(m_ReadBuff, 0, block);

                    ZLibError error = ZLib.uncompress(m_CompBuff, ref ucLen, m_ReadBuff, block);

                    if (error != ZLibError.Z_OK)
                        throw new Exception("ZLib error uncompressing: " + error);

                    m_Uncomp.Write(m_CompBuff, 0, ucLen);
                }

                m_Uncomp.Position = pos;

                return m_Uncomp.Seek(offset, origin);
            }

            return RawStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (IsCompressed)
            {
                long pos = m_Uncomp.Position;
                m_Uncomp.Seek(0, SeekOrigin.End);

                while (pos + count > m_Uncomp.Length && RawStream.Position + 8 < RawStream.Length)
                {
                    int block = Raw.ReadInt32();
                    int ucLen = Raw.ReadInt32();

                    if (block > 0x10000000 || block <= 0 || ucLen > 0x10000000 || ucLen <= 0)
                        break;

                    if (RawStream.Position + block > RawStream.Length)
                        break;

                    if (m_ReadBuff == null || m_ReadBuff.Length < block)
                        m_ReadBuff = new byte[block];

                    if (m_CompBuff == null || m_CompBuff.Length < ucLen)
                        m_CompBuff = new byte[ucLen];
                    else
                        ucLen = m_CompBuff.Length;

                    Raw.Read(m_ReadBuff, 0, block);

                    ZLibError error = ZLib.uncompress(m_CompBuff, ref ucLen, m_ReadBuff, block);

                    if (error != ZLibError.Z_OK)
                        throw new Exception("ZLib error uncompressing: " + error);

                    m_Uncomp.Write(m_CompBuff, 0, ucLen);
                }

                m_Uncomp.Position = pos;

                return m_Uncomp.Read(buffer, offset, count);
            }

            return RawStream.Read(buffer, offset, count);
        }

        public override void Close()
        {
            Raw.Close();
            m_Uncomp.Close();
            m_Self = null;
        }
    }
}