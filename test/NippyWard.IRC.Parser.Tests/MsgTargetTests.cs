using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class MsgTargetTests
    {
        [Fact]
        public void SingleNicknameMsgTargetTest()
        {
            string target = "coolguy";
            AssertMsgTarget(target, TokenType.MsgToNickname);
        }

        [Fact]
        public void SingleChannelMsgTargetTest()
        {
            string target = "#channel";
            AssertMsgTarget(target, TokenType.MsgToChannel);
        }

        [Fact]
        public void SingleMaskMsgTargetTest()
        {
            string target = "$c*lg?y";
            AssertMsgTarget(target, TokenType.MsgToTargetMask);
        }

        [Fact]
        public void ChannelPrefixMsgTargetTest()
        {
            string[][] channelPrefixes = new string[][]
            {
                new string[] { "&", "channel", string.Empty },
                new string[] { "+", "channel", string.Empty },
                new string[] { "#", "channel", string.Empty },
                new string[] { "!1A1A1", "channel", string.Empty }
            };

            foreach(string[] parts in channelPrefixes)
            {
                Token token = AssertMsgTarget
                (
                    string.Join("", parts),
                    TokenType.MsgToChannel
                );

                AssertChannel(token, parts[0], parts[1], parts[2]);
            }
        }

        [Fact]
        public void ChannelPrefixWithSuffixMsgTargetTest()
        {

            string[][] channelPrefixes = new string[][]
            {
                new string[] { "&", "channel", ":chan" },
                new string[] { "+", "channel", ":chan" },
                new string[] { "#", "channel", ":chan" },
                new string[] { "!1A1A1", "channel", ":chan" }
            };

            foreach(string[] parts in channelPrefixes)
            {
                Token token = AssertMsgTarget
                (
                    string.Join("", parts),
                    TokenType.MsgToChannel
                );

                AssertChannel(token, parts[0], parts[1], parts[2]);
            }
        }

        [Fact]
        public void SingleChannelMembershipMsgTargetTest()
        {
            string target = "%#channel";
            AssertMsgTarget(target, TokenType.MsgToChannel);
        }

        [Fact]
        public void ChannelMembershipMsgTargetTest()
        {
            string name = "channel";

            string[] prefix = new string[]
            {
                "#",
                "&",
                "+",
                "!1A1A1"
            };

            string[] membership = new string[]
            {
                "+",
                "%",
                "@",
                "&",
                "~"
            };

            Token channel;
            Token[] prefixTokens;
            foreach(string mem in membership)
            {
                foreach(string pre in prefix)
                {
                    channel = AssertMsgTarget
                    (
                        string.Join("", mem, pre, name),
                        TokenType.MsgToChannel
                    );

                    prefixTokens = channel.GetAllTokensOfMask(0xFF).ToArray();
                    Assert.NotEmpty(prefixTokens);
                    Assert.Equal(2, prefixTokens.Length);

                    Assert.Equal
                    (
                        (Utf8String)mem,
                        prefixTokens[0].ToUtf8String()
                    );

                    Assert.Equal
                    (
                        (Utf8String)pre.Substring(0, 1),
                        prefixTokens[1].ToUtf8String()
                    );
                }
            }
        }

        [Fact]
        public void MultiNicknameMsgTargetTest()
        {
            AssertMsgTargetItems
            (
                ("coolguy1", TokenType.MsgToNickname),
                ("coolguy2", TokenType.MsgToNickname),
                ("coolguy3", TokenType.MsgToNickname),
                ("coolguy4", TokenType.MsgToNickname)
            );
        }

        [Fact]
        public void MultiChannelMsgTargetTest()
        {
            AssertMsgTargetItems
            (
                ("#channel1", TokenType.MsgToChannel),
                ("#channel2", TokenType.MsgToChannel),
                ("#channel3", TokenType.MsgToChannel),
                ("#channel4", TokenType.MsgToChannel)
            );
        }

        [Fact]
        public void MultiMaskMsgTargetTest()
        {
            AssertMsgTargetItems
            (
                ("$c*lg*1", TokenType.MsgToTargetMask),
                ("$c*lg*2", TokenType.MsgToTargetMask),
                ("$c*lg*3", TokenType.MsgToTargetMask),
                ("$c*lg*4", TokenType.MsgToTargetMask)
            );
        }

        [Fact]
        public void MultiMixedMsgTargetTest()
        {
            AssertMsgTargetItems
            (
                ("#channel1", TokenType.MsgToChannel),
                ("$c*lg*1", TokenType.MsgToTargetMask),
                ("coolguy1", TokenType.MsgToNickname),
                ("#channel2", TokenType.MsgToChannel),
                ("#channel3", TokenType.MsgToChannel),
                ("coolguy2", TokenType.MsgToNickname),
                ("$c*lg*2", TokenType.MsgToTargetMask),
                ("$c*lg*3", TokenType.MsgToTargetMask),
                ("coolguy3", TokenType.MsgToNickname),
                ("#channel4", TokenType.MsgToChannel),
                ("$c*lg*4", TokenType.MsgToTargetMask),
                ("coolguy4", TokenType.MsgToNickname)
            );
        }

        #region helper methods
        private static Token AssertMsgTarget
        (
            string target,
            TokenType expectedTarget
        )
        {
            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(target);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseMsgTarget(ref reader);
            Assert.False(token.IsEmpty);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    expectedTarget,
                    out Token child
                )
            );
            Assert.False(child.IsEmpty);

            Assert.Equal
            (
                (Utf8String)target,
                child.ToUtf8String()
            );

            return token;
        }

        private static void AssertMsgTargetItems
        (
            params ValueTuple<string, TokenType>[] items
        )
        {
            string target = string.Join(',', items.Select(x => x.Item1));

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(target);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseMsgTarget(ref reader);
            Assert.False(token.IsEmpty);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.MsgTargetSuffix,
                    out Token suffix
                )
            );
            Assert.False(suffix.IsEmpty);

            int index = 0;
            Token child;
            ValueTuple<string, TokenType> item;
            foreach(Token msgTo in token.GetAllTokensOfType(TokenType.MsgTo))
            {
                item = items[index++];

                Assert.True
                (
                    msgTo.TryGetFirstTokenOfType
                    (
                        item.Item2,
                        out child
                    )
                );
                Assert.False(child.IsEmpty);

                Assert.Equal
                (
                    (Utf8String)item.Item1,
                    child.ToUtf8String()
                );
            }
            Assert.Equal(index, items.Length);
        }

        private static void AssertChannel
        (
            Token token,
            string prefix,
            string name,
            string suffix
        )
        {
            string channel = string.Concat(prefix, name, suffix);

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(channel);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.MsgToChannelPrefix,
                prefix
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ChannelString,
                name
            );

            if(!string.IsNullOrEmpty(suffix))
            {
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.ChannelSuffix,
                    suffix
                );
            }
            else
            {
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.ChannelSuffix
                );
            }
        }

        #endregion
    }
}

