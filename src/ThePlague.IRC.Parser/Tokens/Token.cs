using System;
using System.Buffers;

namespace ThePlague.IRC.Parser.Tokens
{
    public class Token : IDisposable
    {
        public TokenType TokenType { get; private set; }
        public bool IsEmpty => this.Sequence.IsEmpty;
        public ReadOnlySequence<byte> Sequence { get; private set; }

        public Token Next { get; internal set; }
        public Token Previous { get; internal set; }
        public Token Child { get; internal set; }

        private Token()
        {
            this.Sequence = default;
        }

        public Token
        (
            TokenType tokenType
        )
            : this()
        {
            this.TokenType = tokenType;
        }

        public Token
        (
            TokenType tokenType,
            in ReadOnlySequence<byte> sequence
        )
            : this(tokenType)
        {
            this.Sequence = sequence;
        }

        public Token
        (
            TokenType tokenType,
            in ReadOnlySequence<byte> sequence,
            Token children
        )
            : this(tokenType, sequence)
        {
            this.Child = children;
        }

        public void Dispose()
        {
            this.Child?.Dispose();
            this.Next?.Dispose();

            this.Child = null;
            this.Next = null;
            this.Previous = null;
            this.Sequence = default;
        }
    }
}
