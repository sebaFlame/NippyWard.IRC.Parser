using System;
using System.Buffers;
using System.Collections.Generic;

using Xunit;

using NippyWard.Text;
using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser.Tests
{
    public static class AssertHelpers
    {
        internal static Token AssertParsed(string message)
        {
            Assert.True
            (
                IRCParser.TryParse
                (
                    CreateReadOnlySequence(message),
                    out Token token,
                    out _,
                    out _
                )
            );

            Assert.NotNull(token);

            return token;
        }

        internal static void AssertFirstOfTokenTypeIsEmpty
        (
            Token token,
            TokenType tokenType
        )
        {
            Token? foundToken = null;

            if (token.TokenType == tokenType)
            {
                foundToken = token;
            }
            else
            {
                Assert.True
                (
                    token.TryGetFirstTokenOfType
                    (
                        tokenType,
                        out foundToken
                    )
                );
            }

            Assert.True(foundToken.IsEmpty);
        }

        internal static void AssertFirstOfTokenTypeIsEqualTo
        (
            Token token,
            TokenType tokenType,
            string equalityComparer
        )
        {
            Token? foundToken = null;

            if (token.TokenType == tokenType)
            {
                foundToken = token;
            }
            else
            {
                Assert.True
                (
                    token.TryGetFirstTokenOfType
                    (
                        tokenType,
                        out foundToken
                    )
                );
            }

            Assert.False(foundToken.IsEmpty);

            Assert.Equal
            (
                (Utf8String)equalityComparer,
                foundToken.ToUtf8String()
            );
        }

        internal static void AssertFirstOfTokenTypeIsEqualTo
        (
            Token token,
            TokenType tokenType,
            ReadOnlyMemory<byte> equalityComparer
        )
        {
            Token? foundToken = null;

            if (token.TokenType == tokenType)
            {
                foundToken = token;
            }
            else
            {
                Assert.True
                (
                    token.TryGetFirstTokenOfType
                    (
                        tokenType,
                        out foundToken
                    )
                );
            }

            Assert.False(foundToken.IsEmpty);

            Assert.Equal
            (
                new Utf8String(equalityComparer),
                foundToken.ToUtf8String()
            );
        }

        internal static void AssertInNthChildOfTokenTypeInTokenType
        (
            Token token,
            TokenType parentTokenType,
            TokenType childTokenType,
            params Action<Token>[] verify
        )
        {
            Token? foundToken = null;

            if(token.TokenType == parentTokenType)
            {
                foundToken = token;
            }
            else
            {
                Assert.True
                (
                    token.TryGetFirstTokenOfType
                    (
                        parentTokenType,
                        out foundToken
                    )
                );
            }

            Assert.False(foundToken.IsEmpty);

            int childCount = 0;
            foreach(Token t
                    in foundToken.GetAllTokensOfType(childTokenType))
            {
                verify[childCount++](t);
            }

            Assert.Equal(childCount, verify.Length);
        }

        internal static ReadOnlySequence<byte> CreateReadOnlySequence
        (
            string message
        )
        {
            ReadOnlySequenceSegment<byte> segment
                = new Utf8StringSequenceSegment(message);

            return new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );
        }
    }
}
