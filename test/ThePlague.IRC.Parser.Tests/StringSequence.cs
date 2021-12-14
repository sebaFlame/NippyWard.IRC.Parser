using System;
using System.Buffers;
using System.Text;

namespace ThePlague.IRC.Parser.Tests
{
    internal class StringSequence : ReadOnlySequenceSegment<byte>
    {
        private static Encoding _Encoding => Encoding.UTF8;

        public StringSequence(string message)
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

        public StringSequence(byte[] message)
        {
            this.Memory = new Memory<byte>(message);
            this.RunningIndex = 0;
        }
    }
}
