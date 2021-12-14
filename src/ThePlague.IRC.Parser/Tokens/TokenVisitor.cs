using System;
using System.Collections.Generic;

namespace ThePlague.IRC.Parser.Tokens
{
    public static class TokenVisitor
    {
#nullable enable
        private static IEnumerable<Token> VisitToken(Token? token)
        {
            if(token is null)
            {
                yield break;
            }

            yield return token;

            foreach(Token t in VisitToken(token.Child))
            {
                yield return t;
            }

            foreach(Token t in VisitToken(token.Next))
            {
                yield return t;
            }
        }
#nullable disable

        public static void VisitAllTokens
        (
            this Token token,
            Action<Token> action
        )
        {
            foreach(Token t in VisitToken(token))
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

            foreach(Token t in VisitToken(token))
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

            foreach(Token t in VisitToken(token))
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

        public static IEnumerable<Token> GetAllTokensOfType
        (
            this Token token,
            TokenType tokenType
        )
        {

            foreach(Token t in VisitToken(token))
            {
                if(t.TokenType == tokenType)
                {
                    yield return t;
                }
            }
        }
    }
}
