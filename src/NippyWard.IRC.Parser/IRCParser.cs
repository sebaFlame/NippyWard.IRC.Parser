using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser
{
    public static partial class IRCParser
    {
        public static bool TryParse
        (
            in ReadOnlySequence<byte> sequence,
            out Token token,
            out SequencePosition examined
        )
        {
            //sequence is empty
            if(sequence.IsEmpty)
            {
                token = null;
                examined = sequence.Start;
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
                examined = sequence.End;
                return false;
            }

            //compute position including LF
            SequencePosition start = sequence.Start;
            SequencePosition end = sequence.GetPosition
            (
                1,
                lf.Value
            );

            //create a slice
            ReadOnlySequence<byte> message = sequence.Slice
            (
                start,
                end
            );
            examined = message.End;

            //create a reader from the sliced sequence
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
