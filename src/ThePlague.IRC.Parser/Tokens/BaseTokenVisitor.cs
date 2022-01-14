using System;
using System.Collections.Generic;

namespace ThePlague.IRC.Parser.Tokens
{
    public abstract class BaseTokenVisitor : IDisposable
    {
        protected Token Previous => this._previous;
        protected Token Parent
        {
            get
            {
                if(this._ancestors.TryPeek(out Token t))
                {
                    return t;
                }

                return null;
            }
        }

        private readonly Stack<Token> _ancestors;
        private Token _previous;

        public BaseTokenVisitor()
        {
            this._ancestors = new Stack<Token>();
        }

        ~BaseTokenVisitor()
        {
            this.Dispose(false);
        }

        public virtual void VisitToken(Token token)
        {
            if(token is null)
            {
                return;
            }

            //visit token
            this.VisitTokenDefault(token);
        }

        protected virtual void VisitTokenDefault(Token token)
        {
            //visit children
            this.VisitChild(token);

            //visit next token
            this.VisitNext(token);
        }

        //visit child of token
        protected bool VisitChild(Token token)
        {
            if(token.Child is null)
            {
                return false;
            }

            Token previousToken = this._previous;

            this._previous = null;

            //then visit its children
            this._ancestors.Push(token);
            this.VisitToken(token.Child);
            this._ancestors.Pop();

            this._previous = previousToken;

            return true;
        }

        //visit next tokens in linked list
        protected bool VisitNext(Token token)
        {
            if(token.Next is null)
            {
                return false;
            }

            Token previousToken = this._previous;

            this._previous = token;

            this.VisitToken(token.Next);

            this._previous = previousToken;

            return true;
        }

        public virtual void Reset()
        {
            this._ancestors.Clear();
            this._previous = null;
        }

        public void Dispose()
            => this.Dispose(true);

        public virtual void Dispose(bool isDisposing)
        {
            this.Reset();

            if(!isDisposing)
            {
                return;
            }

            GC.SuppressFinalize(this);
        }
    }
}
