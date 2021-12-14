using System.Buffers;
using System.Runtime.CompilerServices;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //Check if the token at the current position is a terminal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTerminal
        (
            Terminal terminal,
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
        private static bool IsTerminal
        (
            Terminal terminal,
            byte value
        )
            => (byte)terminal == value;

        //Match a terminal and advance if found
        private static bool MatchTerminal
        (
            Terminal terminal,
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

        //Match one or more space and advance if found
        private static bool MatchSpaces(ref SequenceReader<byte> reader)
        {
            bool found = false;

            while(MatchTerminal(Terminal.Space, ref reader))
            {
                found = true;
                continue;
            }

            return found;
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
        private static bool IsUTF8WithoutNullCrLfSemiColonSpace
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
                || IsTerminal(Terminal.Bell, value)
                || IsTerminal(Terminal.Comma, value)
                || IsTerminal(Terminal.Colon, value)
                || IsTerminal(Terminal.AtSign, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutCTCPNullCrLFBase
        (
            ref SequenceReader<byte> reader,
            out byte value
        )
        {
            if(!reader.TryPeek(out value))
            {
                return false;
            }

            return IsUTF8WithoutCTCPNullCrLFBase(value);
        }

        //Match an UTF-8 byte excluding CTCP, NUL, CR and LF
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUTF8WithoutCTCPNullCrLFBase(byte value)
            => IsAlphaNumeric(value)
                || IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(value)
                || IsTerminal(Terminal.Bold, value)
                || IsTerminal(Terminal.Color, value)
                || IsTerminal(Terminal.HexColor, value)
                || IsTerminal(Terminal.Reset, value)
                || IsTerminal(Terminal.Monospace, value)
                || IsTerminal(Terminal.Italics, value)
                || IsTerminal(Terminal.Strikethrough, value)
                || IsTerminal(Terminal.Underline, value);

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
            => IsUTF8WithoutCTCPNullCrLFBase(value)
                || IsTerminal(Terminal.CTCP, value);

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
        //byte, NUL, CR, LF, CTCP, BEL, semicolon and at
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool
            IsUTF8WithoutAlphaNumericFormatCTCPNullCrLFBase(byte value)
            => IsNonPrintableWithoutNullLfCrBellFormatCTCP(value)
                || IsNonASCII(value)
                || IsUndefinedPunctuationMark(value)
                || IsTerminal(Terminal.ExclamationMark, value)
                || IsTerminal(Terminal.Percent, value)
                || IsTerminal(Terminal.Plus, value)
                || IsTerminal(Terminal.Minus, value)
                || IsTerminal(Terminal.Period, value)
                || IsTerminal(Terminal.Slash, value)
                || IsTerminal(Terminal.EqualitySign, value)
                || IsSpecial(value)
                || IsTerminal(Terminal.Tilde, value)
                || IsTerminal(Terminal.Asterisk, value)
                || IsTerminal(Terminal.Number, value)
                || IsTerminal(Terminal.Ampersand, value)
                || IsTerminal(Terminal.Dollar, value);

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
        private static bool 
            IsNonPrintableWithoutNullLfCrBellFormatCTCP(byte value)
            => IsTerminal(Terminal.Enquiry, value)
                || IsTerminal(Terminal.Acknowledge, value)
                || IsTerminal(Terminal.Backspace, value)
                || IsTerminal(Terminal.HorizontalTab, value)
                || IsTerminal(Terminal.VerticalTab, value)
                || IsTerminal(Terminal.FormFeed, value)
                || IsTerminal(Terminal.ShiftOut, value)
                || IsTerminal(Terminal.DataLinkEscape, value)
                || IsTerminal(Terminal.DeviceControl2, value)
                || IsTerminal(Terminal.DeviceControl3, value)
                || IsTerminal(Terminal.DeviceControl4, value)
                || IsTerminal(Terminal.NegativeAcknowledge, value)
                || IsTerminal(Terminal.Synchronize, value)
                || IsTerminal(Terminal.EndOfTransmissionBlock, value)
                || IsTerminal(Terminal.Cancel, value)
                || IsTerminal(Terminal.EndOfMedium, value)
                || IsTerminal(Terminal.Substitute, value)
                || IsTerminal(Terminal.Escape, value)
                || IsTerminal(Terminal.FileSeparator, value)
                || IsTerminal(Terminal.Delete, value);

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
            => IsTerminal(Terminal.DoubleQuote, value)
                || IsTerminal(Terminal.SingleQuote, value)
                || IsTerminal(Terminal.LeftParenthesis, value)
                || IsTerminal(Terminal.RightParenthesis, value)
                || IsTerminal(Terminal.LessThan, value)
                || IsTerminal(Terminal.GreaterThan, value)
                || IsTerminal(Terminal.QuestionMark, value);

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
            => IsTerminal(Terminal.LeftSquareBracket, value)
                || IsTerminal(Terminal.Backslash, value)
                || IsTerminal(Terminal.RightSquareBracket, value)
                || IsTerminal(Terminal.Caret, value)
                || IsTerminal(Terminal.Underscore, value)
                || IsTerminal(Terminal.Accent, value)
                || IsTerminal(Terminal.LeftCurlyBracket, value)
                || IsTerminal(Terminal.VerticalBar, value)
                || IsTerminal(Terminal.RightCurlyBracket, value);

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
            => value > (byte)Terminal.Delete;

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
            => value >= (byte)Terminal.Zero
                && value <= (byte)Terminal.Nine;

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
            => value >= (byte)Terminal.a
                && value <= (byte)Terminal.f;

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
            => value >= (byte)Terminal.A
                && value <= (byte)Terminal.F;

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
            => value >= (byte)Terminal.g
                && value <= (byte)Terminal.z;

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
            => value >= (byte)Terminal.G
                && value <= (byte)Terminal.Z;

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
                || IsTerminal(Terminal.Bell, value)
                || IsTerminal(Terminal.Semicolon, value)
                || IsTerminal(Terminal.AtSign, value);

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
    }
}
