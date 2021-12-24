using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    internal class MessageLengthVisitor : BaseTokenVisitor
    {
        public int Length => this._length;

        private int _length;

        public MessageLengthVisitor()
        {
            this._length = 0;
        }

        public int ComputeTokenLength(Token token)
        {
            this.Reset();

            this.VisitToken(token);

            return this._length;
        }

        //create a copy of a token
        protected override void VisitTokenDefault(Token token)
        {
            //only add lenght of tokens without child
            if(!this.VisitChild(token))
            {
                this._length += token.Length;
            }

            this.VisitNext(token);
        }

        public override void Reset()
        {
            this._length = 0;

            base.Reset();
        }
    }
}
