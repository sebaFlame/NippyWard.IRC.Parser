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

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

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

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

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

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

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

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

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

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

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

        [Fact]
        public void HostTest()
        {
            ReadOnlySequence<byte> userhostSequence
                = AssertHelpers.CreateReadOnlySequence
            (
                "127.0.0.1"
            );

            SequenceReader<byte> reader = new SequenceReader<byte>(userhostSequence);

            using Token userHost = IRCParser.ParseSourcePrefixTarget(ref reader);

            //nick
            AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
            (
               userHost,
               TokenType.SourcePrefixTargetPrefix,
               "127.0.0.1"
            );
        }
    }
}
