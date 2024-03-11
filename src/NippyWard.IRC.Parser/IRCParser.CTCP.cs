using System;
using System.Buffers;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser
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

            ctcpMessage = Token.Create
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

            return Token.Create
            (
                TokenType.CTCPCommand,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        public static bool TryParseCTCPMiddle
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

            middle = Token.Create
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
                return Token.Create
                (
                    TokenType.CTCPParams
                );
            }

            Token suffix = ParseCTCPParamsSuffix(ref reader);
            space.Combine(suffix);

            return Token.Create
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
                return Token.Create
                (
                    TokenType.CTCPParamsSuffix
                );
            }

            return Token.Create
            (
                TokenType.CTCPParamsSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                middle
            );
        }

        public static bool TryParseCTCPParamsMiddle
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

            middle = Token.Create
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
                return Token.Create
                (
                    TokenType.CTCPMessageSuffix
                );
            }

            return Token.Create
            (
                TokenType.CTCPMessageSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                ctcp
            );
        }
    }
}
