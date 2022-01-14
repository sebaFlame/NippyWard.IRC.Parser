using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //try to parse a CTCP message
        private static bool TryParseCTCPMessage
        (
            ref SequenceReader<byte> reader,
            out Token ctcpMessage
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal(TokenType.CTCP, ref reader, out Token ctcp))
            {
                ctcpMessage = null;
                return false;
            }

            ctcp.Combine(ParseCTCPCommand(ref reader))
                .Combine(ParseCTCPParams(ref reader))
                .Combine(ParseCTCPMessageSuffix(ref reader));

            ctcpMessage = new Token
            (
                TokenType.CTCPMessage,
                reader.Sequence.Slice(startPosition, reader.Position),
                ctcp
            );

            return true;
        }

        private static Token ParseCTCPCommand
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseCTCPMiddle(ref reader, out Token middle))
            {
                throw new ParserException("CTCP middle expected");
            }

            return new Token
            (
                TokenType.CTCPCommand,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        private static bool TryParseCTCPMiddle
        (
            ref SequenceReader<byte> reader,
            out Token middle
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null, token;

            while(TryParseMiddlePrefixList(ref reader, out token)
                  || TryParseTerminal(TokenType.Colon, ref reader, out token))
            {
                previous = previous.Combine(token);

                if(first is null)
                {
                    first = previous;
                }
            }

            if(first is null)
            {
                middle = null;
                return false;
            }

            middle = new Token
            (
                TokenType.CTCPMiddle,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        //parse CTCP params or return empty
        private static Token ParseCTCPParams
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can return empty
            if(!TryParseTerminal
            (
                TokenType.Space,
                ref reader,
                out Token space
            ))
            {
                return new Token
                (
                    TokenType.CTCPParams
                );
            }

            Token suffix = ParseCTCPParamsSuffix(ref reader);
            space.Combine(suffix);

            return new Token
            (
                TokenType.CTCPParams,
                reader.Sequence.Slice(startPosition, reader.Position),
                space
            );
        }

        //try to parse actual CTCP parameters or return empty
        private static Token ParseCTCPParamsSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseCTCPParamsMiddle(ref reader, out Token middle))
            {
                return new Token
                (
                    TokenType.CTCPParamsSuffix
                );
            }

            return new Token
            (
                TokenType.CTCPParamsSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        private static bool TryParseCTCPParamsMiddle
        (
            ref SequenceReader<byte> reader,
            out Token middle
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null, token;

            while(TryParseCTCPMiddle(ref reader, out token)
                  || TryParseTerminal(TokenType.Space, ref reader, out token))
            {
                previous = previous.Combine(token);

                if(first is null)
                {
                    first = previous;
                }
            }

            if(first is null)
            {
                middle = null;
                return false;
            }

            middle = new Token
            (
                TokenType.CTCPParamsMiddle,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        //try to parse a CTPC marker or return empty
        private static Token ParseCTCPMessageSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal(TokenType.CTCP, ref reader, out Token ctcp))
            {
                return new Token
                (
                    TokenType.CTCPMessageSuffix
                );
            }

            return new Token
            (
                TokenType.CTCPMessageSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                ctcp
            );
        }

        //parse any type of DCC message parameters
        public static Token ParseDCCMessage
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token spaces;

            Token type = ParseDCCType(ref reader);
            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            type.Combine(spaces);

            //first (quoted) argument
            Token quoted = ParseDCCQuotedArgument(ref reader);
            spaces.Combine(quoted);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            quoted.Combine(spaces);

            //2nd argument
            Token argument = ParseDCCArgument(ref reader);
            spaces.Combine(argument);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            argument.Combine(spaces);

            //3rd argument
            argument = ParseDCCArgument(ref reader);
            spaces.Combine(argument);

            return new Token
            (
                TokenType.DCCMessage,
                reader.Sequence.Slice(startPosition, reader.Position),
                type
            );
        }

        //parse a DCC type (SEND, SSEND, CHAT, SCHAT, RESUME, ACCEPT)
        private static Token ParseDCCType
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseCTCPMiddle(ref reader, out Token middle))
            {
                throw new ParserException("DCC type expected");
            }

            return new Token
            (
                TokenType.DCCType,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        //parse a DCC argument
        private static Token ParseDCCArgument
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseCTCPMiddle(ref reader, out Token middle))
            {
                throw new ParserException("DCC argument expected");
            }

            return new Token
            (
                TokenType.DCCArgument,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        //try to parse a quoted or regualer argument
        private static Token ParseDCCQuotedArgument
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token argument;

            if(TryParseDCCQuotedFilename(ref reader, out argument))
            {
                return new Token
                (
                    TokenType.DCCQuotedArgument,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    argument
                );
            }

            argument = ParseDCCFilenameList(ref reader);

            return new Token
            (
                TokenType.DCCQuotedArgument,
                reader.Sequence.Slice(startPosition, reader.Position),
                argument
            );
        }

        //try to parse a filename without spaces
        private static Token ParseDCCFilenameList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            while(IsValidDCCFileNameTerminalBase(ref reader, out _))
            {
                reader.Advance(1);
            }

            return new Token
            (
                TokenType.DCCFilenameList,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }

        //try to parse a quoted filename (path) containing spaces
        private static bool TryParseDCCQuotedFilename
        (
            ref SequenceReader<byte> reader,
            out Token quotedFilename
        )
        {
            SequencePosition startPosition = reader.Position;
            Token first, doubleQuote;

            if(!TryParseTerminal
            (
                TokenType.DoubleQuote,
                ref reader,
                out doubleQuote
            ))
            {
                quotedFilename = null;
                return false;
            }

            first = doubleQuote;

            Token filename = ParseDCCFilenameSpaceList(ref reader);
            first.Combine(filename);

            if(!TryParseTerminal
            (
                TokenType.DoubleQuote,
                ref reader,
                out doubleQuote
            ))
            {
                throw new ParserException("Double quotes expected");
            }

            filename.Combine(doubleQuote);

            quotedFilename = new Token
            (
                TokenType.DCCQuotedFilename,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        //try to parse a filename (path) containing spaces
        private static Token ParseDCCFilenameSpaceList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            while(IsValidDCCFileNameTerminal(ref reader, out _))
            {
                reader.Advance(1);
            }

            return new Token
            (
                TokenType.DCCFilenameSpaceList,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }
    }
}
