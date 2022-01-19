using System;
using System.Buffers;

using ThePlague.Model.Core.Text;
using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static class Utf8StringTagExtensions
    {
        private static readonly byte[] _EscapeValues = new byte[]
        {
            0x5C, 0x5C, // '\'
            0x5C, 0x3A, // ':' for ';'
            0x5C, 0x73, // '\s'
            0x5C, 0x72, // '\r'
            0x5C, 0x6E  // '\n'
        };

        [ThreadStatic]
        private static TagUnescapeVisitor _TagUnescapeVisitor;

        public static Token TagEscape
        (
            this Utf8String str
        )
        {
            SequenceReader<byte> reader = new SequenceReader<byte>(str.Buffer);

            return ParseUnEscapedTagValue(ref reader);
        }

        public static Utf8String TagUnescape
        (
            this Token token
        )
        {
            TagUnescapeVisitor tagUnescapeVisitor = GetUnescapeVisitor();

            ReadOnlySequence<byte> sequence
                = tagUnescapeVisitor.CreateUnescapedSequence(token);

            return new Utf8String(sequence);
        }

        private static TagUnescapeVisitor GetUnescapeVisitor()
        {
            if(_TagUnescapeVisitor is null)
            {
                _TagUnescapeVisitor = new TagUnescapeVisitor();
            }

            _TagUnescapeVisitor.Reset();
            return _TagUnescapeVisitor;
        }

        #region Tag Escape Parser
        private static Token ParseUnEscapedTagValue
        (
            ref SequenceReader<byte> reader
        )
        {
            //try parse a tag value as a list of terminals
            if(TryParseTagValueList(ref reader, out Token tagValueList))
            {
                return Token.Create
                (
                    TokenType.TagValue,
                    tagValueList.Sequence,
                    tagValueList
                );
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.TagValue
                );
            }
        }

        private static bool TryParseTagValueList
        (
            ref SequenceReader<byte> reader,
            out Token tagValueList
        )
        {
            bool found = false;
            Token firstChild = null, previous = null, child;
            Utf8StringSequenceSegment startSegment, segment;

            //always provide a start segment
            startSegment = new Utf8StringSequenceSegment
            (
                ReadOnlyMemory<byte>.Empty
            );

            segment = startSegment;

            while(TryParseUTF8WithoutNullCrLfSemiColonSpace
            (
                ref reader,
                ref segment,
                out child
            ) || TryParseTagValueEscapeList
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
                tagValueList = null;
                return false;
            }

            tagValueList = Token.Create
            (
                TokenType.TagValueList,
                startSegment.CreateReadOnlySequence(segment),
                firstChild
            );

            return true;
        }

        private static bool TryParseUTF8WithoutNullCrLfSemiColonSpace
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token token
        )
        {
            Utf8StringSequenceSegment startSegment = segment;
            if(!IRCParser.TryParseUTF8WithoutNullCrLfSemiColonSpace
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

        private static bool TryParseTagValueEscapeList
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token tagValueEscapeList
        )
        {
            bool found = false;
            Token firstChild = null, previous = null, child;
            Utf8StringSequenceSegment startSegment = segment;

            while(TryParseTagValueEscape
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

                //could be multpile tokens
                previous = child.GetLastToken();

                found = true;
            }

            if(!found)
            {
                tagValueEscapeList = null;
                return false;
            }

            //children should've already been added to segment sequence
            tagValueEscapeList = Token.Create
            (
                TokenType.TagValueEscapeList,
                startSegment.CreateReadOnlySequence(segment),
                firstChild
            );

            return true;
        }

        //can return multiple TagValueEscape
        public static bool TryParseTagValueEscape
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token tagValueEscape
        )
        {
            Token tagValueEscapeSuffix;
            Utf8StringSequenceSegment startSegment = segment;

            if(!TryParseTagValueEscapeSuffix
            (
                ref reader,
                ref segment,
                out tagValueEscapeSuffix
            ))
            {
                tagValueEscape = null;
                return false;
            }

            Token firstBackslash = Token.Create
            (
                TokenType.Backslash,
                new ReadOnlySequence<byte>
                (
                    segment,
                    0,
                    segment,
                    1
                )
            );

            firstBackslash.Combine(tagValueEscapeSuffix);

            tagValueEscape = Token.Create
            (
                TokenType.TagValueEscape,
                startSegment.CreateReadOnlySequence(segment),
                firstBackslash
            );

            return true;
        }

        private static bool TryParseTagValueEscapeSuffix
        (
            ref SequenceReader<byte> reader,
            ref Utf8StringSequenceSegment segment,
            out Token tagValueEscapeSuffix
        )
        {
            byte value;
            TokenType tokenType;

            if(IRCParser.IsTerminal
            (
                TokenType.LineFeed,
                ref reader,
                out value)
            )
            {
                segment = AddEscapedValue(ref segment, 8);
                tokenType = TokenType.TagValueEscapeLf;
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.CarriageReturn,
                value
            ))
            {
                segment = AddEscapedValue(ref segment, 6);
                tokenType = TokenType.TagValueEscapeCr;
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.Space,
                value
            ))
            {
                segment = AddEscapedValue(ref segment, 4);
                tokenType = TokenType.TagValueEscapeSpace;
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.Semicolon,
                value
            ))
            {
                segment = AddEscapedValue(ref segment, 2);
                tokenType = TokenType.TagValueEscapeSemicolon;
            }
            else if(IRCParser.IsTerminal
            (
                TokenType.Backslash,
                value
            ))
            {
                segment = AddEscapedValue(ref segment, 0);
                tokenType = TokenType.TagValueEscapeBackslash;
            }
            else
            {
                tagValueEscapeSuffix = null;
                return false;
            }

            reader.Advance(1);

            Token child = Token.Create
            (
                tokenType,
                new ReadOnlySequence<byte>
                (
                    segment,
                    1,                      //skip first backslash
                    segment,
                    segment.Memory.Length
                )
            );

            tagValueEscapeSuffix = Token.Create
            (
                TokenType.TagValueEscapeSuffix,
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
            //add full escaped value
            return segment.AddNewSequenceSegment
            (
                //add full escaped sequence at once
                new ReadOnlyMemory<byte>(_EscapeValues, startIndex, 2)
            );
        }

        #endregion
    }
}
