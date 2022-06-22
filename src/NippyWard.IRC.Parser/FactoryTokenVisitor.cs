using System;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser
{
    internal class FactoryTokenVisitor : BaseTokenVisitorByType
    {
        private Token _message;
        //tail of the current utf-8 linked list
        private Utf8StringSequenceSegment _segment;
        private Token _parentFactoryToken;
        private Token _previousFactoryToken;

        public FactoryTokenVisitor()
        { }

        private bool VisitFactoryChild
        (
            Token token,
            Token factoryToken
        )
        {
            if(token.Child is null)
            {
                return false;
            }

            Token parentToken = this._parentFactoryToken;
            Token previousToken = this._previousFactoryToken;

            this._previousFactoryToken = null;
            this._parentFactoryToken = factoryToken;

            this.VisitChild(token);

            this._parentFactoryToken = parentToken;
            this._previousFactoryToken = previousToken;

            return true;
        }

        private bool VisitFactoryNext
        (
            Token token,
            Token factoryToken
        )
        {
            if(token.Next is null)
            {
                return false;
            }

            Token previousToken = this._previousFactoryToken;
            Token parentToken = this._parentFactoryToken;

            this._previousFactoryToken = factoryToken;
            this._parentFactoryToken = null;

            this.VisitNext(token);

            this._previousFactoryToken = previousToken;
            this._parentFactoryToken = parentToken;

            return true;
        }


        private void LinkNewTokenIntoTree(Token factoryToken)
        {
            //can not be both
            if(this._previousFactoryToken is not null)
            {
                this._previousFactoryToken.Combine(factoryToken);
            }
            else if(this._parentFactoryToken is not null)
            {
                this._parentFactoryToken.Child = factoryToken;
            }
        }

        //create a copy of a token
        protected override void VisitTokenDefault(Token token)
        {
            Utf8StringSequenceSegment startSegment
                = this._segment;
            Token factoryToken;

            factoryToken = Token.Create
            (
                token.TokenType
            );

            //if no children, construct from token sequence
            if(!this.VisitFactoryChild(token, factoryToken))
            {
                this._segment = this._segment.AddNewSequenceSegment
                (
                    token.Sequence
                );
            }

            factoryToken.Sequence = startSegment.CreateReadOnlySequence
            (
                this._segment
            );

            //link into tree as child or next
            this.LinkNewTokenIntoTree(factoryToken);

            this.VisitFactoryNext(token, factoryToken);
        }

        protected override void VisitConstructedMessage(Token token)
        {
            //start new sequence
            Utf8StringSequenceSegment startSegment
                = new Utf8StringSequenceSegment(ReadOnlyMemory<byte>.Empty);
            Token factoryToken;

            this._segment = startSegment;

            factoryToken = Token.Create
            (
                TokenType.Message
            );

            this.VisitFactoryChild(token, factoryToken);

            factoryToken.Sequence = startSegment.CreateReadOnlySequence
            (
                this._segment
            );

            if(this._message is null)
            {
                this._message = factoryToken;
            }
            else
            {
                this._message
                    .GetLastToken()
                    .Combine(factoryToken);
            }

            this.VisitFactoryNext(token, factoryToken);
        }

        public Token ConstructMessage(Token token)
        {
            this.Reset();
            this.VisitToken(token);
            return this._message;
        }

        public override void Reset()
        {
            this._message = null;
            this._segment = null;
            this._previousFactoryToken = null;
            this._parentFactoryToken = null;

            base.Reset();
        }

        public override void Dispose(bool isDisposing)
        {
            this._message = null;
            this._segment = null;
            this._previousFactoryToken = null;
            this._parentFactoryToken = null;

            base.Dispose(isDisposing);
        }
    }
}
