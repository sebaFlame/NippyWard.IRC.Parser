using System;
using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Model.Core.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class ParameterTests
    {
        [Fact]
        public void NameReplyTest()
        {
            string message = ":server 253 bob = #channel :~bob &alice %coolguy"
                + " +user1 user2"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);
            Assert.True
            (
                token.TryGetTokenAtIndexOfType
                (
                    1,
                    TokenType.ParamsSuffix,
                    out Token skippedTarget
                )
            );

            Assert.Equal
            (
                (Utf8String)"= #channel :~bob &alice %coolguy +user1 user2",
                skippedTarget.ToUtf8String()
            );

            SequenceReader<byte> reader =
                new SequenceReader<byte>(skippedTarget.Sequence);

            Token nameReply = IRCParser.ParseNameReply(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                nameReply,
                TokenType.NameReplyChannelType,
                "="
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                nameReply,
                TokenType.Channel,
                "#channel"
            );

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                nameReply,
                TokenType.NicknameMembershipSpaceList,
                TokenType.NicknameMembership,
                new Action<Token>[]
                {
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ChannelMembershipPrefix,
                            "~"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "bob"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ChannelMembershipPrefix,
                            "&"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "alice"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ChannelMembershipPrefix,
                            "%"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "coolguy"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.ChannelMembershipPrefix,
                            "+"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "user1"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "user2"
                        );
                    },
                }
            );
        }

        [Fact]
        public void UserHostReplyTest()
        {
            string message = ":server 302 bob :"
                + "alice=+a@localhost"
                + " coolguy*=-127.0.0.1"
                + " bob=+bob@127.0.0.1"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);
            Assert.True
            (
                token.TryGetLastOfType
                (
                    TokenType.TrailingPrefix,
                    out Token trailing
                )
            );

            Assert.Equal
            (
                (Utf8String)("alice=+a@localhost"
                    + " coolguy*=-127.0.0.1"
                    + " bob=+bob@127.0.0.1"),
                trailing.ToUtf8String()
            );

            SequenceReader<byte> reader =
                new SequenceReader<byte>(trailing.Sequence);
            Token userHostReply = IRCParser.ParseUserHostList(ref reader);

            AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
            (
                userHostReply,
                TokenType.UserHostList,
                TokenType.UserHostListItem,
                new Action<Token>[]
                {
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "alice"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.UserHostListOp
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListAway,
                            "+"
                        );

                        //signifies first UserHostListHostnamePrefix is a
                        //username
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListHostnameSuffix,
                            "@localhost"
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "coolguy"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListOp,
                            "*"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListAway,
                            "-"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListHostnamePrefix,
                            "127.0.0.1"
                        );

                        //no hostname suffix, prefix signifies a host
                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.UserHostListHostnameSuffix
                        );
                    },
                    (Token t) =>
                    {
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Nickname,
                            "bob"
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.UserHostListOp
                        );

                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListAway,
                            "+"
                        );

                        //signifies first UserHostListHostnamePrefix is a
                        //username
                        AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.UserHostListHostnameSuffix,
                            "@127.0.0.1"
                        );
                    },
                }
            );
        }

        [Fact]
        public void WhoReplyTest()
        {
            string message = ":server 302 bob"
                + " *"
                + " bob"
                + " 127.0.0.1"
                + " server"
                + " bob"
                + " H"
                + " :"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);
            Assert.True
            (
                token.TryGetTokenAtIndexOfType
                (
                    1,
                    TokenType.ParamsSuffix,
                    out Token skippedTarget
                )
            );

            Assert.Equal
            (
                (Utf8String)("*"
                + " bob"
                + " 127.0.0.1"
                + " server"
                + " bob"
                + " H"
                + " :"),
                skippedTarget.ToUtf8String()
            );

            SequenceReader<byte> reader =
                new SequenceReader<byte>(skippedTarget.Sequence);

            Token whoReply = IRCParser.ParseWhoReply(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyPrefix,
                "*"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Username,
                "bob"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Host,
                "127.0.0.1"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.ServerName,
                "server"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Nickname,
                "bob"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyAway,
                "H"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                whoReply,
                TokenType.WhoReplyFlagsOp
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                whoReply,
                TokenType.WhoReplyChannelMembership
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                whoReply,
                TokenType.ModeChars
            );
        }

        [Fact]
        public void WhoReplyChannelTest()
        {
            string message = ":server 302 bob"
                + " &help"
                + " admin"
                + " 127.0.0.1"
                + " server"
                + " admin"
                + " G*~rio"
                + " :"
                + "\r\n";

            Token token = AssertHelpers.AssertParsed(message);
            Assert.True
            (
                token.TryGetTokenAtIndexOfType
                (
                    1,
                    TokenType.ParamsSuffix,
                    out Token skippedTarget
                )
            );

            Assert.Equal
            (
                (Utf8String)("&help"
                + " admin"
                + " 127.0.0.1"
                + " server"
                + " admin"
                + " G*~rio"
                + " :"),
                skippedTarget.ToUtf8String()
            );

            SequenceReader<byte> reader =
                new SequenceReader<byte>(skippedTarget.Sequence);

            Token whoReply = IRCParser.ParseWhoReply(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyPrefix,
                "&help"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Username,
                "admin"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Host,
                "127.0.0.1"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.ServerName,
                "server"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.Nickname,
                "admin"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyAway,
                "G"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyFlagsOp,
                "*"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.WhoReplyChannelMembership,
                "~"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                whoReply,
                TokenType.ModeChars,
                "rio"
            );
        }
    }
}

