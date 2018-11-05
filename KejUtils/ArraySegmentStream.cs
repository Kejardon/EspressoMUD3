using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// Alternative to MemoryStream that can use or swap an ArraySegment for its data.
    /// </summary>
    public class ArraySegmentStream : Stream
    {
        public ArraySegment<byte> Buffer;
        private int position;

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return Buffer.Count; } }

        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                position = (int)value;
            }
        }
        
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, Buffer.Count - position);
            if (count <= 0) return 0;
            Array.Copy(Buffer.Array, position + Buffer.Offset, buffer, offset, count);
            position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    position = (int)offset;
                    break;
                case SeekOrigin.Current:
                    position += (int)offset;
                    break;
                case SeekOrigin.End:
                    position = Buffer.Count + position;
                    break;
            }
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("ArraySegmentStream does not support SetLength");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            count = Math.Max(count, Buffer.Count - position);
            if (count == 0) return;
            Array.Copy(buffer, offset, Buffer.Array, position + Buffer.Offset, count);
            position += count;
        }
        public override bool CanTimeout { get { return false; } }
        public override void Close() { }
        public override int ReadByte()
        {
            if(Buffer.Count <= position)
            {
                return -1;
            }
            return Buffer.Array[Buffer.Offset + position++];
        }
        public override void WriteByte(byte value)
        {
            if (Buffer.Count > position)
            {
                Buffer.Array[Buffer.Offset + position++] = value;
            }
        }
        protected override void Dispose(bool disposing) { }
    }
}
