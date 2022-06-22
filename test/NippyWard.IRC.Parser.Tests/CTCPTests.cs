using System;
using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class CTCPTests
    {
        [Fact]
        public void PRIVMSGCTCPTest_1()
        {
            string message = ":alice!a@localhost PRIVMSG bob :\x01VERSION\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Verb,
                "PRIVMSG"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.TrailingPrefix,
                "\x01VERSION\x01"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPMessage,
                "\x01VERSION\x01"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPCommand,
                "VERSION"
            );
        }

        [Fact]
        public void PRIVMSGCTCPNoTrailingTest()
        {
            string message = ":alice!a@localhost PRIVMSG bob \x01VERSION\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Verb,
                "PRIVMSG"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPMessage,
                "\x01VERSION\x01"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPCommand,
                "VERSION"
            );
        }

        [Fact]
        public void PRIVMSGCTCPTest_2()
        {
            string message = ":alice!a@localhost PRIVMSG #ircv3"
               + " :\x01PING 1473523796 918320"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Verb,
                "PRIVMSG"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPMessage,
                "\x01PING 1473523796 918320"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPCommand,
                "PING"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPParamsSuffix,
                "1473523796 918320"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                token,
                TokenType.CTCPMessageSuffix
            );
        }

        [Fact]
        public void NOTICECTCPTest_1()
        {
            string message = ":bob!b@localhost NOTICE alice"
                + " :\x01VERSION Snak for Mac 4.13\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Verb,
                "NOTICE"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPMessage,
                "\x01VERSION Snak for Mac 4.13\x01"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPCommand,
                "VERSION"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPParamsSuffix,
                "Snak for Mac 4.13"
            );
        }

        [Fact]
        public void NOTICECTCPTest_2()
        {
            string message = ":bob!b@localhost NOTICE alice"
               + " :\x01PING 1473523796 918320\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Verb,
                "NOTICE"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPMessage,
                "\x01PING 1473523796 918320\x01"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPCommand,
                "PING"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.CTCPParamsSuffix,
                "1473523796 918320"
            );
        }

        [Fact]
        public void DCCCTCPTest()
        {
            string message = ":alice!a@localhost PRIVMSG bob"
                + " :\u0001DCC SEND file.dat 2130706433 47515\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.CTCPMessage,
                    out Token ctcpMessage
                )
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                ctcpMessage,
                TokenType.CTCPCommand,
                "DCC"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                ctcpMessage,
                TokenType.CTCPParamsSuffix,
                "SEND file.dat 2130706433 47515"
            );

            Assert.True
            (
                ctcpMessage.TryGetLastOfType
                (
                    TokenType.CTCPParamsSuffix,
                    out Token parameters
                )
            );

            SequenceReader<byte> reader = new SequenceReader<byte>
            (
                parameters.Sequence
            );
            Token dccMessage = IRCParser.ParseDCCMessage(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                dccMessage,
                TokenType.DCCType,
                "SEND"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                dccMessage,
                TokenType.DCCQuotedArgument,
                "file.dat"
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                dccMessage,
                TokenType.DCCMessage,
                TokenType.DCCArgument,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"2130706433",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"47515",
                        t.ToUtf8String()
                    )
                }
            );
        }

        [Fact]
        public void DCCQuotedFilenameCTCPTest()
        {
            string message = ":alice!a@localhost PRIVMSG bob"
                + " :\u0001DCC SEND \"file 1.dat\" 2130706433 47515\x01"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.CTCPMessage,
                    out Token ctcpMessage
                )
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                ctcpMessage,
                TokenType.CTCPCommand,
                "DCC"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                ctcpMessage,
                TokenType.CTCPParamsSuffix,
                "SEND \"file 1.dat\" 2130706433 47515"
            );

            Assert.True
            (
                ctcpMessage.TryGetLastOfType
                (
                    TokenType.CTCPParamsSuffix,
                    out Token parameters
                )
            );

            SequenceReader<byte> reader = new SequenceReader<byte>
            (
                parameters.Sequence
            );
            Token dccMessage = IRCParser.ParseDCCMessage(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                dccMessage,
                TokenType.DCCType,
                "SEND"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                dccMessage,
                TokenType.DCCQuotedArgument,
                "\"file 1.dat\""
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                dccMessage,
                TokenType.DCCFilenameSpaceList,
                "file 1.dat"
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                dccMessage,
                TokenType.DCCMessage,
                TokenType.DCCArgument,
                new Action<Token>[]
                {
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"2130706433",
                        t.ToUtf8String()
                    ),
                    (Token t) => Assert.Equal
                    (
                        (Utf8String)"47515",
                        t.ToUtf8String()
                    )
                }
            );
        }
    }
}

