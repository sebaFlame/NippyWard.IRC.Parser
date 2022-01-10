using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        public static bool TryParse
        (
            in ReadOnlySequence<byte> sequence,
            out Token token
        )
        {
            //sequence is empty
            if(sequence.IsEmpty)
            {
                token = null;
                return false;
            }

            //check if sequence contains a LineFeed (a full message)
            SequencePosition? lf = sequence.PositionOf
            (
                (byte)TokenType.LineFeed
            );

            if(!lf.HasValue)
            {
                token = null;
                return false;
            }

            ReadOnlySequence<byte> message = sequence.Slice
            (
                0,
                sequence.GetOffset(lf.Value) + 1 //slice WITH LineFeed included
            );

            SequenceReader<byte> sequenceReader
                = new SequenceReader<byte>(message);

            token = ParseMessage(ref sequenceReader);
            return true;
        }

        public static Token ParseUserHost(in ReadOnlySequence<byte> sequence)
        {
            SequenceReader<byte> sequenceReader
                = new SequenceReader<byte>(sequence);

            return ParseSourcePrefixTarget(ref sequenceReader);
        }
    }
}
