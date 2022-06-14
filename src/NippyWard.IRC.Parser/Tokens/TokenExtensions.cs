using System;
using System.Collections.Generic;

namespace NippyWard.IRC.Parser.Tokens
{
    public static class TokenExtensions
    {
#nullable enable
        //combine 2 tokens as linked list and return currently added item
        public static Token Combine(this Token? left, Token right)
        {
            //left can be null
            if(left is null)
            {
                return right;
            }

            //always ensure the last one in the linked list
            Token leftToken = left.GetLastToken();

            leftToken.Next = right;

            return right;
        }

        private static IEnumerable<Token> VisitTokenInternal(Token? token)
        {
            if(token is null)
            {
                yield break;
            }

            yield return token;

            foreach(Token t in VisitTokenInternal(token.Child))
            {
                yield return t;
            }

            foreach(Token t in VisitTokenInternal(token.Next))
            {
                yield return t;
            }
        }
#nullable disable

        public static IEnumerable<Token> VisitToken
        (
            this Token token
        )
            => VisitTokenInternal(token);

        public static void VisitAllTokens
        (
            this Token token,
            Action<Token> action
        )
        {
            foreach(Token t in VisitTokenInternal(token))
            {
                action(t);
            }
        }

        public static void VisitTokensOfType
        (
            this Token token,
            TokenType tokenType,
            Action<Token> action)
        {

            foreach(Token t in VisitTokenInternal(token))
            {
                if(t.TokenType != tokenType)
                {
                    continue;
                }

                action(t);
            }
        }

        public static bool TryGetFirstTokenOfType
        (
            this Token token,
            TokenType tokenType,
            out Token result)
        {

            foreach(Token t in VisitTokenInternal(token))
            {
                if(t.TokenType == tokenType)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetTokenAtIndexOfType
        (
            this Token token,
            int index,
            TokenType tokenType,
            out Token result
        )
        {
            int idx = index;

            foreach(Token t in VisitTokenInternal(token))
            {
                if(t.TokenType == tokenType
                   && idx-- == 0)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetLastOfType
        (
            this Token token,
            TokenType tokenType,
            out Token result
        )
        {
            result = null;

            foreach(Token t in VisitTokenInternal(token))
            {
                if(t.TokenType == tokenType)
                {
                    result = t;
                }
            }

            return result is not null;
        }

        public static bool TryGetFirstTokenOfMask
        (
            this Token token,
            int tokenMask,
            out Token result)
        {
            int tokenType;
            foreach(Token t in VisitTokenInternal(token))
            {
                tokenType = (int)t.TokenType;
                if((tokenType & tokenMask) == tokenType)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static IEnumerable<Token> GetAllTokensOfType
        (
            this Token token,
            TokenType tokenType
        )
        {

            foreach(Token t in VisitTokenInternal(token))
            {
                if(t.TokenType == tokenType)
                {
                    yield return t;
                }
            }
        }

        public static IEnumerable<Token> GetAllTokensOfMask
        (
            this Token token,
            int tokenMask
        )
        {
            int tokenType;
            foreach(Token t in VisitTokenInternal(token))
            {
                tokenType = (int)t.TokenType;

                if((tokenType & tokenMask) == tokenType)
                {
                    yield return t;
                }
            }
        }

        public static Token GetLastToken
        (
            this Token token
        )
        {
            Token last = token, next;

            while((next = last.Next) is not null)
            {
                last = next;
            }

            return last;
        }
    }
}