using System;
using System.IO;
using System.Text;

namespace Assistant
{
    public enum PacketPath
    {
        ClientToServer,
        RazorToServer,
        ServerToClient,
        RazorToClient,

        PacketVideo
    }

    public class Packet
    {
        private static bool m_Logging;

        private static readonly byte[] m_Buffer = new byte[4]; // Internal format buffer.
        private bool m_DynSize;
        private byte m_PacketID;

        public Packet()
        {
            UnderlyingStream = new MemoryStream();
        }

        public Packet(byte packetID)
        {
            m_PacketID = packetID;
            m_DynSize = true;
        }

        public Packet(byte packetID, int capacity)
        {
            UnderlyingStream = new MemoryStream(capacity);

            m_PacketID = packetID;
            m_DynSize = false;

            UnderlyingStream.WriteByte(packetID);
        }

        public Packet(byte[] data, int len, bool dynLen)
        {
            UnderlyingStream = new MemoryStream(len);
            m_PacketID = data[0];
            m_DynSize = dynLen;

            UnderlyingStream.Position = 0;
            UnderlyingStream.Write(data, 0, len);

            MoveToData();
        }

        public static bool Logging
        {
            get => m_Logging;
            set
            {
                if (value != m_Logging)
                {
                    m_Logging = value;

                    if (m_Logging)
                        BeginLog();
                }
            }
        }

        public static string PacketsLogFile => Path.Combine(Config.GetInstallDirectory(), "Razor_Packets.log");

        public int PacketID => m_PacketID;

        public long Length => UnderlyingStream.Length;

        public long Position
        {
            get => UnderlyingStream.Position;
            set => UnderlyingStream.Position = value;
        }

        public MemoryStream UnderlyingStream { get; private set; }

        private static void BeginLog()
        {
            using (StreamWriter sw = new StreamWriter(PacketsLogFile, true))
            {
                sw.AutoFlush = true;
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine(">>>>>>>>>> Logging started {0} <<<<<<<<<<", DateTime.UtcNow);
                sw.WriteLine();
                sw.WriteLine();
            }
        }

        public void EnsureCapacity(int capacity)
        {
            UnderlyingStream = new MemoryStream(capacity);
            Write(m_PacketID);

            if (m_DynSize)
                Write((short) 0);
        }

        public byte[] Compile()
        {
            if (m_DynSize)
            {
                UnderlyingStream.Seek(1, SeekOrigin.Begin);
                Write((ushort) UnderlyingStream.Length);
            }

            return ToArray();
        }

        public void MoveToData()
        {
            UnderlyingStream.Position = m_DynSize ? 3 : 1;
        }

        public void Copy(Packet p)
        {
            UnderlyingStream = new MemoryStream((int) p.Length);
            byte[] data = p.ToArray();
            UnderlyingStream.Write(data, 0, data.Length);

            m_DynSize = p.m_DynSize;
            m_PacketID = p.m_PacketID;

            MoveToData();
        }

        /*public override int GetHashCode()
        {
            long oldPos = m_Stream.Position;

            int code = 0;

            m_Stream.Position = 0;

            while ( m_Stream.Length - m_Stream.Position >= 4 )
                code ^= ReadInt32();
            
            code ^= ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24);

            m_Stream.Position = oldPos;

            return code;
        }*/

        public static void Log(string line, params object[] args)
        {
            Log(string.Format(line, args));
        }

        public static void Log(string line)
        {
            if (!m_Logging)
                return;

            try
            {
                using (StreamWriter sw = new StreamWriter(PacketsLogFile, true))
                {
                    sw.AutoFlush = true;
                    sw.WriteLine(line);
                    sw.WriteLine();
                }
            }
            catch
            {
            }
        }

        public static unsafe void Log(PacketPath path, byte* buff, int len)
        {
            Log(path, buff, len, false);
        }

        public static unsafe void Log(PacketPath path, byte* buff, int len, bool blocked)
        {
            if (!m_Logging)
                return;

            try
            {
                using (StreamWriter sw = new StreamWriter(PacketsLogFile, true))
                {
                    sw.AutoFlush = true;

                    string pathStr;

                    switch (path)
                    {
                        case PacketPath.ClientToServer:
                            pathStr = "Client -> Server";

                            break;
                        case PacketPath.RazorToServer:
                            pathStr = "Razor -> Server";

                            break;
                        case PacketPath.ServerToClient:
                            pathStr = "Server -> Client";

                            break;
                        case PacketPath.RazorToClient:
                            pathStr = "Razor -> Client";

                            break;
                        case PacketPath.PacketVideo:
                            pathStr = "PacketVideo -> Client";

                            break;
                        default:
                            pathStr = "Unknown -> Unknown";

                            break;
                    }

                    sw.WriteLine("{0}: {1}{2}0x{3:X2} (Length: {4})", Engine.MistedDateTime.ToString("HH:mm:ss.ffff"), pathStr, blocked ? " [BLOCKED] " : " ", buff[0], len);
                    //if ( buff[0] != 0x80 && buff[0] != 0x91 )
                    Utility.FormatBuffer(sw, buff, len);
                    //else
                    //	sw.WriteLine( "[Censored for Security Reasons]" );

                    sw.WriteLine();
                    sw.WriteLine();
                }
            }
            catch
            {
            }
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return UnderlyingStream.Seek(offset, origin);
        }

        public int ReadInt32()
        {
            if (UnderlyingStream.Position + 4 > UnderlyingStream.Length)
                return 0;

            return (ReadByte() << 24)
                   | (ReadByte() << 16)
                   | (ReadByte() << 8)
                   | ReadByte();
        }

        public short ReadInt16()
        {
            if (UnderlyingStream.Position + 2 > UnderlyingStream.Length)
                return 0;

            return (short) ((ReadByte() << 8) | ReadByte());
        }

        public byte ReadByte()
        {
            if (UnderlyingStream.Position + 1 > UnderlyingStream.Length)
                return 0;

            return (byte) UnderlyingStream.ReadByte();
        }

        public uint ReadUInt32()
        {
            if (UnderlyingStream.Position + 4 > UnderlyingStream.Length)
                return 0;

            return (uint) ((ReadByte() << 24)
                           | (ReadByte() << 16)
                           | (ReadByte() << 8)
                           | ReadByte());
        }

        public ushort ReadUInt16()
        {
            if (UnderlyingStream.Position + 2 > UnderlyingStream.Length)
                return 0;

            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        public sbyte ReadSByte()
        {
            if (UnderlyingStream.Position + 1 > UnderlyingStream.Length)
                return 0;

            return (sbyte) UnderlyingStream.ReadByte();
        }

        public bool ReadBoolean()
        {
            if (UnderlyingStream.Position + 1 > UnderlyingStream.Length)
                return false;

            return UnderlyingStream.ReadByte() != 0;
        }

        public string ReadUnicodeStringLE()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position + 1 < UnderlyingStream.Length && (c = ReadByte() | (ReadByte() << 8)) != 0)
                sb.Append((char) c);

            return sb.ToString();
        }

        public string ReadUnicodeStringLESafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position + 1 < UnderlyingStream.Length && (c = ReadByte() | (ReadByte() << 8)) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char) c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position + 1 < UnderlyingStream.Length && (c = ReadUInt16()) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char) c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position + 1 < UnderlyingStream.Length && (c = ReadUInt16()) != 0)
                sb.Append((char) c);

            return sb.ToString();
        }

        public bool IsSafeChar(int c)
        {
            return c >= 0x20 && c < 0xFFFE;
        }

        public string ReadUTF8StringSafe(int fixedLength)
        {
            if (UnderlyingStream.Position >= UnderlyingStream.Length)
                return string.Empty;

            long bound = UnderlyingStream.Position + fixedLength;
            long end = bound;

            if (bound > UnderlyingStream.Length)
                bound = UnderlyingStream.Length;

            int count = 0;
            long index = UnderlyingStream.Position;
            long start = UnderlyingStream.Position;

            while (index < bound && ReadByte() != 0)
                ++count;

            UnderlyingStream.Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (UnderlyingStream.Position < bound && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            string s = Encoding.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = IsSafeChar(s[i]);

            UnderlyingStream.Seek(start + fixedLength, SeekOrigin.Begin);

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                if (IsSafeChar(s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public string ReadUTF8StringSafe()
        {
            if (UnderlyingStream.Position >= UnderlyingStream.Length)
                return string.Empty;

            int count = 0;
            long index = UnderlyingStream.Position;
            long start = index;

            while (index < UnderlyingStream.Length && ReadByte() != 0)
                ++count;

            UnderlyingStream.Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (UnderlyingStream.Position < UnderlyingStream.Length && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            string s = Encoding.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = IsSafeChar(s[i]);

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                if (IsSafeChar(s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public string ReadUTF8String()
        {
            if (UnderlyingStream.Position >= UnderlyingStream.Length)
                return string.Empty;

            int count = 0;
            long index = UnderlyingStream.Position;
            long start = index;

            while (index < UnderlyingStream.Length && ReadByte() != 0)
                ++count;

            UnderlyingStream.Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (UnderlyingStream.Position < UnderlyingStream.Length && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            return Encoding.UTF8.GetString(buffer);
        }

        public string ReadString()
        {
            return ReadStringSafe();
        }

        public string ReadStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position < UnderlyingStream.Length && (c = ReadByte()) != 0)
                sb.Append((char) c);

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe(int fixedLength)
        {
            return ReadUnicodeString(fixedLength);
        }

        public string ReadUnicodeString(int fixedLength)
        {
            long bound = UnderlyingStream.Position + (fixedLength << 1);
            long end = bound;

            if (bound > UnderlyingStream.Length)
                bound = UnderlyingStream.Length;

            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position + 1 < bound && (c = ReadUInt16()) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char) c);
            }

            UnderlyingStream.Seek(end, SeekOrigin.Begin);

            return sb.ToString();
        }

        public string ReadStringSafe(int fixedLength)
        {
            return ReadString(fixedLength);
        }

        public string ReadString(int fixedLength)
        {
            long bound = UnderlyingStream.Position + fixedLength;

            if (bound > UnderlyingStream.Length)
                bound = UnderlyingStream.Length;

            long end = bound;

            StringBuilder sb = new StringBuilder();

            int c;

            while (UnderlyingStream.Position < bound && (c = ReadByte()) != 0)
                sb.Append((char) c);

            UnderlyingStream.Seek(end, SeekOrigin.Begin);

            return sb.ToString();
        }



        /////////////////////////////////////////////
        ///Packet Writer/////////////////////////////
        /////////////////////////////////////////////
        public void Write(bool value)
        {
            UnderlyingStream.WriteByte((byte) (value ? 1 : 0));
        }

        public void Write(byte value)
        {
            UnderlyingStream.WriteByte(value);
        }

        public void Write(sbyte value)
        {
            UnderlyingStream.WriteByte((byte) value);
        }

        public void Write(short value)
        {
            m_Buffer[0] = (byte) (value >> 8);
            m_Buffer[1] = (byte) value;

            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        public void Write(ushort value)
        {
            m_Buffer[0] = (byte) (value >> 8);
            m_Buffer[1] = (byte) value;

            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        public void Write(int value)
        {
            m_Buffer[0] = (byte) (value >> 24);
            m_Buffer[1] = (byte) (value >> 16);
            m_Buffer[2] = (byte) (value >> 8);
            m_Buffer[3] = (byte) value;

            UnderlyingStream.Write(m_Buffer, 0, 4);
        }

        public void Write(uint value)
        {
            m_Buffer[0] = (byte) (value >> 24);
            m_Buffer[1] = (byte) (value >> 16);
            m_Buffer[2] = (byte) (value >> 8);
            m_Buffer[3] = (byte) value;

            UnderlyingStream.Write(m_Buffer, 0, 4);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            UnderlyingStream.Write(buffer, offset, size);
        }

        public void WriteAsciiFixed(string value, int size)
        {
            if (value == null)
                value = string.Empty;

            byte[] buffer = Encoding.ASCII.GetBytes(value);

            if (buffer.Length >= size)
                UnderlyingStream.Write(buffer, 0, size);
            else
            {
                UnderlyingStream.Write(buffer, 0, buffer.Length);

                byte[] pad = new byte[size - buffer.Length];

                UnderlyingStream.Write(pad, 0, pad.Length);
            }
        }

        public void WriteAsciiNull(string value)
        {
            if (value == null)
                value = string.Empty;

            byte[] buffer = Encoding.ASCII.GetBytes(value);

            UnderlyingStream.Write(buffer, 0, buffer.Length);
            UnderlyingStream.WriteByte(0);
        }

        public void WriteLittleUniNull(string value)
        {
            if (value == null)
                value = string.Empty;

            byte[] buffer = Encoding.Unicode.GetBytes(value);

            UnderlyingStream.Write(buffer, 0, buffer.Length);

            m_Buffer[0] = 0;
            m_Buffer[1] = 0;
            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        public void WriteLittleUniFixed(string value, int size)
        {
            if (value == null)
                value = string.Empty;

            size *= 2;

            byte[] buffer = Encoding.Unicode.GetBytes(value);

            if (buffer.Length >= size)
                UnderlyingStream.Write(buffer, 0, size);
            else
            {
                UnderlyingStream.Write(buffer, 0, buffer.Length);

                byte[] pad = new byte[size - buffer.Length];

                UnderlyingStream.Write(pad, 0, pad.Length);
            }
        }

        public void WriteBigUniNull(string value)
        {
            if (value == null)
                value = string.Empty;

            byte[] buffer = Encoding.BigEndianUnicode.GetBytes(value);

            UnderlyingStream.Write(buffer, 0, buffer.Length);

            m_Buffer[0] = 0;
            m_Buffer[1] = 0;
            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        public void WriteBigUniFixed(string value, int size)
        {
            if (value == null)
                value = string.Empty;

            size *= 2;

            byte[] buffer = Encoding.BigEndianUnicode.GetBytes(value);

            if (buffer.Length >= size)
                UnderlyingStream.Write(buffer, 0, size);
            else
            {
                UnderlyingStream.Write(buffer, 0, buffer.Length);

                byte[] pad = new byte[size - buffer.Length];

                UnderlyingStream.Write(pad, 0, pad.Length);
            }
        }

        public void WriteUTF8Fixed(string value, int size)
        {
            if (value == null)
                value = string.Empty;

            size *= 2;

            byte[] buffer = Encoding.UTF8.GetBytes(value);

            if (buffer.Length >= size)
                UnderlyingStream.Write(buffer, 0, size);
            else
            {
                UnderlyingStream.Write(buffer, 0, buffer.Length);

                byte[] pad = new byte[size - buffer.Length];

                UnderlyingStream.Write(pad, 0, pad.Length);
            }
        }

        public void WriteUTF8Null(string value)
        {
            if (value == null)
                value = string.Empty;

            byte[] buffer = Encoding.UTF8.GetBytes(value);

            UnderlyingStream.Write(buffer, 0, buffer.Length);
            m_Buffer[0] = 0;
            m_Buffer[1] = 0;
            UnderlyingStream.Write(m_Buffer, 0, 2);
        }

        public void Fill()
        {
            byte[] buffer = new byte[UnderlyingStream.Capacity - Position];
            UnderlyingStream.Write(buffer, 0, buffer.Length);
        }

        public void Fill(int length)
        {
            UnderlyingStream.Write(new byte[length], 0, length);
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return UnderlyingStream.Seek(offset, origin);
        }

        public byte[] ToArray()
        {
            return UnderlyingStream.ToArray();
        }
    }

    public sealed unsafe class PacketReader
    {
        private readonly byte* m_Data;

        public PacketReader(byte* buff, int len, bool dyn)
        {
            m_Data = buff;
            Length = len;
            Position = 0;
            DynamicLength = dyn;
        }

        public PacketReader(byte[] buff, bool dyn)
        {
            fixed (byte* p = buff)
                m_Data = p;
            Length = buff.Length;
            Position = 0;
            DynamicLength = dyn;
        }

        public int Length { get; }
        public bool DynamicLength { get; }

        public byte PacketID => *m_Data;
        public int Position { get; set; }

        public bool AtEnd => Position >= Length;

        public void MoveToData()
        {
            Position = DynamicLength ? 3 : 1;
        }

        public int Seek(int offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.End:
                    Position = Length - offset;

                    break;
                case SeekOrigin.Current:
                    Position += offset;

                    break;
                case SeekOrigin.Begin:
                    Position = offset;

                    break;
            }

            if (Position < 0)
                Position = 0;
            else if (Position > Length)
                Position = Length;

            return Position;
        }

        public byte[] CopyBytes(int offset, int count)
        {
            byte[] read = new byte[count];

            for (Position = offset; Position < offset + count && Position < Length; Position++)
                read[Position - offset] = m_Data[Position];

            return read;
        }

        public PacketReader GetCompressedReader()
        {
            int fullLen = ReadInt32();
            int destLen = 0;
            byte[] buff;

            if (fullLen >= 4)
            {
                int packLen = ReadInt32();
                destLen = packLen;

                if (destLen < 0)
                    destLen = 0;

                buff = new byte[destLen];

                if (fullLen > 4 && destLen > 0)
                {
                    if (ZLib.uncompress(buff, ref destLen, CopyBytes(Position, fullLen - 4), fullLen - 4) != ZLibError.Z_OK)
                    {
                        destLen = 0;
                        buff = new byte[1];
                    }
                }
                else
                {
                    destLen = 0;
                    buff = new byte[1];
                }
            }
            else
                buff = new byte[1];

            return new PacketReader(buff, false);
        }

        public byte ReadByte()
        {
            if (Position + 1 > Length || m_Data == null)
                return 0;

            return m_Data[Position++];
        }

        public int ReadInt32()
        {
            return (ReadByte() << 24)
                   | (ReadByte() << 16)
                   | (ReadByte() << 8)
                   | ReadByte();
        }

        public short ReadInt16()
        {
            return (short) ((ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt32()
        {
            return (uint) (
                              (ReadByte() << 24)
                              | (ReadByte() << 16)
                              | (ReadByte() << 8)
                              | ReadByte());
        }

        public ulong ReadRawUInt64()
        {
            return ((ulong) ReadByte() << 0)
                   | ((ulong) ReadByte() << 8)
                   | ((ulong) ReadByte() << 16)
                   | ((ulong) ReadByte() << 24)
                   | ((ulong) ReadByte() << 32)
                   | ((ulong) ReadByte() << 40)
                   | ((ulong) ReadByte() << 48)
                   | ((ulong) ReadByte() << 56);
        }

        public ushort ReadUInt16()
        {
            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        public sbyte ReadSByte()
        {
            if (Position + 1 > Length)
                return 0;

            return (sbyte) m_Data[Position++];
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public string ReadUnicodeStringLE()
        {
            return ReadUnicodeString();
        }

        public string ReadUnicodeStringLESafe()
        {
            return ReadUnicodeStringSafe();
        }

        public string ReadUnicodeStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((c = ReadUInt16()) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char) c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((c = ReadUInt16()) != 0)
                sb.Append((char) c);

            return sb.ToString();
        }

        public bool IsSafeChar(int c)
        {
            return c >= 0x20 && c < 0xFFFE;
        }

        public string ReadUTF8StringSafe(int fixedLength)
        {
            if (Position >= Length)
                return string.Empty;

            int bound = Position + fixedLength;
            int end = bound;

            if (bound > Length)
                bound = Length;

            int count = 0;
            int index = Position;
            int start = Position;

            while (index < bound && ReadByte() != 0)
                ++count;

            Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (Position < bound && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            string s = Encoding.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = IsSafeChar(s[i]);

            Seek(start + fixedLength, SeekOrigin.Begin);

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                if (IsSafeChar(s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public string ReadUTF8StringSafe()
        {
            if (Position >= Length)
                return string.Empty;

            int count = 0;
            int index = Position;
            int start = index;

            while (index < Length && ReadByte() != 0)
                ++count;

            Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (Position < Length && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            string s = Encoding.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i)
                isSafe = IsSafeChar(s[i]);

            if (isSafe)
                return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                if (IsSafeChar(s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public string ReadUTF8String()
        {
            if (Position >= Length)
                return string.Empty;

            int count = 0;
            int index = Position;
            int start = index;

            while (index < Length && ReadByte() != 0)
                ++count;

            Seek(start, SeekOrigin.Begin);

            index = 0;

            byte[] buffer = new byte[count];
            int value = 0;

            while (Position < Length && (value = ReadByte()) != 0)
                buffer[index++] = (byte) value;

            return Encoding.UTF8.GetString(buffer);
        }

        public string ReadString()
        {
            return ReadStringSafe();
        }

        public string ReadStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (Position < Length && (c = ReadByte()) != 0)
                sb.Append((char) c);

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe(int fixedLength)
        {
            return ReadUnicodeString(fixedLength);
        }

        public string ReadUnicodeString(int fixedLength)
        {
            int bound = Position + (fixedLength << 1);
            int end = bound;

            if (bound > Length)
                bound = Length;

            StringBuilder sb = new StringBuilder();

            int c;

            while (Position + 1 < bound && (c = ReadUInt16()) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char) c);
            }

            Seek(end, SeekOrigin.Begin);

            return sb.ToString();
        }

        public string ReadUnicodeStringBE(int fixedLength)
        {
            int bound = Position + (fixedLength << 1);
            int end = bound;

            if (bound > Length)
                bound = Length;

            StringBuilder sb = new StringBuilder();

            int c;

            while (Position + 1 < bound)
            {
                c = (ushort) (ReadByte() | (ReadByte() << 8));
                sb.Append((char) c);
            }

            Seek(end, SeekOrigin.Begin);

            return sb.ToString();
        }

        public string ReadStringSafe(int fixedLength)
        {
            return ReadString(fixedLength);
        }

        public string ReadString(int fixedLength)
        {
            int bound = Position + fixedLength;
            int end = bound;

            if (bound > Length)
                bound = Length;

            StringBuilder sb = new StringBuilder();

            int c;

            while (Position < bound && (c = ReadByte()) != 0)
                sb.Append((char) c);

            Seek(end, SeekOrigin.Begin);

            return sb.ToString();
        }
    }
}