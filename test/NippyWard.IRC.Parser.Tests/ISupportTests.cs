using System;
using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class ISupportTests
    {
        [Fact]
        public void SingleTokenWithoutValueISupportTest()
        {
            string parameter = "EXCEPTS";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(parameter);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportParameter,
                parameter
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                token,
                TokenType.ISupportTokenSuffix
            );
        }

        [Fact]
        public void SingleTokenWithValueISupportTest()
        {
            string parameter = "KICKLEN=255";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(parameter);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportParameter,
                "KICKLEN"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportValueItem,
                "255"
            );
        }

        [Fact]
        public void MulitTokenISupportTest()
        {
            string isupport = @"CASEMAPPING=ascii CHANNELLEN=32 CHANTYPES=#&"
                + " EXCEPTS";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(isupport);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                token,
                TokenType.ISupport,
                TokenType.ISupportToken,
                new Action<Token>[]
                {
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportParameter,
                            "CASEMAPPING"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportValueItem,
                            "ascii"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportParameter,
                            "CHANNELLEN"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportValueItem,
                            "32"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportParameter,
                            "CHANTYPES"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportValueItem,
                            "#&"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ISupportParameter,
                            "EXCEPTS"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.ISupportTokenSuffix
                        );
                    },
                }
            );
        }

        [Fact]
        public void MultiValueISupportTest()
        {
            string parameter = "TARGMAX=PRIVMSG:3,WHOIS:1,JOIN:";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(parameter);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportParameter,
                "TARGMAX"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportValue,
                "PRIVMSG:3"
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                token,
                TokenType.ISupportTokenSuffix,
                TokenType.ISupportValueItem,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"PRIVMSG",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"WHOIS",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"JOIN",
                        t.ToUtf8String()
                    ),
                }
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                token,
                TokenType.ISupportTokenSuffix,
                TokenType.ISupportValueItemSuffixValue,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"3",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"1",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.True
                    (
                        t.IsEmpty
                    ),
                }
            );
        }

        [Fact]
        public void EscapeISupportTest()
        {
            string parameter = "NETWORK=Example\\x20Network";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(parameter);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportParameter,
                "NETWORK"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportValueItem,
                "Example Network"
            );
        }

        [Fact]
        public void UnescapeISupportTest()
        {
            Utf8String value = (Utf8String)"Example Network";
            Token token = value.ISupportEscape();

            Assert.Equal
            (
                TokenType.ISupportValueItem,
                token.TokenType
            );

            Assert.Equal
            (
                (Utf8String)"Example\\x20Network",
                new Utf8String(token.Sequence)
            );
        }

        [Fact]
        public void EmptyISupportWithEqualityValueValueTest()
        {
            string parameter = "EXCEPTS=";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(parameter);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseISupport(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportParameter,
                "EXCEPTS"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ISupportTokenSuffix,
                "="
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                token,
                TokenType.ISupportValue
            );
        }
    }
}

