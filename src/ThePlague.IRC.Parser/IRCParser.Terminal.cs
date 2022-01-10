using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //Check if the token at the current position is a terminal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsTerminal
        (
            TokenType terminal,
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsTerminal(terminal, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsTerminal
        (
            TokenType terminal,
            byte value
        )
            => (byte)terminal == value; //ensure only first byte gets used

        //Match a terminal and advance if found
        private static bool MatchTerminal
        (
            TokenType terminal,
            ref SequenceReader<byte> reader
        )
        {
            if(IsTerminal(terminal, ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        //Match a terminal, advance and create a token for the terminal if found 
        private static bool TryParseTerminal
        (
            TokenType terminal,
            ref SequenceReader<byte> reader,
            out Token token
        )
        {
            SequencePosition startPosition = reader.Position;

            if(IsTerminal(terminal, ref reader, out _))
            {
                reader.Advance(1);

                //create a token for the terminal
                token = new Token
                (
                    terminal,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );

                return true;
            }

            token = null;
            return false;
        }

        //Match one or more space and advance if found. It can produce a linked
        //list of multiple space terminals
        private static bool TryParseSpaces
        (
            ref SequenceReader<byte> reader,
            out Token space
        )
        {
            SequencePosition startPosition = reader.Position;
            int count = 0;
            Token previous = null, first = null;

            while(MatchTerminal(TokenType.Space, ref reader))
            {
                count++;
            }

            if(count == 0)
            {
                space = null;
                return false;
            }

            ReadOnlySequence<byte> sequence = reader.Sequence;
            int offset = (int)sequence.GetOffset(startPosition);

            for(int i = 0; i < count; i++)
            {
                //space is always a length of 1
                space = new Token
                (
                    TokenType.Space,
                    reader.Sequence.Slice(offset + i, 1)
                );

                if(first is null)
                {
                    first = space;
                }
                else
                {
                    previous.Combine(space);
                }

                previous = space;
            }

            space = first;

            return true;
        }

        //check if the current byte is an alphanumeric
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlphaNumeric
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsAlphaNumeric(value);
        }

        //Match 0-9, a-z or A-Z
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlphaNumeric(byte value)
            => IsLetter(value)
                || IsDigit(value);

        //match an alphanumeric and advance
        private static bool MatchAlphaNumeric(ref SequenceReader<byte> reader)
        {
            if(IsAlphaNumeric(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        //match an UTF-8 byte excluding NUL, CR, LF, semicolon and space.
        //Advance if found
        private static bool MatchUTF8WithoutNullCrLfSemiColonSpace
        (
            ref SequenceReader<byte> reader
        )
        {
            if(IsUTF8WithoutNullCrLfSemiColonSpace(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        //match an UTF-8 byte excluding NUL, CR, LF, semicolon and space.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUTF8WithoutNullCrLfSemiColonSpace
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutNullCrLfSemiColonSpace(value);
        }

        //check if a byte is  an UTF-8 byte excluding NUL, CR, LF, semicolon and
        //space.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLfSemiColonSpace
        (
            byte value
        )
            => IsUTF8WithoutNullCrLFBase(value)
                || IsTerminal(TokenType.Bell, value)
                || IsTerminal(TokenType.Comma, value)
                || IsTerminal(TokenType.Colon, value)
                || IsTerminal(TokenType.AtSign, value)
                || IsTerminal(TokenType.LeftSquareBracket, value)
                || IsTerminal(TokenType.RightSquareBracket, value)
                || IsTerminal(TokenType.Caret, value)
                || IsTerminal(TokenType.Underscore, value)
                || IsTerminal(TokenType.Accent, value)
                || IsTerminal(TokenType.LeftCurlyBracket, value)
                || IsTerminal(TokenType.VerticalBar, value)
                || IsTerminal(TokenType.RightCurlyBracket, value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLFBase
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutNullCrLFBase(value);
        }

        //check if a byte is an UTF-8 byte excluding NUL, CR and LF
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLFBase(byte value)
            => IsAlphaNumeric(value)
                || IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(value)
                || IsTerminal(TokenType.CTCP, value)
                || IsTerminal(TokenType.Bold, value)
                || IsTerminal(TokenType.Color, value)
                || IsTerminal(TokenType.HexColor, value)
                || IsTerminal(TokenType.Reset, value)
                || IsTerminal(TokenType.Monospace, value)
                || IsTerminal(TokenType.Italics, value)
                || IsTerminal(TokenType.Strikethrough, value)
                || IsTerminal(TokenType.Underline, value)
                || IsTerminal(TokenType.DoubleQuote, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(value);
        }

        //check if a byte is a UTF-8 byte excluding alphanumeric, any format
        //byte, NUL, CR, LF, CTCP, BEL, semicolon, at and special
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool
            IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(byte value)
            => IsNonPrintableWithoutNullLfCrBellFormatCTCP(value)
                || IsNonASCII(value)
                || IsUndefinedPunctuationMark(value)
                || IsTerminal(TokenType.ExclamationMark, value)
                || IsTerminal(TokenType.Percent, value)
                || IsTerminal(TokenType.Plus, value)
                || IsTerminal(TokenType.Minus, value)
                || IsTerminal(TokenType.Period, value)
                || IsTerminal(TokenType.Slash, value)
                || IsTerminal(TokenType.EqualitySign, value)
                || IsTerminal(TokenType.Tilde, value)
                || IsTerminal(TokenType.Asterisk, value)
                || IsTerminal(TokenType.Number, value)
                || IsTerminal(TokenType.Ampersand, value)
                || IsTerminal(TokenType.Dollar, value);

        //check if current byte is non pritable byte excluding NUL, CR, LF, any
        //format byte and CTCP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonPrintableWithoutNullLfCrBellFormatCTCP
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsNonPrintableWithoutNullLfCrBellFormatCTCP(value);
        }

        //check if a byte is non printable byte excluding NUL, CR, LF, any
        //format byte and CTCP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonPrintableWithoutNullLfCrBellFormatCTCP
        (
            byte value
        )
            => IsTerminal(TokenType.Enquiry, value)
                || IsTerminal(TokenType.Acknowledge, value)
                || IsTerminal(TokenType.Backspace, value)
                || IsTerminal(TokenType.HorizontalTab, value)
                || IsTerminal(TokenType.VerticalTab, value)
                || IsTerminal(TokenType.FormFeed, value)
                || IsTerminal(TokenType.ShiftOut, value)
                || IsTerminal(TokenType.DataLinkEscape, value)
                || IsTerminal(TokenType.DeviceControl2, value)
                || IsTerminal(TokenType.DeviceControl3, value)
                || IsTerminal(TokenType.DeviceControl4, value)
                || IsTerminal(TokenType.NegativeAcknowledge, value)
                || IsTerminal(TokenType.Synchronize, value)
                || IsTerminal(TokenType.EndOfTransmissionBlock, value)
                || IsTerminal(TokenType.Cancel, value)
                || IsTerminal(TokenType.EndOfMedium, value)
                || IsTerminal(TokenType.Substitute, value)
                || IsTerminal(TokenType.Escape, value)
                || IsTerminal(TokenType.FileSeparator, value)
                || IsTerminal(TokenType.Delete, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUndefinedPunctuationMark
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUndefinedPunctuationMark(value);
        }

        //check if a byte is a non-special/non-terminal punctuation mark
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUndefinedPunctuationMark(byte value)
            => IsTerminal(TokenType.SingleQuote, value)
                || IsTerminal(TokenType.LeftParenthesis, value)
                || IsTerminal(TokenType.RightParenthesis, value)
                || IsTerminal(TokenType.LessThan, value)
                || IsTerminal(TokenType.GreaterThan, value)
                || IsTerminal(TokenType.QuestionMark, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSpecial
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsSpecial(value);
        }

        //check if a byte is a special terminal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSpecial(byte value)
            => IsTerminal(TokenType.LeftSquareBracket, value)
                || IsTerminal(TokenType.Backslash, value)
                || IsTerminal(TokenType.RightSquareBracket, value)
                || IsTerminal(TokenType.Caret, value)
                || IsTerminal(TokenType.Underscore, value)
                || IsTerminal(TokenType.Accent, value)
                || IsTerminal(TokenType.LeftCurlyBracket, value)
                || IsTerminal(TokenType.VerticalBar, value)
                || IsTerminal(TokenType.RightCurlyBracket, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonASCII
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsNonASCII(value);
        }

        //check if a byte is outside the ASCII range
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonASCII(byte value)
            => value > (byte)TokenType.Delete;

        //match a letter and advance
        private static bool MatchLetter
        (
            ref SequenceReader<byte> reader
        )
        {
            if(IsLetter(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLetter
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsLetter(value);
        }

        //check if a byte is an uppercase or lowercase letter
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLetter(byte value)
            => IsLowerCaseHexLetter(value)
                || IsLowerCaseLetter(value)
                || IsUpperCaseHexLetter(value)
                || IsUpperCaseLetter(value);

        //match a digit and advance
        private static bool MatchDigit
        (
            ref SequenceReader<byte> reader
        )
        {
            if(IsDigit(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsDigit(value);
        }

        //check if a byte is a digit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(byte value)
            => value is >= ((byte)TokenType.Zero)
                and <= ((byte)TokenType.Nine);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowerCaseHexLetter
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsLowerCaseHexLetter(value);
        }

        //check if a byte is considered a lowercase hex letter
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowerCaseHexLetter(byte value)
            => value is >= ((byte)TokenType.a)
                and <= ((byte)TokenType.f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperCaseHexLetter
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUpperCaseHexLetter(value);
        }

        //check if a byte is considered a uppercase hex letter
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperCaseHexLetter(byte value)
            => value is >= ((byte)TokenType.A)
                and <= ((byte)TokenType.F);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowerCaseLetter
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsLowerCaseLetter(value);
        }

        //check if a letter is a lowercase letter minus hex
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowerCaseLetter(byte value)
            => value is >= ((byte)TokenType.g)
                and <= ((byte)TokenType.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperCaseLetter
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUpperCaseLetter(value);
        }

        //check if a letter is a uppercase letter minus hex
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperCaseLetter(byte value)
            => value is >= ((byte)TokenType.G)
                and <= ((byte)TokenType.Z);

        private static bool
            MatchUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon
        (
            ref SequenceReader<byte> reader
        )
        {
            if(IsUTF8WithoutNullCrLfSemiColonSpace(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon(value);
        }

        //check if a byte is a UTF-8 byte excluding alphanumeric, any format
        //byte, NUL, CR, LF and CTCP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon
        (
            byte value
        )
            => IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(value)
                || IsTerminal(TokenType.Bell, value)
                || IsTerminal(TokenType.Semicolon, value)
                || IsTerminal(TokenType.AtSign, value)
                || IsTerminal(TokenType.DoubleQuote, value)
                || IsSpecial(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLfSpaceAt
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutNullCrLfSpaceAt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLfSpaceAt
        (
            byte value
        )
            => IsUTF8WithoutNullCrLFBase(value)
                || IsTerminal(TokenType.Bell, value)
                || IsTerminal(TokenType.Comma, value)
                || IsTerminal(TokenType.Semicolon, value)
                || IsTerminal(TokenType.Colon, value)
                || IsSpecial(value);

        private static bool MatchHexDigit(ref SequenceReader<byte> reader)
        {
            if(IsHexDigit(ref reader, out _))
            {
                reader.Advance(1);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHexDigit
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsHexDigit(value);
        }

        //check if a byte can be used to construct a hex decimal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHexDigit(byte value)
            => IsDigit(value)
                || IsLowerCaseHexLetter(value)
                || IsUpperCaseHexLetter(value);

        private static bool IsUpperCaseOrDigit
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUpperCaseOrDigit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperCaseOrDigit(byte value)
            => IsDigit(value)
                || IsUpperCaseHexLetter(value)
                || IsUpperCaseLetter(value);

        private static bool IsUTF8WithoutNullBellCrLfSpaceCommaAndColon
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutNullBellCrLfSpaceCommaAndColon(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullBellCrLfSpaceCommaAndColon
        (
            byte value
        )
            => IsUTF8WithoutNullCrLFBase(value)
                || IsTerminal(TokenType.Semicolon, value)
                || IsTerminal(TokenType.AtSign, value)
                || IsSpecial(value);

        private static bool IsUTF8WithoutNullCrLfSpaceAndComma
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutNullCrLfSpaceAndComma(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutNullCrLfSpaceAndComma
        (
            byte value
        )
            => IsUTF8WithoutNullCrLFBase(value)
               || IsTerminal(TokenType.Bell, value)
               || IsTerminal(TokenType.Semicolon, value)
               || IsTerminal(TokenType.AtSign, value);

        private static bool IsValidISupportValueItemTerminal
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsValidISupportValueItemTerminal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidISupportValueItemTerminal
        (
            byte value
        )
            => IsAlphaNumeric(value)
                || (value >= (byte)TokenType.ExclamationMark
                    && value <= (byte)TokenType.Slash)
                || (value >= (byte)TokenType.Colon
                    && value <= (byte)TokenType.AtSign)
                || (value >= (byte)TokenType.RightCurlyBracket
                    && value <= (byte)TokenType.Tilde);

        private static bool IsValidDCCFileNameTerminalBase
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsValidDCCFileNameTerminalBase(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidDCCFileNameTerminalBase
        (
            byte value
        )
            => IsAlphaNumeric(value)
                || IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(value)
                || IsTerminal(TokenType.CTCP, value)
                || IsTerminal(TokenType.Bold, value)
                || IsTerminal(TokenType.Color, value)
                || IsTerminal(TokenType.HexColor, value)
                || IsTerminal(TokenType.Reset, value)
                || IsTerminal(TokenType.Monospace, value)
                || IsTerminal(TokenType.Italics, value)
                || IsTerminal(TokenType.Strikethrough, value)
                || IsTerminal(TokenType.Underline, value)
                || IsTerminal(TokenType.AtSign, value)
                || IsTerminal(TokenType.Semicolon, value)
                || IsSpecial(value);

        private static bool IsValidDCCFileNameTerminal
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsValidDCCFileNameTerminal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidDCCFileNameTerminal
        (
            byte value
        )
            => IsValidDCCFileNameTerminalBase(value)
                || IsTerminal(TokenType.Space, value);
    }
}
