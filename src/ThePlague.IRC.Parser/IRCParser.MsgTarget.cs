using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        public static Token ParseMsgTarget
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseMsgTo(ref reader);

            //try to parse the rest of a msgtarget list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseMsgTo,
                    TokenType.Comma,
                    TokenType.MsgTargetSuffix,
                    TokenType.MsgTargetItems,
                    ref reader
                )
            );

            return new Token
            (
                TokenType.MsgTarget,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        /* parse a single MsgTo item
         * nickname[!username@host]
         * username[%host][@servername]
         *   with username restricted to nickname terminals
         * [~&@+%]{#+&{!A1A1A}}channel */
        private static Token ParseMsgTo
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token msgTo;

            if(!(TryParseMsgToChannel(ref reader, out msgTo)
                 || TryParseMsgToTargetMask(ref reader, out msgTo)))
            {
                msgTo = ParseMsgToNickname(ref reader);
            }

            return new Token
            (
                TokenType.MsgTo,
                reader.Sequence.Slice(startPosition, reader.Position),
                msgTo
            );
        }

        //parse a channel with or without membership
        private static bool TryParseMsgToChannel
        (
            ref SequenceReader<byte> reader,
            out Token channel
        )
        {
            SequencePosition startPosition = reader.Position;

            /* Try to find a channel prefix
             * - this can be a single prefix, eg #
             * - or a prefix prefixed with membership, eg +# */
            if(!TryParseMsgToChannelPrefix(ref reader, out Token prefix))
            {
                channel = null;
                return false;
            }

            //parse the channel string
            Token channelString = ParseChannelString(ref reader);

            prefix.Combine(channelString);

            channel = new Token
            (
                TokenType.MsgToChannel,
                reader.Sequence.Slice(startPosition, reader.Position),
                prefix
            );

            return true;
        }

        //try to parse a combination of channel prefixes
        private static bool TryParseMsgToChannelPrefix
        (
            ref SequenceReader<byte> reader,
            out Token prefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token channelPrefix;

            //try parse a prefix (or membership) of '&' or '+'
            if(TryParseMsgToChannelChannelPrefixMembership
            (
                ref reader,
                out channelPrefix
            ))
            {
                prefix = new Token
                (
                    TokenType.MsgToChannelPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }
            //try parse a channel membership of '@', '%' or '~'
            else if(TryParseChannelMembershipPrefixWithoutChannelPrefix
            (
                ref reader,
                out channelPrefix
            ))
            {
                //try parse a channel prefix of '#' or '!A1A1A'
                if(!TryParseChannelPrefixWithoutMembership
                (
                    ref reader,
                    out Token prefixWithoutMembership
                ))
                {
                    throw new ParserException("Channel prefix expected");
                }

                channelPrefix.Combine(prefixWithoutMembership);

                prefix = new Token
                (
                    TokenType.MsgToChannelPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }
            //try parse a channel prefix of '#' or '!A1A1A'
            else if(TryParseChannelPrefixWithoutMembership
            (
                ref reader,
                out channelPrefix
            ))
            {
                prefix = new Token
                (
                    TokenType.MsgToChannelPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }
            else
            {
                prefix = null;
                return false;
            }
        }

        private static bool TryParseMsgToChannelChannelPrefixMembership
        (
            ref SequenceReader<byte> reader,
            out Token prefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token channelPrefix;

            if(TryParseTerminal
            (
                TokenType.Ampersand,
                ref reader,
                out channelPrefix
            ) || TryParseTerminal
                (
                    TokenType.Plus,
                    ref reader,
                    out channelPrefix
                ))
            {
                if(TryParseMsgToChannelPrefix(ref reader, out Token nextPrefix))
                {
                    channelPrefix.Combine(nextPrefix);
                }

                prefix = new Token
                (
                    TokenType.MsgToChannelChannelPrefixMembership,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }

            prefix = null;
            return false;
        }

        private static bool TryParseChannelMembershipPrefixWithoutChannelPrefix
        (
            ref SequenceReader<byte> reader,
            out Token prefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token channelPrefix;

            if(TryParseTerminal
            (
                TokenType.Tilde,
                ref reader,
                out channelPrefix
            ) || TryParseTerminal
                (
                    TokenType.AtSign,
                    ref reader,
                    out channelPrefix
                ) || TryParseTerminal
                (
                    TokenType.Percent,
                    ref reader,
                    out channelPrefix
                ))
            {
                prefix = new Token
                (
                    TokenType.ChannelMembershipPrefixWithoutChannelPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }

            prefix = null;
            return false;
        }

        //try to parse a channel membership prefix
        private static bool TryParseChannelMembershipPrefix
        (
            ref SequenceReader<byte> reader,
            out Token prefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token channelPrefix;

            if(TryParseTerminal
            (
                TokenType.Ampersand,
                ref reader,
                out channelPrefix
            )
                || TryParseTerminal
                (
                    TokenType.Plus,
                    ref reader,
                    out channelPrefix
                )
                || TryParseChannelMembershipPrefixWithoutChannelPrefix
                (
                    ref reader,
                    out channelPrefix
                ))
            {
                prefix = new Token
                (
                    TokenType.ChannelMembershipPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelPrefix
                );

                return true;
            }

            prefix = null;
            return false;
        }

        //parse a target mask
        private static bool TryParseMsgToTargetMask
        (
            ref SequenceReader<byte> reader,
            out Token targetMask
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Dollar,
                ref reader,
                out Token dollar
            ))
            {
                targetMask = null;
                return false;
            }

            dollar.Combine(ParseMsgToTargetMaskLetters(ref reader));

            targetMask = new Token
            (
                TokenType.MsgToTargetMask,
                reader.Sequence.Slice(startPosition, reader.Position),
                dollar
            );

            return true;
        }

        private static Token ParseMsgToTargetMaskLetters
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //check valid key terminals
            while(IsUTF8WithoutNullCrLfSpaceAndComma
            (
                ref reader,
                out _
            ))
            {
                reader.Advance(1);
            }

            return new Token
            (
                TokenType.MsgToTargetMaskLetters,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }

        //parse a nickname with or without suffix
        private static Token ParseMsgToNickname
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token nickName = ParseNickname(ref reader);

            nickName.Combine(ParseMsgToNicknameSuffix(ref reader));

            return new Token
            (
                TokenType.MsgToNickname,
                reader.Sequence.Slice(startPosition, reader.Position),
                nickName
            );
        }

        //parse the nickname suffix
        private static Token ParseMsgToNicknameSuffix
        (
             ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseUserHostUsername(ref reader, out Token username))
            {
                if(TryParseUserHostUsername(ref reader, out Token hostName))
                {
                    username.Combine(hostName);

                    return new Token
                    (
                        TokenType.MsgToNicknameSuffix,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        username
                    );
                }
                else
                {
                    throw new ParserException("Host expected");
                }
            }

            Token userHost = ParseMsgToUserHost(ref reader);

            userHost.Combine(ParseMsgToUserHostServer(ref reader));

            return new Token
            (
                TokenType.MsgToNicknameSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                userHost
            );
        }

        //parse the user host or return empty
        private static Token ParseMsgToUserHost
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Percent,
                ref reader,
                out Token percent
            ))
            {
                //return empty
                return new Token
                (
                    TokenType.MsgToUserHost
                );
            }

            percent.Combine(ParseHost(ref reader));

            return new Token
            (
                TokenType.MsgToUserHost,
                reader.Sequence.Slice(startPosition, reader.Position),
                percent
            );
        }

        //parse the user server or return empty
        private static Token ParseMsgToUserHostServer
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.AtSign,
                ref reader,
                out Token at
            ))
            {
                //return empty
                return new Token
                (
                    TokenType.MsgToUserHostServer
                );
            }

            at.Combine(ParseServerName(ref reader));

            return new Token
            (
                TokenType.MsgToUserHostServer,
                reader.Sequence.Slice(startPosition, reader.Position),
                at
            );
        }
    }
}
