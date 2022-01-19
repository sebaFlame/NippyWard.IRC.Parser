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

        [ThreadStatic]
        private static TokenStack _Pool;
        private const int _PoolSize = 64;

        public static int PooledTokens => _Pool.Count;

        private Token()
        {
            this.Sequence = default;
        }

        private static Token Create()
        {
            Token t;

            if(_Pool is null)
            {
                _Pool = new TokenStack(_PoolSize);
            }

            if(!_Pool.TryPop(out t))
            {
                t = new Token();
            }

            t.Sequence = default;
            return t;
        }

        public static Token Create
        (
            TokenType tokenType
        )
        {
            Token t = Create();
            t.TokenType = tokenType;
            return t;
        }

        public static Token Create
        (
            TokenType tokenType,
            in ReadOnlySequence<byte> sequence
        )
        {
            Token t = Create(tokenType);
            t.Sequence = sequence;
            return t;
        }

        public static Token Create
        (
            TokenType tokenType,
            in ReadOnlyMemory<byte> memory
        )
            => Create(tokenType, new ReadOnlySequence<byte>(memory));

        public static Token Create
        (
            TokenType tokenType,
            in ReadOnlySequence<byte> sequence,
            Token child
        )
        {
            Token t = Create(tokenType, sequence);
            t.Child = child;
            return t;
        }

        internal static Token Create
        (
            TokenType tokenType,
            Token child
        )
            => Create(tokenType, default, child);

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
            => this.Dispose(true);

        public void Dispose(bool isDisposing)
        {
            this.Child?.Dispose(isDisposing);
            this.Next?.Dispose(isDisposing);

            this.Child = null;
            this.Next = null;
            this.Sequence = default;

            if(!isDisposing)
            {
                return;
            }

            if(_Pool is null)
            {
                return;
            }

            _Pool.Push(this);
        }
    }
}
