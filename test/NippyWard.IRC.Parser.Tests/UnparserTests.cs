using System;
using System.Linq;
using System.Text;

using Xunit;
using Xunit.Abstractions;

using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;

namespace NippyWard.IRC.Parser.Tests
{
    //TODO:
    // add factories to a global (reusable) context
    public class UnparserTests : IDisposable
    {
        private readonly GeneralIRCMessageFactory _factory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TestIRCMessageFactory _testFactory;

        public UnparserTests(ITestOutputHelper outputHelper)
        {
            this._factory = new GeneralIRCMessageFactory();
            this._testFactory = new TestIRCMessageFactory();
            this._outputHelper = outputHelper;
        }

        [Fact]
        public void VerbParamsTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    "foo bar baz asdf" + "\r\n",
                    "foo bar baz :asdf" + "\r\n"
                );

                Assert.True
                (
                    constructedMessage.TryGetFirstTokenOfType(TokenType.Verb, out Token v)
                        && new Utf8String(v.Sequence).Equals(new Utf8String("foo"))
                );

                Assert.True
                (
                    constructedMessage.TryGetTokenAtIndexOfType(0, TokenType.Middle, out Token m0)
                        && new Utf8String(m0.Sequence).Equals(new Utf8String("bar"))
                );

                Assert.True
                (
                    constructedMessage.TryGetTokenAtIndexOfType(1, TokenType.Middle, out Token m1)
                        && new Utf8String(m1.Sequence).Equals(new Utf8String("baz"))
                );

                Assert.True
                (
                    constructedMessage.TryGetTokenAtIndexOfType(2, TokenType.Middle, out Token m2)
                        && new Utf8String(m2.Sequence).Equals(new Utf8String("asdf"))
                );
            }
        }

        [Fact]
        public void SourceVerbTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("AWAY")
                .SourcePrefix("src")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":src AWAY" + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbEmptyParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("AWAY")
                .SourcePrefix("src")
                .Parameter(string.Empty)
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":src AWAY :" + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbParamsTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo bar baz asdf" + "\r\n",
                    ":coolguy foo bar baz :asdf" + "\r\n"
                );
            }
        }

        [Fact]
        public void VerbTrailingParamsTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf quux")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    "foo bar baz :asdf quux" + "\r\n"
                );
            }
        }

        [Fact]
        public void VerbParamsEmptyParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(string.Empty)
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    "foo bar baz :" + "\r\n"
                );
            }
        }

        [Fact]
        public void VerbParamsColonParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(":asdf")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    "foo bar baz ::asdf" + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbParamsTrailingParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf quux")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo bar baz :asdf quux" + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbParamsTrailingWhitespaceParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("  asdf quux ")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo bar baz :  asdf quux " + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbParamsDoubleTrailingParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("PRIVMSG")
                .Parameter("bar")
                .Parameter("lol :) ")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy PRIVMSG bar :lol :) " + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbParamsEmptyParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(string.Empty)
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo bar baz :" + "\r\n"
                );
            }
        }

        [Fact]
        public void SourceVerbTabParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("b\tar")
                .Parameter("baz")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo b\tar baz" + "\r\n",
                    ":coolguy foo b\tar :baz" + "\r\n"
                );
            }
        }

        [Fact]
        public void EmptyTagSourceVerbParamTrailingWhitespaceParamTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Tag("asd", string.Empty)
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("  ")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    @"@asd :coolguy foo bar baz :  " + "\r\n"
                );
            }
        }

        [Fact]
        public void EscapedTagVerbTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Tag("a", "b\\and\nk")
                .Tag("d", @"gh;764")
                .Verb("foo")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    @"@a=b\\and\nk;d=gh\:764 foo" + "\r\n",
                    @"@d=gh\:764;a=b\\and\nk foo" + "\r\n"
                );
            }
        }

        [Fact]
        public void EscapedTagVerbParamsTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Tag("a", "b\\and\nk")
                .Tag("d", @"gh;764")
                .Verb("foo")
                .Parameter("par1")
                .Parameter("par2")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    @"@a=b\\and\nk;d=gh\:764 foo par1 par2" + "\r\n",
                    @"@a=b\\and\nk;d=gh\:764 foo par1 :par2" + "\r\n",
                    @"@d=gh\:764;a=b\\and\nk foo par1 par2" + "\r\n",
                    @"@d=gh\:764;a=b\\and\nk foo par1 :par2" + "\r\n"
                );
            }
        }

        [Fact]
        public void LongEscapedTagVerbTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .Tag("foo", "\\\\;\\s \r\n")
                .Verb("COMMAND")
                .ConstructMessage())
            {
                AssertMessage
                (
                    constructedMessage,
                    @"@foo=\\\\\:\\s\s\r\n COMMAND" + "\r\n"
                );
            }
        }

        [Fact]
        public void NewMessageSourceTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .NewMessage()
                .Parameter("baz")
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage.Next);

                AssertMessage
                (
                    constructedMessage,
                    ":coolguy foo bar" + "\r\n"
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    ":coolguy foo baz" + "\r\n"
                );
            }
        }

        [Fact]
        public void NewMessageAddSourceTest()
        {
            Assert.Throws<ArgumentException>
            (
                () => this._factory
                    .Reset()
                    .SourcePrefix("coolguy")
                    .Verb("foo")
                    .Parameter("bar")
                    .NewMessage()
                    .SourcePrefix("coolguy1")
                    .Parameter("baz")
                    .ConstructMessage()
            );
        }

        [Fact]
        public void NewMessageTagTest()
        {
            using(Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Tag("asd", string.Empty)
                .Verb("foo")
                .Parameter("bar")
                .NewMessage()
                .Parameter("baz")
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage.Next);

                AssertMessage
                (
                    constructedMessage,
                    "@asd :coolguy foo bar" + "\r\n"
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    "@asd :coolguy foo baz" + "\r\n"
                );
            }
        }

        [Fact]
        public void NewMessageAddTagTest()
        {
            Assert.Throws<ArgumentException>
            (
                () => this._factory
                    .Reset()
                    .SourcePrefix("coolguy")
                    .Tag("asd", string.Empty)
                    .Verb("foo")
                    .Parameter("bar")
                    .NewMessage()
                    .Tag("asdf", "a")
                    .Parameter("baz")
                    .ConstructMessage()
            );
        }

        [Fact]
        public void MultipleTrailingTest()
        {
            Assert.Throws<ArgumentException>
            (
                () => this._factory
                    .Reset()
                    .SourcePrefix("coolguy")
                    .Verb("foo")
                    .Parameter("  asdf quux ")
                    .Parameter("  quux asdf")
                    .ConstructMessage()
            );
        }

        [Fact]
        public void ParameterTooLongTest()
        {
            //ensure it is too long even with prefix and verb
            string message = new string('a', 512);

            Assert.Throws<ArgumentException>
            (
                () => this._factory
                    .Reset()
                    .SourcePrefix("coolguy")
                    .Verb("PRIVMSG")
                    .Parameter("bob")
                    .Parameter(message)
                    .ConstructMessage()
            );
        }

        [Fact]
        public void TooManyParametersTest()
        {
            BaseIRCMessageFactory AddParams(BaseIRCMessageFactory fact)
            {
                //add one too many
                for(int i = 0; i <= 15; i++)
                {
                    fact.Parameter(i.ToString());
                }

                return fact;
            }

            Assert.Throws<ArgumentException>
            (
                () => AddParams
                (
                    this._factory
                        .Reset()
                        .SourcePrefix("coolguy")
                        .Verb("TAGMSG")
                )
                .ConstructMessage()
            );
        }

        [Fact]
        public void ParameterTooLongNewMessageTest()
        {
            //ensure it is too long even with prefix and verb
            string message = new string('a', 512);

            using(Token constructedMessage = this._testFactory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("PRIVMSG")
                .Parameter("bob")
                .Parameter(message)
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage);
                Assert.NotNull(constructedMessage.Next);

                Assert.True(constructedMessage.Length <= BaseIRCMessageFactory._MaxMessageLength);
                Assert.True(constructedMessage.Next.Length <= BaseIRCMessageFactory._MaxMessageLength);

                AssertMessage
                (
                    constructedMessage,
                    string.Concat
                    (
                        ":coolguy PRIVMSG bob ",
                        new string('a', 489),
                        "\r\n"
                    )
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    string.Concat
                    (
                        ":coolguy PRIVMSG bob ",
                        new string('a', 23),
                        "\r\n"
                    )
                );
            }
        }

        [Fact]
        public void TooManyParametersNewMessageTest()
        {
            TestIRCMessageFactory factory = new TestIRCMessageFactory
            (
                0,
                true,
                true
            );

            BaseIRCMessageFactory AddParams(BaseIRCMessageFactory fact)
            {
                //add one too many
                for(int i = 0; i <= 15; i++)
                {
                    fact.Parameter(i.ToString());
                }

                return fact;
            }

            using(Token constructedMessage = AddParams
            (
                factory
                    .Reset()
                    .SourcePrefix("coolguy")
                    .Verb("TAGMSG")
            )
            .ConstructMessage())
            {
                Assert.NotNull(constructedMessage);
                Assert.NotNull(constructedMessage.Next);

                AssertMessage
                (
                    constructedMessage,
                    string.Concat
                    (
                        ":coolguy TAGMSG ",
                        string.Join
                        (
                            ' ', Enumerable.Range(0, 15)
                        ),
                        "\r\n"
                    )
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    string.Concat
                    (
                        ":coolguy TAGMSG 15",
                        "\r\n"
                    )
                );
            }
        }

        [Fact]
        public void KeepMultipleParametersNewMessageTest()
        {
            TestIRCMessageFactory factory = new TestIRCMessageFactory
            (
                2,
                true,
                true
            );

            using(Token constructedMessage = factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf")
                .NewMessage()
                .Parameter("quux")
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage.Next);

                AssertMessage
                (
                    constructedMessage,
                    "foo bar baz asdf" + "\r\n"
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    "foo bar baz quux" + "\r\n"
                );
            }
        }

        [Fact]
        public void DontKeepSourceTagNewMessageTest()
        {
            TestIRCMessageFactory factory = new TestIRCMessageFactory
            (
                1,
                false,
                false
            );

            using(Token constructedMessage = factory
                .Reset()
                .SourcePrefix("coolguy")
                .Tag("asd", string.Empty)
                .Verb("foo")
                .Parameter("bar")
                .NewMessage()
                .Parameter("baz")
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage.Next);

                AssertMessage
                (
                    constructedMessage,
                    "@asd :coolguy foo bar" + "\r\n"
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    "foo bar baz" + "\r\n"
                );
            }
        }

        [Fact]
        public void ParameterTooLongSpaceSplitTest()
        {
            string message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer tincidunt odio in interdum efficitur. Fusce at fermentum augue. Pellentesque venenatis sagittis quam, sit amet porta leo consectetur nec. Ut non venenatis enim. Nulla sed mi vehicula, ultricies felis vel, blandit justo. In mauris nunc, eleifend vitae egestas nec, volutpat eu risus. Nulla sed nisi arcu. Vivamus eget luctus nisl. Fusce dignissim consequat placerat. Donec ultrices, purus quis gravida volutpat, justo arcu scelerisque elit, quis lacinia diam ex in nibh. Aliquam ultrices pulvinar sem, ac mollis metus fringilla sit amet. Nullam consectetur tortor non turpis gravida, sit amet efficitur purus dignissim. Sed arcu sapien, sodales sit amet odio at, tempor congue orci. Ut eget libero non ligula condimentum ullamcorper. Aliquam enim ligula, commodo in hendrerit ac, tincidunt eu augue.";

            using(Token constructedMessage = this._testFactory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("PRIVMSG")
                .Parameter("bob")
                .Parameter(message)
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage);
                Assert.NotNull(constructedMessage.Next);

                Assert.True(constructedMessage.Length <= BaseIRCMessageFactory._MaxMessageLength);
                Assert.True(constructedMessage.Next.Length <= BaseIRCMessageFactory._MaxMessageLength);

                AssertMessage
                (
                    constructedMessage,
                    string.Concat
                    (
                        ":coolguy PRIVMSG bob :",
                        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer tincidunt odio in interdum efficitur. Fusce at fermentum augue. Pellentesque venenatis sagittis quam, sit amet porta leo consectetur nec. Ut non venenatis enim. Nulla sed mi vehicula, ultricies felis vel, blandit justo. In mauris nunc, eleifend vitae egestas nec, volutpat eu risus. Nulla sed nisi arcu. Vivamus eget luctus nisl. Fusce dignissim consequat placerat. Donec ultrices, purus quis gravida volutpat, justo arcu",
                        "\r\n"
                    )
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    string.Concat
                    (
                        ":coolguy PRIVMSG bob :",
                        " scelerisque elit, quis lacinia diam ex in nibh. Aliquam ultrices pulvinar sem, ac mollis metus fringilla sit amet. Nullam consectetur tortor non turpis gravida, sit amet efficitur purus dignissim. Sed arcu sapien, sodales sit amet odio at, tempor congue orci. Ut eget libero non ligula condimentum ullamcorper. Aliquam enim ligula, commodo in hendrerit ac, tincidunt eu augue.",
                        "\r\n"
                    )
                );
            }
        }

        [Fact]
        public void ParameterTooLongIgnoreTagsTest()
        {
            //first ensure there is 512 bytes in tags
            static BaseIRCMessageFactory AddTags(BaseIRCMessageFactory factory)
            {
                for(int i = 'a'; i <= 'z'; i++)
                {
                    char c = (char)i;
                    factory
                        .Tag(c.ToString(), new string(c, 20));
                }

                return factory;
            }

            StringBuilder builder = new StringBuilder();

            builder.Append('@');

            for(int i = 'a'; i <= 'z'; i++)
            {
                char c = (char)i;
                builder.Append(c);
                builder.Append('=');
                builder.Append(c, 20);
                if(i != 'z')
                {
                    builder.Append(';');
                }
            }

            builder.Append(' ');
            string tags = builder.ToString();

            //ensure it is too long even with prefix and verb
            string message = new string('a', 512);

            using(Token constructedMessage = AddTags
            (
                this._testFactory
                    .Reset()
                    .SourcePrefix("coolguy")

            )
                .Verb("PRIVMSG")
                .Parameter("bob")
                .Parameter(message)
                .ConstructMessage())
            {
                Assert.NotNull(constructedMessage);
                Assert.NotNull(constructedMessage.Next);

                Assert.False(constructedMessage.Length <= BaseIRCMessageFactory._MaxMessageLength);
                Assert.False(constructedMessage.Next.Length <= BaseIRCMessageFactory._MaxMessageLength);

                AssertMessage
                (
                    constructedMessage,
                    string.Concat
                    (
                        tags,
                        ":coolguy PRIVMSG bob ",
                        new string('a', 489),
                        "\r\n"
                    )
                );

                AssertMessage
                (
                    constructedMessage.Next,
                    string.Concat
                    (
                        tags,
                        ":coolguy PRIVMSG bob ",
                        new string('a', 23),
                        "\r\n"
                    )
                );
            }
        }

        [Fact]
        public void TagsTooLongTest()
        {
            //first ensure there is over 8192 bytes in tags
            static BaseIRCMessageFactory AddTags(BaseIRCMessageFactory factory)
            {
                for(int i = 'a'; i <= 'z'; i++)
                {
                    char c = (char)i;
                    factory
                        .Tag(c.ToString(), new string(c, 20 * 16));
                }

                return factory;
            }

            Assert.Throws<ArgumentOutOfRangeException>
            (
                () => AddTags
                (
                    this._factory
                        .Reset()
                        .SourcePrefix("coolguy")

                )
                    .Verb("PRIVMSG")
                    .Parameter("bob")
                    .Parameter("bar")
                    .ConstructMessage()
            );
        }

        [Fact]
        public void SourceTooLongNewMessageTest()
        {
            //ensure it fills a single message (accounting for space)
            string message = new string('a', 509);

            Assert.Throws<ArgumentOutOfRangeException>
            (
                () => this._testFactory
                    .Reset()
                    .Parameter(message)
                    .SourcePrefix("coolguy")
                    .ConstructMessage()
            );
        }

        [Fact]
        public void VerbTooLongNewMessageTest()
        {
            //ensure it fills a single message (accounting for space)
            string message = new string('a', 509);

            Assert.Throws<ArgumentOutOfRangeException>
            (
                () => this._testFactory
                    .Reset()
                    .Parameter(message)
                    .Verb("PRIVMSG")
                    .ConstructMessage()
            );
        }

        public void Dispose()
        {
            this._factory.Dispose();
            this._testFactory.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Helper methods
        private static void AssertMessage
        (
            Token message,
            params string[] assertionStrings
        )
        {
            Utf8String result = message.ToUtf8String();

            Utf8String[] assertions = Array.ConvertAll
            (
                assertionStrings,
                static (s) => (Utf8String)s
            );

            Assert.Contains(result, assertions);
        }
        #endregion
    }
}
