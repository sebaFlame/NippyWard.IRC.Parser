using System;
using System.Buffers;
using System.Text;

namespace ThePlague.Model.Core.Text.Tests
{
    internal class Utf8StringSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        private static Encoding _Encoding => Encoding.UTF8;

        public Utf8StringSequenceSegment(string message)
        {
            Encoder utf8Encoder = _Encoding.GetEncoder();

            int byteCount = utf8Encoder.GetByteCount(message, true);

            byte[] utf8bytes = new byte[byteCount];

            Memory<byte> memory = new Memory<byte>(utf8bytes);

            utf8Encoder.Convert
            (
                message,
                memory.Span,
                true,
                out _,
                out _,
                out _
            );

            this.Memory = memory;
            this.RunningIndex = 0;
        }

        public void AddNext(Utf8StringSequenceSegment next)
        {
            this.Next = next;
            next.RunningIndex = this.RunningIndex + this.Memory.Length;
        }
    }
}
