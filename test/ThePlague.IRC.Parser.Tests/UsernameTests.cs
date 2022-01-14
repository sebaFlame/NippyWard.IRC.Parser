using System.Buffers;

using Xunit;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    public class UsernameTests
    {
        [Fact]
        public void SimpleUsernameTest()
            => AssertUsername("coolguy");

        [Fact]
        public void IdentUsernameTest()
            => AssertUsername("~coolguy");

        [Fact]
        public void ValidCharactersUsernameTest()
        {
            byte[] username = new byte[256 - 5];
            int index = 0;

            for(int i = 0; i < 256; i++)
            {
                //full utf-8 without NUL, CR, LF, SPACE or @
                if(i is 0
                   or 13
                   or 10
                   or 32
                   or 64)
                {
                    continue;
                }

                username[index++] = (byte)i;
            }

            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(username);
            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );
            SequenceReader<byte> reader = new SequenceReader<byte>(utf8);

            Token token = IRCParser.ParseUsername(ref reader);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.Username,
                    out Token child
                )
            );

            Assert.True(utf8.SequenceEquals(child.Sequence));
        }

        [Fact]
        public void UserhostTest()
        {
            string userHost = "coolguy!~coolguy@localhost";

            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(userHost);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseUserHost(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Nickname,
                "coolguy"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Username,
                "~coolguy"
            );

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Host,
                "localhost"
            );
        }

        #region helper methods
        private static void AssertUsername
        (
            string username
        )
        {
            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(username);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = IRCParser.ParseUsername(ref reader);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
                token,
                TokenType.Username,
                username
            );
        }
        #endregion
    }
}

