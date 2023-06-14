using System;
using System.Buffers;

using Xunit;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser.Tests
{
    public class SourcePrefixTests
    {
        [Fact]
        public void NickTest()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence("coolguy");

            using Token userHost = IRCParser.ParseUserHost(in userhostSequence);

            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "coolguy"
            );
        }

        [Fact]
        public void UserHostTest_1()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence
            (
                "coolguy!ag@127.0.0.1"
            );

            using Token userHost = IRCParser.ParseUserHost(in userhostSequence);

            //nick
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "coolguy"
            );

            //user
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Username,
               "ag"
            );

            //host
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Host,
               "127.0.0.1"
            );
        }

        [Fact]
        public void UserHostTest_2()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence
            (
                "coolguy!~ag@localhost"
            );

            using Token userHost = IRCParser.ParseUserHost(in userhostSequence);

            //nick
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "coolguy"
            );

            //user
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Username,
               "~ag"
            );

            //host
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Host,
               "localhost"
            );
        }

        [Fact]
        public void PartialUserHostTest_1()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence
            (
                "coolguy@127.0.0.1"
            );

            using Token userHost = IRCParser.ParseUserHost(in userhostSequence);

            //nick
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "coolguy"
            );

            //user
            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
               userHost,
               TokenType.SourcePrefixUsername
            );

            //host
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Host,
               "127.0.0.1"
            );
        }

        [Fact]
        public void PartialUserHostTest_2()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence
            (
                "coolguy!ag"
            );

            using Token userHost = IRCParser.ParseUserHost(in userhostSequence);

            //nick
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "coolguy"
            );

            //user
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.Username,
               "ag"
            );

            //host
            AssertHelpers.AssertFirstOfTokenTypeIsEmpty
            (
               userHost,
               TokenType.SourcePrefixHostname
            );
        }
    }
}
