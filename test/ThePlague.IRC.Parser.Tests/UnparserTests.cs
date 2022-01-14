using System;

using Xunit;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    //TODO:
    // - max message length
    // - max tag length
    // - max paramters
    public class UnparserTests : IDisposable
    {
        private readonly GeneralIRCMessageFactory _factory;

        public UnparserTests()
        {
            this._factory = new GeneralIRCMessageFactory();
        }

        [Fact]
        public void VerbParamsTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                "foo bar baz asdf" + "\r\n",
                "foo bar baz :asdf" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("AWAY")
                .SourcePrefix("src")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":src AWAY" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbEmptyParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("AWAY")
                .SourcePrefix("src")
                .Parameter(string.Empty)
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":src AWAY :" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbParamsTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy foo bar baz asdf" + "\r\n",
                ":coolguy foo bar baz :asdf" + "\r\n"
            );
        }

        [Fact]
        public void VerbTrailingParamsTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf quux")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                "foo bar baz :asdf quux" + "\r\n"
            );
        }

        [Fact]
        public void VerbParamsEmptyParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(string.Empty)
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                "foo bar baz :" + "\r\n"
            );
        }

        [Fact]
        public void VerbParamsColonParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(":asdf")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                "foo bar baz ::asdf" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbParamsTrailingParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("asdf quux")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy foo bar baz :asdf quux" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbParamsTrailingWhitespaceParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("  asdf quux ")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy foo bar baz :  asdf quux " + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbParamsDoubleTrailingParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("PRIVMSG")
                .Parameter("bar")
                .Parameter("lol :) ")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy PRIVMSG bar :lol :) " + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbParamsEmptyParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter(string.Empty)
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy foo bar baz :" + "\r\n"
            );
        }

        [Fact]
        public void SourceVerbTabParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("b\tar")
                .Parameter("baz")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                ":coolguy foo b\tar baz" + "\r\n",
                ":coolguy foo b\tar :baz" + "\r\n"
            );
        }

        [Fact]
        public void EmptyTagSourceVerbParamTrailingWhitespaceParamTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Tag("asd", string.Empty)
                .SourcePrefix("coolguy")
                .Verb("foo")
                .Parameter("bar")
                .Parameter("baz")
                .Parameter("  ")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                @"@asd :coolguy foo bar baz :  " + "\r\n"
            );
        }

        [Fact]
        public void EscapedTagVerbTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Tag("a", "b\\and\nk")
                .Tag("d", @"gh;764")
                .Verb("foo")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                @"@a=b\\and\nk;d=gh\:764 foo" + "\r\n",
                @"@d=gh\:764;a=b\\and\nk foo" + "\r\n"
            );
        }

        [Fact]
        public void EscapedTagVerbParamsTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Tag("a", "b\\and\nk")
                .Tag("d", @"gh;764")
                .Verb("foo")
                .Parameter("par1")
                .Parameter("par2")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                @"@a=b\\and\nk;d=gh\:764 foo par1 par2" + "\r\n",
                @"@a=b\\and\nk;d=gh\:764 foo par1 :par2" + "\r\n",
                @"@d=gh\:764;a=b\\and\nk foo par1 par2" + "\r\n",
                @"@d=gh\:764;a=b\\and\nk foo par1 :par2" + "\r\n"
            );
        }

        [Fact]
        public void LongEscapedTagVerbTest()
        {
            Token constructedMessage = this._factory
                .Reset()
                .Tag("foo", "\\\\;\\s \r\n")
                .Verb("COMMAND")
                .ConstructMessage();

            AssertMessage
            (
                constructedMessage,
                @"@foo=\\\\\:\\s\s\r\n COMMAND" + "\r\n"
            );
        }

        public void Dispose()
            => this._factory.Dispose();

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
