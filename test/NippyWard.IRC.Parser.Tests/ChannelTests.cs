using System;
using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class ChannelTests
    {
        [Fact]
        public void NetworkChannelTest()
            => AssertChannel("#", "channel", string.Empty);

        [Fact]
        public void GeneralChannelSuffixTest()
            => AssertChannel("#", "channel", ":channel");

        [Fact]
        public void LocalChannelTest()
            => AssertChannel("&", "channel", string.Empty);

        [Fact]
        public void LocalChannelSuffixTest()
            => AssertChannel("&", "channel", ":channel");

        [Fact]
        public void UnmoderatedChannelTest()
            => AssertChannel("+", "channel", string.Empty);

        [Fact]
        public void UnmoderatedChannelSuffixTest()
            => AssertChannel("+", "channel", ":channel");

        [Fact]
        public void SafeChannelTest()
            => AssertChannel("!1A1A1", "channel", string.Empty);

        [Fact]
        public void SafeChannelSuffixTest()
            => AssertChannel("!1A1A1", "channel", ":channel");

        [Fact]
        public void InvalidChannelPrefixTest()
        {
            Assert.Throws<ParserException>
            (
                () => AssertChannel(@"@", "channel", string.Empty)
            );
        }

        [Fact]
        public void InvalidSafeChannelTest()
        {
            Assert.Throws<ParserException>
            (
                () => AssertChannel(@"!2aE", "channel", string.Empty)
            );
        }

        [Fact]
        public void ValidCharactersChannelTest()
        {
            byte[] channel = new byte[256 + 1 - 7];
            int index = 0;

            //add initial '#'
            channel[index++] = 35;
            for(int i = 0; i < 256; i++)
            {
                //full utf-8 without NUL, BELL, CR, LF, SPACE, COMMA or COLON
                if(i is 0
                   or 7
                   or 13
                   or 10
                   or 32
                   or 44
                   or 59)
                {
                    continue;
                }

                channel[index++] = (byte)i;
            }

            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(channel);
            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );
            SequenceReader<byte> reader = new SequenceReader<byte>(utf8);

            Token token = IRCParser.ParseChannel(ref reader);

            Assert.True
            (
                token.Sequence.SequenceEquals
                (
                    utf8
                )
            );
        }

        [Fact]
        public void InvalidCharactersChannelTest()
        {
            byte[] invalidChars = new byte[5];
            int index = 0;

            invalidChars[index++] = 0;
            invalidChars[index++] = 7;
            invalidChars[index++] = 10;
            invalidChars[index++] = 13;
            invalidChars[index++] = 44;

            index = 0;
            byte[] channel = new byte[5];
            //add initial '#'
            channel[index++] = 35;
            //add chan
            channel[index++] = 99;
            channel[index++] = 97;
            channel[index++] = 110;

            //initialize sequence
            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(channel);
            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );

            //iterate over all invalid characters
            for(int i = 0; i < invalidChars.Length; i++)
            {
                SequenceReader<byte> reader = new SequenceReader<byte>(utf8);

                //add invalid character
                channel[index] = invalidChars[i];

                Token token = IRCParser.ParseChannel(ref reader);

                Assert.False
                (
                    token.Sequence.SequenceEquals
                    (
                        utf8
                    )
                );
            }
        }

        #region helper methods
        private static void AssertChannel
        (
            string prefix,
            string name,
            string suffix
        )
        {
            string channel = string.Concat(prefix, name, suffix);

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(channel);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseChannel(ref reader);

            Assert.Equal
            (
                (Utf8String)channel,
                token.ToUtf8String()
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ChannelPrefix,
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

