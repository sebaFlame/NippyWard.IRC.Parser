using System.Buffers;

using Xunit;
using Xunit.Sdk;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser.Tests
{
    public class NicknameTests
    {
        [Fact]
        public void SimpleNicknameTest()
            => AssertNickname("coolguy");

        [Fact]
        public void SingleLetterNicknameTest()
            => AssertNickname("a");

        [Fact]
        public void InvalidStartLetterNicknameTest()
        {
            Assert.Throws<ParserException>
            (
                () => AssertNickname("1coolguy")
            );
        }

        [Fact]
        public void ValidStartLetterNicknameTest()
        {
            char[] validLetters = new char[26 + 26 + 9];

            int index = 0;
            //capital letters
            for(int i = 65; i <= 90; i++)
            {
                validLetters[index++] = (char)i;
            }

            //capital letters
            for(int i = 97; i <= 122; i++)
            {
                validLetters[index++] = (char)i;
            }

            //special
            validLetters[index++] = (char)0x5B;
            validLetters[index++] = (char)0x5C;
            validLetters[index++] = (char)0x5D;
            validLetters[index++] = (char)0x5E;
            validLetters[index++] = (char)0x5F;
            validLetters[index++] = (char)0x60;
            validLetters[index++] = (char)0x7B;
            validLetters[index++] = (char)0x7C;
            validLetters[index++] = (char)0x7D;

            for(int i = 0; i < validLetters.Length; i++)
            {
                AssertNickname
                (
                    string.Concat(validLetters[i], "coolguy{1}")
                );
            }
        }

        [Fact]
        public void ValidCharactersNicknameTest()
        {
            char[] validLetters = new char[26 + 10 + 26 + 9 + 1];

            int index = 0;
            //capital letters
            for(int i = 65; i <= 90; i++)
            {
                validLetters[index++] = (char)i;
            }

            //capital letters
            for(int i = 97; i <= 122; i++)
            {
                validLetters[index++] = (char)i;
            }

            //digits
            for(int i = 48; i <= 57; i++)
            {
                validLetters[index++] = (char)i;
            }

            //special
            validLetters[index++] = (char)0x5B;
            validLetters[index++] = (char)0x5C;
            validLetters[index++] = (char)0x5D;
            validLetters[index++] = (char)0x5E;
            validLetters[index++] = (char)0x5F;
            validLetters[index++] = (char)0x60;
            validLetters[index++] = (char)0x7B;
            validLetters[index++] = (char)0x7C;
            validLetters[index++] = (char)0x7D;

            //hyphen
            validLetters[index++] = (char)0x2D;

            //no byte-byte comparison, is valid UTF-8
            AssertNickname(new string(validLetters));
        }

        [Fact]
        public void InvalidCharactersNicknameTest()
        {
            Assert.Throws<EqualException>
            (
                () => AssertNickname(@"c!o@o+l #g%u&y")
            );
        }

        #region helper methods
        private static void AssertNickname
        (
            string nickname
        )
        {
            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(nickname);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            using Token token = IRCParser.ParseNickname(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Nickname,
                nickname
            );
        }
        #endregion
    }
}

