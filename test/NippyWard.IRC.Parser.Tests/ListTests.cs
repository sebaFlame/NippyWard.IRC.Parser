using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Model.Core.Text;

namespace NippyWard.IRC.Parser.Tests
{
    public class ListTests
    {
        [Fact]
        public void KeyListTest()
        {
            string list = "key1,key2,key3";

            AssertList
            (
                list,
                IRCParser.ParseKeyList,
                TokenType.Key,
                "key1",
                "key2",
                "key3"
            );
        }

        //also tests masks and elistcond
        [Fact]
        public void ValidCharactersKeyTest()
        {
            byte[] key = new byte[256 - 5];
            int index = 0;

            for(int i = 0; i < 256; i++)
            {
                //full utf-8 without NUL, CR, LF, SPACE or COMMA
                if
                (
                    i is 0
                    or 13
                    or 10
                    or 32
                    or 44
                )
                {
                    continue;
                }

                key[index++] = (byte)i;
            }

            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(key);
            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );
            SequenceReader<byte> reader = new SequenceReader<byte>(utf8);

            Token token = IRCParser.ParseKeyListItem(ref reader);

            Assert.True
            (
                token.TryGetFirstTokenOfType
                (
                    TokenType.Key,
                    out Token child
                )
            );

            Assert.False(child.IsEmpty);
            Assert.True(utf8.SequenceEquals(child.Sequence));
        }

        [Fact]
        public void InvalidCharactersKeyTest()
        {
            byte[] invalidChars = new byte[5];
            int index = 0;

            invalidChars[index++] = 0;
            invalidChars[index++] = 10;
            invalidChars[index++] = 13;
            invalidChars[index++] = 32;
            invalidChars[index++] = 44;

            index = 0;
            byte[] key = new byte[4];
            //add key
            key[index++] = 107;
            key[index++] = 101;
            key[index++] = 121;

            //initialize sequence
            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(key);
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
                key[index] = invalidChars[i];

                Token token = IRCParser.ParseKeyListItem(ref reader);

                Assert.False
                (
                    token.Sequence.SequenceEquals
                    (
                        utf8
                    )
                );

                Assert.True
                (
                    token.TryGetFirstTokenOfType
                    (
                        TokenType.Key,
                        out Token child
                    )
                );

                Assert.False(child.IsEmpty);
                Assert.False(utf8.SequenceEquals(child.Sequence));
            }
        }

        [Fact]
        public void NicknameSpaceListTest()
        {
            string list = "coolguy1 coolguy2 coolguy3";

            AssertList
            (
                list,
                IRCParser.ParseNicknameSpaceList,
                TokenType.Nickname,
                "coolguy1",
                "coolguy2",
                "coolguy3"
            );
        }

        [Fact]
        public void ChannelCommaListTest()
        {
            string list = "#channel,&channel,+channel";

            AssertList
            (
                list,
                IRCParser.ParseChannelCommaList,
                TokenType.Channel,
                "#channel",
                "&channel",
                "+channel"
            );
        }

        [Fact]
        public void ElistCondListTest()
        {
            string list = "c*y,c??lg?y,?channel";

            AssertList
            (
                list,
                IRCParser.ParseElistCondList,
                TokenType.ElistCond,
                "c*y",
                "c??lg?y",
                "?channel"
            );
        }

        #region helper methods
        private static void AssertList
        (
            string list,
            ParseToken parseList,
            TokenType itemType,
            params string[] items
        )
        {
            ReadOnlySequence<byte> sequence
                = AssertHelpers.CreateReadOnlySequence(list);
            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);

            Token token = parseList(ref reader);

            int index = 0;
            foreach(Token item
                    in token.GetAllTokensOfType(itemType))
            {
                Assert.Equal
                (
                    (Utf8String)items[index++],
                    item.ToUtf8String()
                );
            }
        }
        #endregion
    }
}

