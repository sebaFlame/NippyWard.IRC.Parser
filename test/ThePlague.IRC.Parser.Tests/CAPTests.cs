using System;
using System.Buffers;

using Xunit;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    public class CAPTests
    {
        [Fact]
        public void CAPReplyTest()
        {
            string message = "CAP * LS"
                + " :multi-prefix"
                + " sasl=PLAIN,EXTERNAL"
                + " server-time"
                + " draft/packing=EX1,EX2"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.TrailingPrefix,
                    out Token trailing
                )
            );

            Assert.Equal
            (
                (Utf8String)"multi-prefix sasl=PLAIN,EXTERNAL server-time draft/packing=EX1,EX2",
                trailing.ToUtf8String()
            );

            SequenceReader<byte> reader
                = new SequenceReader<byte>(trailing.Sequence);
            Token capItems = IRCParser.ParseCapList(ref reader);

            Assert.Equal
            (
                (Utf8String)"multi-prefix sasl=PLAIN,EXTERNAL server-time draft/packing=EX1,EX2",
                capItems.ToUtf8String()
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                capItems,
                TokenType.CapList,
                TokenType.CapListItem,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"multi-prefix",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"sasl=PLAIN,EXTERNAL",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"server-time",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"draft/packing=EX1,EX2",
                        t.ToUtf8String()
                    ),
                }
            );
        }

        [Fact]
        public void CAPOptionsCAPTest()
        {
            string capList = "userhost-in-names example.org/dummy-cap sasl=EXTERNAL,DH-AES,DH-BLOWFISH,ECDSA-NIST256P-CHALLENGE,PLAIN";

            SequenceReader<byte> reader = new SequenceReader<byte>
            (
                AssertHelpers.CreateReadOnlySequence(capList)
            );

            Token token = IRCParser.ParseCapList(ref reader);
            Token valueList = null;

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                token,
                TokenType.CapList,
                TokenType.CapListItem,
                new Action<Token>[]
                {
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.CapListItemKey,
                            "userhost-in-names"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.CapListItemSuffix
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.CapListItemKey,
                            "example.org/dummy-cap"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.CapListItemSuffix
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.CapListItemKey,
                            "sasl"
                        );

                        valueList = t;
                    }
                }
            );

            Assert.NotNull(valueList);

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                valueList,
                TokenType.CapListItemValueList,
                TokenType.CapListItemValueListItem,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"EXTERNAL",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"DH-AES",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"DH-BLOWFISH",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"ECDSA-NIST256P-CHALLENGE",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"PLAIN",
                        t.ToUtf8String()
                    ),
                }
            );
        }
    }
}

