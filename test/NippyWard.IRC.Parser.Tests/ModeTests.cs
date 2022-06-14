using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Model.Core.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class ModeTests
    {
        [Fact]
        public void SingleModeTest()
        {
            string modes = "+v";

            AssertModes
            (
                modes,
                "+v"
            );
        }

        [Fact]
        public void MultiModeTest()
        {
            string modes = "+vohr";

            AssertModes
            (
                modes,
                "+v",
                "+o",
                "+h",
                "+r"
            );
        }

        [Fact]
        public void MultiModeStringTest()
        {
            string modes = "+vohr-jklm";

            AssertModes
            (
                modes,
                "+v",
                "+o",
                "+h",
                "+r",
                "-j",
                "-k",
                "-l",
                "-m"
            );
        }

        [Fact]
        public void EmptyModeTest()
        {
            string modes = "+";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(modes);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseModeStringList(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
                token,
                TokenType.ModeChars
            );
        }

        #region helper methods
        private static void AssertModes
        (
            string modes,
            params string[] expectedModes
        )
        {
            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(modes);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseModeStringList(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.ModeStringList,
                modes
            );

            int index = 0;
            foreach(Token modeString
                    in token.GetAllTokensOfType(TokenType.ModeString))
            {
                AssertModeString
                (
                    modeString,
                    ref index,
                    expectedModes
                );
            }
        }

        private static void AssertModeString
        (
            Token modeString,
            ref int index,
            params string[] expectedModes
        )
        {
            Assert.False(modeString.IsEmpty);
            Assert.NotNull(modeString.Child);
            TokenType modifier = modeString.Child.TokenType;

            Assert.True
            (
                modeString.TryGetFirstTokenOfType
                (
                    TokenType.ModeChars,
                    out Token modeChars
                )
            );

            Assert.False(modeChars.IsEmpty);
            Assert.NotNull(modeChars.Child);

            Assert.True
            (
                modeString.TryGetFirstTokenOfType
                (
                    TokenType.ModeCharsList,
                    out Token modeCharsList
                )
            );

            AssertModeCharsList
            (
                modeCharsList,
                modifier,
                ref index,
                expectedModes
            );
        }

        private static void AssertModeCharsList
        (
            Token modeCharsList,
            TokenType modifier,
            ref int index,
            params string[] expectedModes
        )
        {
            string expectedMode;
            foreach(Token mode
                    in modeCharsList.GetAllTokensOfType(TokenType.Mode))
            {
                expectedMode = expectedModes[index++];

                Assert.NotNull(expectedMode);
                Assert.Equal(2, expectedMode.Length);
                Assert.Equal((char)modifier, expectedMode[0]);

                Assert.Equal
                (
                    (Utf8String)expectedMode[1].ToString(),
                    mode.ToUtf8String()
                );
            }
        }
        #endregion

    }
}

