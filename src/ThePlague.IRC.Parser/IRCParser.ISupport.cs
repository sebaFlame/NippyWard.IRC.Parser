using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //parse an RPL_ISUPPORT parameter sequence (without first parameter)
        public static Token ParseISupport
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseISupportToken(ref reader);

            //try to parse the rest of a ISupport list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseISupportToken,
                    TokenType.Space,
                    TokenType.ISupportSuffix,
                    TokenType.ISupportList,
                    ref reader
                )
            );

            return new Token
            (
                TokenType.ISupport,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse an ISupport token consisting of a parameter name and a possible
        //value
        private static Token ParseISupportToken
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //check if the paramater should be negated
            Token negated = ParseISupportTokenNegated(ref reader);

            //parse the parameter name
            Token parameter = ParseISupportParameter(ref reader);

            negated.Combine(parameter);

            //parse the suffix containing a value or return empty
            parameter.Combine(ParseISupportTokenSuffix(ref reader));

            return new Token
            (
                TokenType.ISupportToken,
                reader.Sequence.Slice(startPosition, reader.Position),
                negated
            );
        }

        //check if the parameter should be negated
        private static Token ParseISupportTokenNegated
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(IsTerminal(TokenType.Minus, ref reader, out _))
            {
                reader.Advance(1);

                return new Token
                (
                    TokenType.ISupportTokenNegated,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );
            }
            //can return empty
            else
            {
                return new Token
                (
                    TokenType.ISupportTokenNegated
                );
            }
        }

        //parse an ISupport parameter
        private static Token ParseISupportParameter
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            while(IsUpperCaseOrDigit(ref reader, out _))
            {
                reader.Advance(1);
            }

            return new Token
            (
                TokenType.ISupportParameter,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }

        private static Token ParseISupportTokenSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out Token equals
            ))
            {
                //can return empty
                return new Token
                (
                    TokenType.ISupportTokenSuffix
                );
            }

            equals.Combine(ParseISupportValue(ref reader));

            return new Token
            (
                TokenType.ISupportTokenSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                equals
            );
        }

        //parse a (list of) value or return empty
        private static Token ParseISupportValue
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can return empty if no value found
            if(TryParseISupportValueItem(ref reader, out Token first))
            {
                return new Token
                (
                    TokenType.ISupportValue
                );
            }

            //try to parse the rest of an ISupport value list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseISupportValueItem,
                    TokenType.Comma,
                    TokenType.ISupportValueSuffix,
                    TokenType.ISupportValueList,
                    ref reader
                )
            );

            return new Token
            (
                TokenType.ISupportValue,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        private static bool TryParseISupportValueItem
        (
            ref SequenceReader<byte> reader,
            out Token valueItem
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child;

            while(TryParseISupportValueItemTerminals
            (
                ref reader,
                out child
            ) || TryParseISupportValueItemEscape(ref reader, out child))
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

            valueItem = new Token
            (
                TokenType.ISupportValueItem,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
            );

            return true;
        }

        private static Token ParseISupportValueItem
        (
            ref SequenceReader<byte> reader
        )
        {
            if(!TryParseISupportValueItem(ref reader, out Token valueItem))
            {
                throw new ParserException("ISupport value item expected");
            }

            return valueItem;
        }

        private static bool TryParseISupportValueItemTerminals
        (
            ref SequenceReader<byte> reader,
            out Token terminals
        )
        {
            SequencePosition startPosition = reader.Position;
            int count = 0;

            while(IsValidISupportValueItemTerminal
            (
                ref reader,
                out _
            ))
            {
                count++;
            }

            if(count == 0)
            {
                terminals = null;
                return false;
            }

            reader.Advance(count);

            terminals = new Token
            (
                TokenType.ISupportValueItemTerminals,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        private static bool TryParseISupportValueItemEscape
        (
            ref SequenceReader<byte> reader,
            out Token terminals
        )
        {
            SequencePosition startPosition = reader.Position;
            Token x, escape;

            //check if it starts with a correct escape prefix
            if(TryParseTerminal
            (
                TokenType.Backslash,
                ref reader,
                out Token backslash
            ))
            {
                if(TryParseTerminal(TokenType.x, ref reader, out x)
                   || TryParseTerminal(TokenType.X, ref reader, out x))
                {
                    backslash.Combine(x);

                    //throw an exception if an unknown escape sequence is found
                    if(!(TryParseISupportValueItemEscapeBackslash
                    (
                        ref reader,
                        out escape
                    )
                    || TryParseISupportValueItemEscapeSpace
                    (
                        ref reader,
                        out escape
                    )
                    || TryParseISupportValueItemEscapeEqual
                    (
                        ref reader,
                        out escape
                    )))
                    {
                        throw new ParserException("Unknown escape sequence");
                    }

                    x.Combine(escape);

                    terminals = new Token
                    (
                        TokenType.ISupportValueItemEscape,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        backslash
                    );

                    return true;
                }
                else
                {
                    throw new ParserException("Unknown escape sequence");
                }
            }
            else
            {
                terminals = null;
                return false;
            }
        }

        //try to match an escaped backslash
        private static bool TryParseISupportValueItemEscapeBackslash
        (
            ref SequenceReader<byte> reader,
            out Token escape
        )
        {
            SequencePosition startPosition = reader.Position;

            if(MatchTerminal(TokenType.Five, ref reader)
               && (MatchTerminal(TokenType.C, ref reader)
                   || MatchTerminal(TokenType.c, ref reader)))
            {
                escape = new Token
                (
                    TokenType.ISupportValueItemEscapeBackslash,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );

                return true;
            }
            else
            {
                escape = null;
                return false;
            }
        }

        //try to match an escaped space
        private static bool TryParseISupportValueItemEscapeSpace
        (
            ref SequenceReader<byte> reader,
            out Token escape
        )
        {
            SequencePosition startPosition = reader.Position;

            if(MatchTerminal(TokenType.Two, ref reader)
               && MatchTerminal(TokenType.Zero, ref reader))
            {
                escape = new Token
                (
                    TokenType.ISupportValueItemEscapeSpace,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );

                return true;
            }
            else
            {
                escape = null;
                return false;
            }
        }

        //try to match an escaped equal
        private static bool TryParseISupportValueItemEscapeEqual
        (
            ref SequenceReader<byte> reader,
            out Token escape
        )
        {
            SequencePosition startPosition = reader.Position;

            if(MatchTerminal(TokenType.Five, ref reader)
               && (MatchTerminal(TokenType.D, ref reader)
                   || MatchTerminal(TokenType.d, ref reader)))
            {
                escape = new Token
                (
                    TokenType.ISupportValueItemEscapeEqual,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );

                return true;
            }
            else
            {
                escape = null;
                return false;
            }
        }
    }
}
