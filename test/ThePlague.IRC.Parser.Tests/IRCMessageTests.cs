using System;
using System.Buffers;
using System.Text;

using Xunit;
using Xunit.Abstractions;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    public class IRCMessageTests
    {
        private readonly ITestOutputHelper _output;

        public IRCMessageTests(ITestOutputHelper testOutputHelper)
        {
            this._output = testOutputHelper;
        }

        [Fact]
        public void ServerSourcePrefixCodeTest()
        {
            string message = @":irc.jupiter.seb 001 seba :Welcome to the"
               + @" JupiterNET IRC Network seba!tkr@192.168.2.3"
               + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //check prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "irc.jupiter.seb"
                );

                //check command code
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandCode,
                    "001"
                );

                //verify the parameters
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "seba"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "Welcome to the JupiterNET IRC Network"
                                + " seba!tkr@192.168.2.3"
                        )
                    }
                );
            }
        }

        [Fact]
        public void UserSourcePrefixCommandTest()
        {
            string message = @":seba!tkr@C6D24F7.A68321BB.8CB04532.IP JOIN"
               + @" :#bleh"
               + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //check prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "seba!tkr@C6D24F7.A68321BB.8CB04532.IP"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "JOIN"
                );

                //verify the parameters
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "#bleh"
                        )
                    }
                );
            }
        }

        [Fact]
        public void ModeCommandTest()
        {
            string message = @"MODE seba :+iwx" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "MODE"
                );

                //verify the parameters
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "seba"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "+iwx"
                        )
                    }
                );
            }
        }

        [Fact]
        public void TagPrefixCommandMiddleTest()
        {
            string message = @"@url=;netsplit=tur,ty JOIN #bleh,#derp" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //verify the tags
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Tag,
                            "url="
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Tag,
                            "netsplit=tur,ty"
                        )
                    }
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "JOIN"
                );

                //verify the parameters
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#bleh,#derp"
                        )
                    }
                );
            }
        }

        //everything else should be based on the same base terminal collection
        [Fact]
        public void ValidMiddleParamCharacterTest()
        {
            byte[] message = new byte[256 - 4 + 3 + 1 + 2];
            Span<byte> cmd = new Span<byte>(message, 0, 3);

            //add command
            Encoding.UTF8.GetBytes("CMD".AsSpan(), cmd);

            //add space
            message[3] = 32;

            //add middle param
            int index = 4;

            //go from high to low to guarantee a Middle and not a CTCPMessage
            //(starts with 0x01)
            for(int i = 255; i >= 0; i--)
            {
                //full utf-8 without NUL, CR, LF, SPACE
                if(i is 0
                   or 13
                   or 10
                   or 32)
                {
                    continue;
                }

                message[index++] = (byte)i;
            }

            message[^2] = 13;
            message[^1] = 10;

            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(message);
            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );

            Assert.True
            (
                IRCParser.TryParse
                (
                    utf8,
                    out Token token
                )
            );

            Assert.NotNull(token);

            using(token)
            {
                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "CMD"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            new Memory<byte>(message, 4, message.Length - 4 - 2)
                        )
                    }
                );
            }
        }

        [Fact]
        public void ValidTrailingParamCharacterTest()
        {
            byte[] message = new byte[256 - 3 + 3 + 1 + 1 + 2];
            Span<byte> cmd = new Span<byte>(message, 0, 3);

            //add command
            Encoding.UTF8.GetBytes("CMD".AsSpan(), cmd);

            //add space
            message[3] = 32;
            //add colon
            message[4] = 58;

            //add trailing param
            int index = 5;
            for(int i = 0; i < 256; i++)
            {
                //full utf-8 without NUL, CR, LF
                if(i is 0
                   or 13
                   or 10)
                {
                    continue;
                }

                message[index++] = (byte)i;
            }

            message[^2] = 13;
            message[^1] = 10;

            Utf8StringSequenceSegment segment
                = new Utf8StringSequenceSegment(message);

            ReadOnlySequence<byte> utf8 = new ReadOnlySequence<byte>
            (
                segment,
                0,
                segment,
                segment.Memory.Length
            );

            Assert.True
            (
                IRCParser.TryParse
                (
                    utf8,
                    out Token token
                )
            );

            Assert.NotNull(token);

            using(token)
            {
                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "CMD"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            new Memory<byte>(message, 5, message.Length - 5 - 2)
                        )
                    }
                );
            }
        }

        //Rest of tests are examples taken from
        //https://github.com/ircdocs/parser-tests/blob/fdc9980582e548d069288e63ce38b9a6289dbc00/tests/msg-split.yaml

        [Fact]
        public void CommandMiddleTest()
        {
            string message = @"foo bar baz asdf" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "asdf"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleTest()
        {
            string message = @":coolguy foo bar baz asd" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "asd"
                        )
                    }
                );
            }
        }

        [Fact]
        public void CommandMiddleTrailingSpaceTest()
        {
            string message = @"foo bar baz :asdf quux" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "asdf quux"
                        )
                    }
                );
            }
        }

        [Fact]
        public void CommandMiddleEmptyTrailingTest()
        {
            string message = @"foo bar baz :" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.TrailingPrefix
                        )
                    }
                );
            }
        }

        [Fact]
        public void CommandMiddleTrailingColonStartTest()
        {
            string message = @"foo bar baz ::asdf" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix is empty
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            ":asdf"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleTrailingSpaceTest()
        {
            string message = @":coolguy foo bar baz :asdf quux" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "asdf quux"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleTrailingSpaceStartEndTest()
        {
            string message = @":coolguy foo bar baz :  asdf quux " + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "  asdf quux "
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleTrailingSpaceEndTest()
        {
            string message = @":coolguy PRIVMSG bar :lol :) " + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "lol :) "
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleEmptyTrailingTest()
        {
            string message = @":coolguy foo bar baz :" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.TrailingPrefix
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleSpaceTrailingTest()
        {
            string message = @":coolguy foo bar baz :  " + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "coolguy"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "baz"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "  "
                        )
                    }
                );
            }
        }

        [Fact]
        public void TagPrefixCommandTest()
        {
            string message = @"@a=b;c=32;k;rt=ql7 foo" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "a"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "b"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "c"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "32"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "k"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagSuffix
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "rt"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "ql7"
                            );
                        },
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );
            }
        }

        [Fact]
        public void EscapedTagPrefixCommandTest()
        {
            string message = @"@a=b\\and\nk;c=72\s45;d=gh\:764 foo"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "a"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "b\\and\nk"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "c"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                @"72 45"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "d"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                @"gh;764"
                            );
                        }
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "foo"
                );
            }
        }

        [Fact]
        public void TagPrefixSourcePrefixCommandMiddleTest()
        {
            string message = @"@c;h=;a=b :quux ab cd" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "c"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagSuffix
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "h"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagValue
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "a"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "b"
                            );
                        }
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "quux"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "ab"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "cd"
                        )
                    }
                );

            }
        }

        [Fact]
        public void SourcePrefixCommandMiddleLastParam()
        {
            string message = @":src JOIN #chan" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "src"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "JOIN"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#chan"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandTrailingLastParam()
        {
            string message = @":src JOIN :#chan" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "src"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "JOIN"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "#chan"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandEmptyParams()
        {
            string message = @":src AWAY" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "src"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "AWAY"
                );

                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.Params
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandEmptyLastParamWithoutColon()
        {
            string message = @":src AWAY " + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "src"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "AWAY"
                );

                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.ParamsSuffix
                );
            }
        }

        //DO NOT allow control non-hostname/non-nickname chars in source prefix,
        //cfr msg-split.yaml line 186

        [Fact]
        public void TagSourceCommandParamsTest()
        {
            string message = "@tag1=value1;tag2;vendor1/tag3=value2;"
                + "vendor2/tag4= :irc.example.com COMMAND param1 param2"
                + " :param3 param3"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "tag1"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "value1"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "tag2"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagSuffix
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "vendor1/tag3"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "value2"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "vendor2/tag4"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagValue
                            );
                        }
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "irc.example.com"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "COMMAND"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param1"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param2"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "param3 param3"
                        )
                    }
                );
            }
        }

        [Fact]
        public void SourcePrefixCommandParamsTest()
        {
            string message = ":irc.example.com COMMAND param1 param2"
                + " :param3 param3"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "irc.example.com"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "COMMAND"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param1"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param2"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "param3 param3"
                        )
                    }
                );
            }
        }

        [Fact]
        public void TagPrefixCommandParamsTest()
        {
            string message = "@tag1=value1;tag2;vendor1/tag3=value2"
                + ";vendor2/tag4 COMMAND param1 param2 :param3 param3"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "tag1"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "value1"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "tag2"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagSuffix
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "vendor1/tag3"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "value2"
                            );
                        },
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "vendor2/tag4"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                            (
                                t,
                                TokenType.TagSuffix
                            );
                        }
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "COMMAND"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param1"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "param2"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "param3 param3"
                        )
                    }
                );
            }
        }

        [Fact]
        public void Command()
        {
            string message = @"COMMAND" + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.TagPrefix
                );

                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "COMMAND"
                );

                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.Params
                );
            }
        }

        [Fact]
        public void EscapedTagPrefixCommandTest_2()
        {
            string message = @"@foo=\\\\\:\\s\s\r\n COMMAND"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Tags,
                    TokenType.Tag,
                    new Action<Token>[]
                    {
                        (Token t) =>
                        {
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagKey,
                                "foo"
                            );
                            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                            (
                                t,
                                TokenType.TagValue,
                                "\\\\;\\s \r\n"
                            );
                        }
                    }
                );

                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    token,
                    TokenType.SourcePrefix
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "COMMAND"
                );
            }
        }

        [Fact]
        public void UnrealBrokenMessagesTest_1()
        {
            string message = ":gravel.mozilla.org 432  #momo"
                + " :Erroneous Nickname: Illegal characters"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "gravel.mozilla.org"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandCode,
                    "432"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#momo"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "Erroneous Nickname: Illegal characters"
                        )
                    }
                );
            }
        }

        [Fact]
        public void UnrealBrokenMessagesTest_2()
        {
            string message = ":gravel.mozilla.org MODE #tckk +n "
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "gravel.mozilla.org"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "MODE"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#tckk"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "+n"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.ParamsSuffix
                        )
                    }
                );
            }
        }

        [Fact]
        public void UnrealBrokenMessagesTest_3()
        {
            string message = @":services.esper.net MODE #foo-bar +o foobar  "
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "services.esper.net"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "MODE"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#foo-bar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "+o"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "foobar"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                        (
                            t,
                            TokenType.ParamsSuffix
                        )
                    }
                );
            }
        }

        //Skip tag parsing tests

        [Fact]
        public void ModeCommandParamsTest_1()
        {
            string message = @":SomeOp MODE #channel :+i"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "SomeOp"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "MODE"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#channel"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "+i"
                        )
                    }
                );
            }
        }

        [Fact]
        public void ModeCommandParamsTest_2()
        {
            string message = @":SomeOp MODE #channel +oo SomeUser :AnotherUser"
                + "\r\n";

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "SomeOp"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "MODE"
                );

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#channel"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "+oo"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "SomeUser"
                        ),
                        (Token t) => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.TrailingPrefix,
                            "AnotherUser"
                        )
                    }
                );
            }
        }

        #region helper methods

        #endregion
    }
}
