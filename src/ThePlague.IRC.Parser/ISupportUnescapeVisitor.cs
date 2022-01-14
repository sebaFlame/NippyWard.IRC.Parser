using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser
{
    internal class ISupportUnescapeVisitor : BaseTokenVisitorByType
    {
        private static readonly byte[] _UnescapeValues = new byte[]
        {
            0x5C, // '\'
            0x20, // ' '
            0x3D  // '='
        };

        private static ReadOnlyMemory<byte> Backslash
            => new ReadOnlyMemory<byte>(_UnescapeValues, 0, 1);
        private static ReadOnlyMemory<byte> Space
            => new ReadOnlyMemory<byte>(_UnescapeValues, 1, 1);
        private static ReadOnlyMemory<byte> EqualitySign
            => new ReadOnlyMemory<byte>(_UnescapeValues, 2, 1);

        private Utf8StringSequenceSegment _segment;

        public ReadOnlySequence<byte> CreateUnescapedSequence
        (
            Token isupportValue
        )
        {
            this.Reset();

            if(isupportValue.TokenType != TokenType.ISupportValueItem)
            {
                throw new InvalidOperationException("Incorrect token type");
            }

            Utf8StringSequenceSegment startSegment = this._segment;

            this.VisitToken(isupportValue);

            return startSegment.CreateReadOnlySequence(this._segment);
        }

        //enforce a single value (all values are in a linked list)
        protected override void VisitISupportValueItem(Token token)
            => this.VisitChild(token);

        protected override void VisitISupportValueItemTerminals(Token token)
        {
            //add the full sequence to the sequence
            this._segment = this._segment.AddNewSequenceSegment(token.Sequence);

            this.VisitTokenDefault(token);
        }

        protected override void VisitISupportValueItemEscapeBackslash
        (
            Token token
        )
        {
            //add a '\' to the sequence
            this._segment = this._segment.AddNewSequenceSegment(Backslash);

            this.VisitTokenDefault(token);
        }

        protected override void VisitISupportValueItemEscapeSpace
        (
            Token token
        )
        {
            //add a ' ' to the sequence
            this._segment = this._segment.AddNewSequenceSegment(Space);

            this.VisitTokenDefault(token);
        }

        protected override void VisitISupportValueItemEscapeEqual
        (
            Token token
        )
        {
            //add a '=' to the sequence
            this._segment = this._segment.AddNewSequenceSegment(EqualitySign);

            this.VisitTokenDefault(token);
        }

        public override void Reset()
        {
            this._segment = new Utf8StringSequenceSegment
            (
                ReadOnlyMemory<byte>.Empty
            );

            base.Reset();
        }
    }
}
