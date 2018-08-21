using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Assistant.JMap
{
    internal sealed class RiffPalette : IEnumerable<Color>
    {
        #region Fields

        public Color[] _palette;

        #endregion

        #region Constructors

        public RiffPalette()
        {
            _palette = new Color[0];
        }

        #endregion

        #region Methods

        public void Load(string fileName)
        {
            using (Stream stream = File.OpenRead(fileName))
            {
                this.Load(stream);
            }
        }

        public void Load(Stream stream)
        {
            byte[] buffer;
            bool eof;

            buffer = new byte[1032]; // create a buffer big enough to hold a default 256 color palette

            // first read and validate the form header
            // this is comprised of
            // * RIFF header
            // * document size
            // * FORM header
            stream.Read(buffer, 0, 12);
            if (buffer[0] != 'R' || buffer[1] != 'I' || buffer[2] != 'F' || buffer[3] != 'F')
            {
                throw new InvalidDataException("Source stream is not a RIFF document.");
            }

            // TODO: Validate the document size if required

            if (buffer[8] != 'P' || buffer[9] != 'A' || buffer[10] != 'L' || buffer[11] != ' ')
            {
                throw new InvalidDataException("Source stream is not a palette.");
            }

            eof = false;

            while (!eof)
            {
                // a RIFF document can have one or more chunks.
                // for our purposes, each chunk has a 4 letter
                // identifier and a size of the data part of the
                // chunk. We can use these two values to either
                // process the palette data, or skip data we
                // don't recognise

                // TODO: This procedure only supports simple, not extended, palettes

                if (stream.Read(buffer, 0, 8) == 8)
                {
                    int chunkSize;

                    chunkSize =  buffer.ToInt(4);

                    // see if we have the palette data
                    if (buffer[0] == 'd' && buffer[1] == 'a' && buffer[2] == 't' && buffer[3] == 'a')
                    {
                        if (chunkSize > buffer.Length)
                        {
                            buffer = new byte[chunkSize];
                        }

                        if (stream.Read(buffer, 0, chunkSize) != chunkSize)
                        {
                            throw new InvalidDataException("Failed to read enough data to match chunk size.");
                        }

                        _palette = this.ReadPalette(buffer);

                        // only one palette in a file, so break out of the chunk scan
                        eof = true;
                    }
                    else
                    {
                        // not the palette data? advance the stream to the next chunk

                        // riff chunks are word-aligned. if the size is odd
                        // then a padding byte is included after the chunk
                        // data. This is junk data, so we just skip it
                        if (chunkSize % 2 != 0)
                        {
                            chunkSize++;
                        }

                        stream.Position += chunkSize;
                    }
                }
                else
                {
                    // nothing to read, abort
                    eof = true;
                }
            }
          ;
        }

        private Color[] ReadPalette(byte[] buffer)
        {
            Color[] palette;
            ushort count;

            // The buffer should hold a LOGPALETTE structure containing
            // OS version (2 bytes, always 03)
            // Color count (2 bytes)
            // Color data (4 bytes * color count)

            count = buffer.ToInt16(2);

            palette = new Color[count];

            for (int i = 0; i < count; i++)
            {
                byte r;
                byte g;
                byte b;
                int offset;

                offset = i * 4 + 4;
                r = buffer[offset];
                g = buffer[offset + 1];
                b = buffer[offset + 2];

                // TODO: The fourth byte are flags, which we have no use for here

                palette[i] = Color.FromArgb(r, g, b);
            }

            return palette;
        }

        #endregion

        #region IEnumerable<Color> Interface

        public IEnumerator<Color> GetEnumerator()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < _palette.Length; i++)
            {
                yield return _palette[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }

    internal static class StreamExtensions
    {
        public static int ToInt(this byte[] buffer, int offset)
        {
            return buffer[offset + 3] << 24 | buffer[offset + 2] << 16 | buffer[offset + 1] << 8 | buffer[offset];
        }

        public static ushort ToInt16(this byte[] buffer, int offset)
        {
            return (ushort)(buffer[offset + 1] << 8 | buffer[offset]);
        }
    }
}
