using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //try parsing a format code
        private static bool TryParseFormat
        (
            ref SequenceReader<byte> reader,
            out Token format
        )
        {
            SequencePosition startPosition = reader.Position;
            Token child;

            //TODO: optimize!
            if(!(TryParseBold(ref reader, out child)
                || TryParseItalics(ref reader, out child)
                || TryParseUnderline(ref reader, out child)
                || TryParseStrikethrough(ref reader, out child)
                || TryParseMonospace(ref reader, out child)
                || TryParseReset(ref reader, out child)
                || TryParseColorFormat(ref reader, out child)
                || TryParseHexColorFormat(ref reader, out child)))
            {
                format = null;
                return false;
            }
            else
            {
                format = Token.Create
                (
                    TokenType.Format,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    child
                );

                return true;
            }
        }

        //try parse bold
        private static bool TryParseBold
        (
            ref SequenceReader<byte> reader,
            out Token boldFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal(TokenType.Bold, ref reader, out Token bold))
            {
                boldFormat = null;
                return false;
            }

            boldFormat = Token.Create
            (
                TokenType.BoldFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                bold
            );

            return true;
        }

        //try parse italics
        private static bool TryParseItalics
        (
            ref SequenceReader<byte> reader,
            out Token italicsFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Italics,
                ref reader,
                out Token italics
            ))
            {
                italicsFormat = null;
                return false;
            }

            italicsFormat = Token.Create
            (
                TokenType.ItalicsFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                italics
            );

            return true;
        }

        //try parse underline
        private static bool TryParseUnderline
        (
            ref SequenceReader<byte> reader,
            out Token underlineFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Underline,
                ref reader,
                out Token underline
            ))
            {
                underlineFormat = null;
                return false;
            }

            underlineFormat = Token.Create
            (
                TokenType.UnderlineFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                underline
            );

            return true;
        }

        //try parse strikthrough
        private static bool TryParseStrikethrough
        (
            ref SequenceReader<byte> reader,
            out Token strikethroughFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Strikethrough,
                ref reader,
                out Token strikethrough
            ))
            {
                strikethroughFormat = null;
                return false;
            }

            strikethroughFormat = Token.Create
            (
                TokenType.StrikethroughFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                strikethrough
            );

            return true;
        }

        //try parse monospace
        private static bool TryParseMonospace
        (
            ref SequenceReader<byte> reader,
            out Token monospaceFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Monospace,
                ref reader,
                out Token monospace))
            {
                monospaceFormat = null;
                return false;
            }

            monospaceFormat = Token.Create
            (
                TokenType.MonospaceFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                monospace
            );

            return true;
        }

        //try parse reset
        private static bool TryParseReset
        (
            ref SequenceReader<byte> reader,
            out Token resetFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Reset,
                ref reader,
                out Token reset
            ))
            {
                resetFormat = null;
                return false;
            }

            resetFormat = Token.Create
            (
                TokenType.ResetFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                reset
            );

            return true;
        }

        //try parse color format
        private static bool TryParseColorFormat
        (
            ref SequenceReader<byte> reader,
            out Token colorFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            //atleast a single color terminal is needed
            if(!TryParseTerminal(TokenType.Color, ref reader, out Token color))
            {
                colorFormat = null;
                return false;
            }

            //parse a color combination or return empty
            Token colorFormatSuffix = ParseColorCombination(ref reader);

            color.Combine(colorFormatSuffix);

            colorFormat = Token.Create
            (
                TokenType.ColorFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                color
            );

            return true;
        }

        //parse a combination of foreground / background color
        private static Token ParseColorCombination
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing the foreground color, foreground color is mandatory
            if(TryParseForegroundColor(ref reader, out Token fg))
            {
                //parse the suffix (can contaref backgroundcolor) or return empty
                Token combinationSuffix =
                    ParseColorCombinationSuffix(ref reader);

                fg.Combine(combinationSuffix);

                return Token.Create
                (
                    TokenType.ColorCombination,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    fg
                );
            }
            //else return empty
            else
            {
                return Token.Create
                (
                    TokenType.ColorCombination
                );
            }
        }

        //try parse the foreground color
        private static bool TryParseForegroundColor
        (
            ref SequenceReader<byte> reader,
            out Token fg
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse color as single or double digit
            if(!TryParseColor(ref reader, out Token color))
            {
                fg = null;
                return false;
            }

            fg = Token.Create
            (
                TokenType.ForegroundColor,
                reader.Sequence.Slice(startPosition, reader.Position),
                color
            );

            return true;
        }

        //parse backgroundcolor ref suffix or return empty
        private static Token ParseColorCombinationSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try to match a comma
            if(TryParseTerminal(TokenType.Comma, ref reader, out Token comma))
            {
                //try parse backgroudn color
                if(TryParseBackgroundColor(ref reader, out Token bg))
                {
                    comma.Combine(bg);

                    return Token.Create
                    (
                        TokenType.ColorCombinationSuffix,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        comma
                    );
                }
                //allow a comma (as text) after foreground color!
                else
                {
                    //rewind to before the comma
                    reader.Rewind(1);

                    //return empty
                    return Token.Create
                    (
                        TokenType.ColorCombinationSuffix,
                        reader.Sequence.Slice(startPosition, reader.Position)
                    );
                }
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.ColorCombinationSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );
            }
        }

        //try parse the background color
        private static bool TryParseBackgroundColor
        (
            ref SequenceReader<byte> reader,
            out Token bg
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse color as single or double digit
            if(!TryParseColor(ref reader, out Token color))
            {
                bg = null;
                return false;
            }

            bg = Token.Create
            (
                TokenType.BackgroundColor,
                reader.Sequence.Slice(startPosition, reader.Position),
                color
            );

            return true;

        }

        //try parse atleast a single digit of a color
        private static bool TryParseColor
        (
            ref SequenceReader<byte> reader,
            out Token color
        )
        {
            SequencePosition startPosition = reader.Position;

            //try match a single digit
            if(MatchDigit(ref reader))
            {
                //try parse the other digit or return empty
                Token colorSuffix = ParseColorSuffix(ref reader);

                color = Token.Create
                (
                    TokenType.ColorNumber,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    colorSuffix
                );

                return true;
            }
            else
            {
                color = null;
                return false;
            }
        }

        //parse the color suffix
        private static Token ParseColorSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //match a (2nd) digit
            if(MatchDigit(ref reader))
            {
                return Token.Create
                (
                    TokenType.ColorSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.ColorSuffix
                );
            }
        }

        //try parse a hex color format
        private static bool TryParseHexColorFormat
        (
            ref SequenceReader<byte> reader,
            out Token hexColorFormat
        )
        {
            SequencePosition startPosition = reader.Position;

            //match atleast a hexcolor terminal
            if(!TryParseTerminal
            (
                TokenType.HexColor,
                ref reader,
                out Token hexColor
            ))
            {
                hexColorFormat = null;
                return false;
            }

            //parse as hex color combination or return empty
            Token hexColorFormatSuffix = ParseHexColorCombination(ref reader);

            hexColor.Combine(hexColorFormatSuffix);

            hexColorFormat = Token.Create
            (
                TokenType.HexColorFormat,
                reader.Sequence.Slice(startPosition, reader.Position),
                hexColor
            );

            return true;
        }

        private static Token ParseHexColorCombination
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing the foreground hex color, foreground color is
            //mandatory
            if(TryParseForegroundHexColor(ref reader, out Token fg))
            {
                //try parse as hex color suffix or return empty
                Token suffix = ParseHexColorCombinationSuffix(ref reader);

                fg.Combine(suffix);

                return Token.Create
                (
                    TokenType.HexColorCombination,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    fg
                );
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.HexColorCombination
                );
            }
        }

        //try parse a hex color foreground color
        private static bool TryParseForegroundHexColor
        (
            ref SequenceReader<byte> reader,
            out Token fg
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse all 3 hex decimals as a single color
            if(!TryParseHexColor(ref reader, out Token color))
            {
                fg = null;
                return false;
            }

            fg = Token.Create
            (
                TokenType.ForegroundHexColor,
                reader.Sequence.Slice(startPosition, reader.Position),
                color
            );

            return true;
        }

        //try parse a hex color background or return empty
        private static Token ParseHexColorCombinationSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //match a comma
            if(TryParseTerminal(TokenType.Comma, ref reader, out Token comma))
            {
                if(TryParseBackgroundHexColor(ref reader, out Token bg))
                {
                    comma.Combine(bg);

                    return Token.Create
                    (
                        TokenType.HexColorCombinationSuffix,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        comma
                    );
                }
                //allow a comma as text!
                else
                {
                    reader.Rewind(1);

                    //return empty
                    return Token.Create
                    (
                        TokenType.HexColorCombinationSuffix
                    );
                }
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.HexColorCombinationSuffix
                );
            }
        }

        //try parse a hex color background
        private static bool TryParseBackgroundHexColor
        (
            ref SequenceReader<byte> reader,
            out Token bg
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse all 3 hex color decimals
            if(!TryParseHexColor(ref reader, out Token color))
            {
                bg = null;
                return false;
            }

            bg = Token.Create
            (
                TokenType.BackgroundHexColor,
                reader.Sequence.Slice(startPosition, reader.Position),
                color
            );

            return true;
        }

        //try parsing triple hex decimals as a color
        private static bool TryParseHexColor
        (
            ref SequenceReader<byte> reader,
            out Token color
        )
        {
            SequencePosition startPosition = reader.Position;
            Token firstChild, previous, child;

            //try to find atleast 1 hex decimal
            if(TryParseHexDecimal(ref reader, out child))
            {
                firstChild = child;

                //if found the next 2 are mandatory
                if(TryParseHexDecimal(ref reader, out child))
                {
                    firstChild.Combine(child);
                    previous = child;

                    if(TryParseHexDecimal(ref reader, out child))
                    {
                        previous.Combine(child);

                        color = Token.Create
                        (
                            TokenType.HexColorTriplet,
                            reader.Sequence.Slice
                            (
                                startPosition,
                                reader.Position
                            ),
                            firstChild
                        );

                        return true;
                    }
                    else
                    {
                        throw new ParserException("HexDecimal expected");
                    }
                }
                else
                {
                    throw new ParserException("HexDecimal expected");
                }
            }
            //if not found, return false
            else
            {
                color = null;
                return false;
            }
        }

        //try parse a hexdecimal
        private static bool TryParseHexDecimal
        (
            ref SequenceReader<byte> reader,
            out Token hex
        )
        {
            SequencePosition startPosition = reader.Position;

            //a hexdecimal consists of 2 hex digits
            if(MatchHexDigit(ref reader))
            {
                if(MatchHexDigit(ref reader))
                {
                    hex = Token.Create
                    (
                        TokenType.HexDecimal,
                        reader.Sequence.Slice(startPosition, reader.Position)
                    );

                    return true;
                }
                //not a hex digit! rewind
                else
                {
                    reader.Rewind(1);

                    hex = null;
                    return false;
                }
            }
            else
            {
                hex = null;
                return false;
            }
        }
    }
}
