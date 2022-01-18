using System;
using System.Buffers;

using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tokens
{
    public class Token : IDisposable
    {
        public TokenType TokenType { get; private set; }
        public bool IsEmpty => this.Sequence.IsEmpty && this.Child is null;
        public int Length => (int)this.Sequence.Length;
        public ReadOnlySequence<byte> Sequence { get; internal set; }

        public Token Next { get; internal set; }
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
            in ReadOnlyMemory<byte> memory
        )
            : this(tokenType, new ReadOnlySequence<byte>(memory))
        { }

        public Token
        (
            TokenType tokenType,
            in ReadOnlySequence<byte> sequence,
            Token child
        )
            : this(tokenType, sequence)
        {
            this.Child = child;
        }

        internal Token
        (
            TokenType tokenType,
            Token child
        )
            : this(tokenType, default, child)
        { }

        ~Token()
        {
            this.Dispose(false);
        }

        public Utf8String ToUtf8String()
        {
            if(this.TokenType == TokenType.TagValue)
            {
                return this.TagUnescape();
            }
            else if(this.TokenType == TokenType.ISupportValueItem)
            {
                return this.ISupportUnescape();
            }
            else
            {
                return new Utf8String(this.Sequence);
            }
        }

        public void ReplaceChild(Token newChild)
        {
            Token oldChild = this.Child;

            this.Child = newChild;

            if(oldChild is not null)
            {
                oldChild.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool isDisposing)
        {
            this.Child?.Dispose();
            this.Next?.Dispose();

            this.Child = null;
            this.Next = null;
            this.Sequence = default;
        }
    }
}
