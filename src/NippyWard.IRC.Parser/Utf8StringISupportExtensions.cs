using System;
using System.Buffers;

using NippyWard.Model.Core.Text;
using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser
{
    public static class Utf8StringISupportExtensions
    {
        private static readonly byte[] _EscapeValues = new byte[]
        {
            0x5C, 0x78, 0x35, 0x43, // '\'
            0x5C, 0x78, 0x32, 0x30, // ' '
            0x5C, 0x78, 0x33, 0x44  // '="
        };

        [ThreadStatic]
        private static ISupportUnescapeVisitor _ISupportUnescapeVisitor;

        public static Token ISupportEscape
        (
            this Utf8String str
        )
        {
            SequenceReader<byte> reader = new SequenceReader<byte>(str.Buffer);
            return ParseUnescapedISupportValueItem(ref reader);
        }

        public static Utf8String ISupportUnescape
        (
            this Token token
        )
        {
            ISupportUnescapeVisitor isupportUnescapeVisitor
                = GetUnescapeVisitor();

            ReadOnlySequence<byte> sequence
                = isupportUnescapeVisitor.CreateUnescapedSequence(token);

            return new Utf8String(sequence);
        }

        private static ISupportUnescapeVisitor GetUnescapeVisitor()
        {
            if(_ISupportUnescapeVisitor is null)
            {
                _ISupportUnescapeVisitor = new ISupportUnescapeVisitor();
            }

            _ISupportUnescapeVisitor.Reset();
            return _ISupportUnescapeVisitor;
        }

        //most parts are directly copied from IRCParser.ISupport
        #region ISupport escape parser
        private static Token ParseUnescapedISupportValueItem
        (
            ref SequenceReader<byte> reader
        )
        {
            Utf8StringSequenceSegment startSegment, segment;

            //always provide a start segment
            startSegment = new Utf8StringSequenceSegment
            (
                ReadOnlyMemory<byte>.Empty
            );

            segment = startSegment;

            //can return empty of no value found
            if(!TryParseISupportValueItem
            (
                ref reader,
                ref segment,
                out Token first
            ))
            {
                return Token.Create
                (
                    TokenType.ISupportValueItem
                );
            }
            else
            {
                return Token.Create
                (
                    TokenType.ISupportValueItem,
                    startSegment.CreateReadOnlySequence(segment),
                    first
                );
            }
        }

        private static bool TryParseISupportValueItem
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token valueItem
        )
        {
            bool found = false;
            Token firstChild = null, previous = null, child;
            Utf8StringSequenceSegment startSegment = segment;

            while(TryParseISupportValueItemTerminals
            (
                ref reader,
                ref segment,
                out child
            ) || TryParseISupportValueItemEscape
            (
                ref reader,
                ref segment,
                out child
            ))
            {
                if(firstChild is null)
                {
                    firstChild = child;
                }

                previous.Combine(child);

                previous = child;

                found = true;
            }

            if(!found)
            {
                valueItem = null;
                return false;
            }

            valueItem = Token.Create
            (
                TokenType.ISupportValueItem,
                startSegment.CreateReadOnlySequence(segment),
                firstChild
            );

            return true;
        }

        private static bool TryParseISupportValueItemTerminals
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token token
        )
        {
            Utf8StringSequenceSegment startSegment = segment;

            if(!IRCParser.TryParseISupportValueItemTerminals
            (
                ref reader,
                out Token child
            ))
            {
                token = null;
                return false;
            }

            //copy sequence into new segments
            segment = segment.AddNewSequenceSegment(child.Sequence);

            //create new token from segments
            token = Token.Create
            (
                child.TokenType,
                startSegment.CreateReadOnlySequence(segment)
            );

            return true;
        }

        private static bool TryParseISupportValueItemEscape
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token terminals
        )
        {
            byte value;
            TokenType tokenType;

            //check if it starts with a correct escape prefix
            if(IRCParser.IsTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out value
            ))
            {
                tokenType = TokenType.ISupportValueItemEscapeEqual;
                segment = AddEscapedValue(ref segment, 8);
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.Backslash,
                value
            ))
            {
                tokenType = TokenType.ISupportValueItemEscapeBackslash;
                segment = AddEscapedValue(ref segment, 0);
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.Space,
                value
            ))
            {
                tokenType = TokenType.ISupportValueItemEscapeSpace;
                segment = AddEscapedValue(ref segment, 4);
            }
            else
            {
                terminals = null;
                return false;
            }

            reader.Advance(1);

            Token child = Token.Create
            (
                tokenType,
                new ReadOnlySequence<byte>
                (
                    segment,
                    0,
                    segment,
                    segment.Memory.Length
                )
            );

            terminals = Token.Create
            (
                TokenType.ISupportValueItemEscape,
                child.Sequence,
                child
            );

            return true;
        }

        private static Utf8StringSequenceSegment AddEscapedValue
        (
            ref Utf8StringSequenceSegment segment,
            int startIndex
        )
        {
            return segment.AddNewSequenceSegment
            (
                new ReadOnlyMemory<byte>(_EscapeValues, startIndex, 4)
            );
        }
        #endregion
    }
}

