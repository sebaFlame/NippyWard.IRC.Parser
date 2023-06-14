using System;
using System.Buffers;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser
{
    internal class TagUnescapeVisitor : BaseTokenVisitorByType
    {

        private static readonly byte[] _UnescapeValues = new byte[]
        {
            0x5C, // '\'
            0x3B, // ';'
            0x20, // '\s'
            0x0D, // '\r'
            0x0A  // '\n'
        };

        private static ReadOnlyMemory<byte> Backslash
            => new ReadOnlyMemory<byte>(_UnescapeValues, 0, 1);
        private static ReadOnlyMemory<byte> Semicolon
            => new ReadOnlyMemory<byte>(_UnescapeValues, 1, 1);
        private static ReadOnlyMemory<byte> Space
            => new ReadOnlyMemory<byte>(_UnescapeValues, 2, 1);
        private static ReadOnlyMemory<byte> CarriageReturn
            => new ReadOnlyMemory<byte>(_UnescapeValues, 3, 1);
        private static ReadOnlyMemory<byte> LineFeed
            => new ReadOnlyMemory<byte>(_UnescapeValues, 4, 1);

        private Utf8StringSequenceSegment _segment;

        public ReadOnlySequence<byte> CreateUnescapedSequence(Token tagValue)
        {
            this.Reset();

            if(tagValue.TokenType != TokenType.TagValue)
            {
                throw new InvalidOperationException("Incorrect token type");
            }

            Utf8StringSequenceSegment startSegment = this._segment;

            this.VisitToken(tagValue);

            return startSegment.CreateReadOnlySequence(this._segment);
        }

        protected override void VisitUTF8WithoutNullCrLfSemiColonSpace
        (
            Token token
        )
        {
            //add the full sequence to the sequence
            this._segment = this._segment.AddNewSequenceSegment(token.Sequence);

            this.VisitTokenDefault(token);
        }

        protected override void VisitTagValueEscapeSemicolon(Token token)
        {
            //add a semicolon to the sequence
            this._segment = this._segment.AddNewSequenceSegment(Semicolon);

            this.VisitTokenDefault(token);
        }

        protected override void VisitTagValueEscapeSpace(Token token)
        {
            //add a space to the sequence
            this._segment = this._segment.AddNewSequenceSegment(Space);

            this.VisitTokenDefault(token);
        }

        protected override void VisitTagValueEscapeCr(Token token)
        {
            //add a CR to the sequence
            this._segment = this._segment.AddNewSequenceSegment(CarriageReturn);

            this.VisitTokenDefault(token);
        }

        protected override void VisitTagValueEscapeLf(Token token)
        {
            //add a LF to the sequence
            this._segment = this._segment.AddNewSequenceSegment(LineFeed);

            this.VisitTokenDefault(token);
        }

        protected override void VisitTagValueEscapeBackslash(Token token)
        {
            //add a '\' to the sequence
            this._segment = this._segment.AddNewSequenceSegment(Backslash);

            this.VisitTokenDefault(token);
        }

        //skips the TagValueEscape backslash
        protected override void VisitTagValueEscapeInvalid(Token token)
            => this.VisitTokenDefault(token);

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
