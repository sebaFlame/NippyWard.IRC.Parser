using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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

            SequenceReader<byte> sequenceReader
                = new SequenceReader<byte>(sequence);

            //check reader if it might contain a message
            ReadOnlySequence<byte> message;
            if(!sequenceReader.TryReadTo
            (
                out message,
                (byte)Terminal.LineFeed,
                true
            ))
            {
                token = null;
                return false;
            }

            //initialize new reader alligned to the "message"
            sequenceReader = new SequenceReader<byte>
            (
                sequence.Slice(sequence.Start, message.Length + 1)
            );

            token = ParseMessage(ref sequenceReader);
            return true;
        }

        //combine 2 tokens as linked list and return currently added item
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Token Combine(Token left, Token right)
        {
            if(left is null)
            {
                return right;
            }

            left.Next = right;
            right.Previous = left;

            return right;
        }
    }
}
