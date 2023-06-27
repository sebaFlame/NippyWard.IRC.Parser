using System;
using System.Buffers;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser
{
    public delegate Token ParseToken(ref SequenceReader<byte> reader);

    public static partial class IRCParser
    {
        //pin certain item parse method to prevent excessive GC
        private static readonly ParseToken _ParseKeyListItem
            = new ParseToken(ParseKeyListItem);
        private static readonly ParseToken _ParseNickname
            = new ParseToken(ParseNickname);
        private static readonly ParseToken _ParseChannel
            = new ParseToken(ParseChannel);
        private static readonly ParseToken _ParseElistCond
            = new ParseToken(ParseElistCond);
        private static readonly ParseToken _ParseMsgTo
            = new ParseToken(ParseMsgTo);
        private static readonly ParseToken _ParseISupportToken
            = new ParseToken(ParseISupportToken);
        private static readonly ParseToken _ParseISupportValue
            = new ParseToken(ParseISupportValue);
        private static readonly ParseToken _ParseCapListItem
            = new ParseToken(ParseCapListItem);
        private static readonly ParseToken _ParseCapListItemValueListItem
            = new ParseToken(ParseCapListItemValueListItem);
        private static readonly ParseToken _ParseNicknameMembership
            = new ParseToken(ParseNicknameMembership);
        private static readonly ParseToken _ParseMsgToChannel
            = new ParseToken(ParseMsgToChannel);
        private static readonly ParseToken _ParseUserHostListItem
            = new ParseToken(ParseUserHostListItem);

        //parse a unix timestamp
        public static Token ParseTimestamp
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseInteger(ref reader, out Token integer))
            {
                throw new ParserException("Integer expected");
            }

            return Token.Create
            (
                TokenType.Timestamp,
                reader.Sequence.Slice(startPosition, reader.Position),
                integer
            );
        }

        //parse a servername (which is a host)
        public static Token ParseServerName
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token host = ParseHost(ref reader);

            return Token.Create
            (
                TokenType.ServerName,
                reader.Sequence.Slice(startPosition, reader.Position),
                host
            );
        }

        //parse a sequence of mode strings
        public static bool TryParseModeStringList
        (
            ref SequenceReader<byte> reader,
            out Token modeStringList
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null, modeString;

            while(TryParseModeString(ref reader, out modeString))
            {
                previous = previous.Combine(modeString);

                if(first is null)
                {
                    first = previous;
                }
            }

            if(first is null)
            {
                modeStringList = null;
                return false;
            }

            modeStringList = Token.Create
            (
                TokenType.ModeStringList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
            return true;
        }

        //parse a full modestring
        private static bool TryParseModeString
        (
            ref SequenceReader<byte> reader,
            out Token modeString
        )
        {
            SequencePosition startPosition = reader.Position;
            Token prefix;

            if(TryParseTerminal
            (
                TokenType.Plus,
                ref reader,
                out prefix
            )
                || TryParseTerminal
                (
                    TokenType.Minus,
                    ref reader,
                    out prefix
                ))
            {
                Token modeChars = ParseModeChars(ref reader);

                prefix.Combine(modeChars);

                modeString = Token.Create
                (
                    TokenType.ModeString,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    prefix
                );

                return true;
            }

            modeString = null;
            return false;
        }

        //parse a sequence of modes
        private static Token ParseModeChars
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can return empty
            if(!TryParseModeCharsList(ref reader, out Token modeChars))
            {
                return Token.Create
                (
                    TokenType.ModeChars
                );
            }
            else
            {
                return Token.Create
                (
                    TokenType.ModeChars,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    modeChars
                );
            }
        }

        private static bool TryParseModeCharsList
        (
            ref SequenceReader<byte> reader,
            out Token modeCharsList
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null, letter;

            while(TryParseMode(ref reader, out letter))
            {
                previous = previous.Combine(letter);

                if(first is null)
                {
                    first = previous;
                }
            }

            if(first is null)
            {
                modeCharsList = null;
                return false;
            }

            modeCharsList = Token.Create
            (
                TokenType.ModeCharsList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        //parse a single mode (a letter)
        private static bool TryParseMode
        (
            ref SequenceReader<byte> reader,
            out Token mode
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;
            Token letter;

            if(IsLetter(ref reader, out value)
               && TryParseTerminal((TokenType)value, ref reader, out letter))
            {
                mode = Token.Create
                (
                    TokenType.Mode,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    letter
                );

                return true;
            }

            mode = null;
            return false;
        }

        //parse a list of 1 or more keys (eg for JOIN command)
        public static Token ParseKeyList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseKeyListItem(ref reader);

            //try to parse the rest of a key list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseKeyListItem,
                    TokenType.Comma,
                    TokenType.KeyListSuffix,
                    TokenType.KeyListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.KeyList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //try to parse a list or return empty
        private static Token ParseListSuffix
        (
            ParseToken parseItem,
            TokenType seperatorType,
            TokenType suffixType,
            TokenType listItemType,
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseListItems
            (
                parseItem,
                seperatorType,
                listItemType,
                ref reader,
                out Token keyListItems
            ))
            {
                //can return empty
                return Token.Create
                (
                    suffixType
                );
            }

            return Token.Create
            (
                suffixType,
                reader.Sequence.Slice(startPosition, reader.Position),
                keyListItems
            );
        }

        //parse list items with a seperator
        private static bool TryParseListItems
        (
            ParseToken parseItem,
            TokenType seperatorType,
            TokenType listItemType,
            ref SequenceReader<byte> reader,
            out Token listItems
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token previous = null, first = null, seperator;

            //a comma denotes a next item
            while(TryParseTerminal
            (
                seperatorType,
                ref reader,
                out seperator
            ))
            {
                previous = previous.Combine(seperator);

                if(first is null)
                {
                    first = previous;
                }

                previous = previous.Combine(parseItem(ref reader));

                found = true;
            }

            if(!found)
            {
                listItems = null;
                return false;
            }

            listItems = Token.Create
            (
                listItemType,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        //try to parse a key or return empty
        public static Token ParseKeyListItem
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can return empty
            if(!TryParseKey(ref reader, out Token key))
            {
                return Token.Create
                (
                    TokenType.KeyListItem
                );
            }
            else
            {
                return Token.Create
                (
                    TokenType.KeyListItem,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    key
                );
            }
        }

        //parse a single key
        public static bool TryParseKey
        (
            ref SequenceReader<byte> reader,
            out Token key
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;

            //check valid key terminals
            while(IsUTF8WithoutNullCrLfCommaSpaceListTerminal
            (
                ref reader,
                out _
            ))
            {
                reader.Advance(1);
                found = true;
            }

            if(!found)
            {
                key = null;
                return false;
            }

            key = Token.Create
            (
                TokenType.Key,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse a space seperated list of nicknames
        public static Token ParseNicknameSpaceList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseNickname(ref reader);

            //try to parse the rest of a nickname list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseNickname,
                    TokenType.Space,
                    TokenType.NicknameSpaceListSuffix,
                    TokenType.NicknameSpaceListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.NicknameSpaceList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

        }

        //parse a comma seperated list of channels
        public static Token ParseChannelCommaList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseChannel(ref reader);

            //try to parse the rest of a channel list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseChannel,
                    TokenType.Comma,
                    TokenType.ChannelCommaListSuffix,
                    TokenType.ChannelCommaListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.ChannelCommaList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //try to parse 1 or more comma seperated elist conditions
        public static Token ParseElistCondList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseElistCond(ref reader);

            //try to parse the rest of a elistcond list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseElistCond,
                    TokenType.Comma,
                    TokenType.ElistCondListSuffix,
                    TokenType.ElistCondListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.ElistCondList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse a single elist condition
        private static Token ParseElistCond
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //check valid key terminals
            while(IsUTF8WithoutNullCrLfCommaSpaceListTerminal
            (
                ref reader,
                out _
            ))
            {
                reader.Advance(1);
            }

            return Token.Create
            (
                TokenType.ElistCond,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }

        //parse a list of supported capablilities (CAP LS response)
        public static Token ParseCapList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseCapListItem(ref reader);

            //try to parse the rest of a cap list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseCapListItem,
                    TokenType.Space,
                    TokenType.CapListSuffix,
                    TokenType.CapListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.CapList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse a capability consisting of a key and an optional value
        private static Token ParseCapListItem
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token key = ParseCapListItemKey(ref reader);

            key.Combine(ParseCapListItemSuffix(ref reader));

            return Token.Create
            (
                TokenType.CapListItem,
                reader.Sequence.Slice(startPosition, reader.Position),
                key
            );
        }

        //parse a capability name
        private static Token ParseCapListItemKey
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token shortName = ParseHost(ref reader);

            shortName.Combine(ParseCapListItemKeySuffix(ref reader));

            return Token.Create
            (
                TokenType.CapListItemKey,
                reader.Sequence.Slice(startPosition, reader.Position),
                shortName
            );
        }

        //parse the rest of the vendor-specific capability or return empty
        private static Token ParseCapListItemKeySuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal(TokenType.Slash, ref reader, out Token slash))
            {
                return Token.Create
                (
                    TokenType.CapListItemKeySuffix
                );
            }

            slash.Combine(ParseShortName(ref reader));

            return Token.Create
            (
                TokenType.CapListItemKeySuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                slash
            );
        }

        //try to parse a value or return empty
        private static Token ParseCapListItemSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out Token equals
            ))
            {
                equals.Combine(ParseCapListItemValueList(ref reader));

                return Token.Create
                (
                    TokenType.CapListItemSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    equals
                );
            }
            //return empty
            else
            {
                return Token.Create
                (
                    TokenType.CapListItemSuffix
                );
            }
        }

        //parse the optional value(s)
        private static Token ParseCapListItemValueList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseCapListItemValueListItem(ref reader);

            //try to parse the rest of a cap list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseCapListItemValueListItem,
                    TokenType.Comma,
                    TokenType.CapListItemValueListSuffix,
                    TokenType.CapListItemValueListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.CapListItemValueList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse a single capability option
        private static Token ParseCapListItemValueListItem
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token shortName = ParseShortName(ref reader);

            return Token.Create
            (
                TokenType.CapListItemValueListItem,
                reader.Sequence.Slice(startPosition, reader.Position),
                shortName
            );
        }

        //parse a name RPL_NAMREPLY (353)
        public static Token ParseNameReply
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token spaces;

            //first parse the channel type
            Token channelType = ParseNameReplyChannelType(ref reader);

            //next token should be space(s)
            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }

            channelType.Combine(spaces);

            Token channel = ParseChannel(ref reader);
            spaces.Combine(channel);

            //next token should be space(s)
            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            channel.Combine(spaces);

            //next token should be a colon
            if(!TryParseTerminal
            (
                TokenType.Colon,
                ref reader,
                out Token colon
            ))
            {
                throw new ParserException("Colon expected");
            }

            spaces.Combine(colon);
            colon.Combine(ParseNicknameMembershipSpaceList(ref reader));

            return Token.Create
            (
                TokenType.NameReply,
                reader.Sequence.Slice(startPosition, reader.Position),
                channelType
            );
        }

        //try to parse a known channel type or throw an exception
        private static Token ParseNameReplyChannelType
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token channelType;

            if(TryParseTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out channelType
            )
                || TryParseTerminal
                (
                    TokenType.EqualitySign,
                    ref reader,
                    out channelType
                )
                || TryParseTerminal
                (
                    TokenType.Asterisk,
                    ref reader,
                    out channelType
                ))
            {
                return Token.Create
                (
                    TokenType.NameReplyChannelType,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channelType
                );
            }
            else
            {
                throw new ParserException("Invalid channel type");
            }
        }

        //parse a list of nicknames optionally prefixed with channel membership
        private static Token ParseNicknameMembershipSpaceList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseNicknameMembership(ref reader);

            //try to parse the rest of a nickname list or return empty
            first.Combine
            (
                ParseListSuffix
                (
                    _ParseNicknameMembership,
                    TokenType.Space,
                    TokenType.NicknameMembershipSpaceListSuffix,
                    TokenType.NicknameMembershipSpaceListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.NicknameMembershipSpaceList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        private static Token ParseNicknameMembership
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token first = null;

            if(TryParseChannelMembershipPrefix(ref reader, out Token prefix))
            {
                first = prefix;
            }

            Token nickname = ParseNickname(ref reader);

            if(first is null)
            {
                first = nickname;
            }
            else
            {
                first.Combine(nickname);
            }

            return Token.Create
            (
                TokenType.NicknameMembership,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse a space limited channel membership list (RPL_WHOISCHANNELS
        //(319))
        public static Token ParseChannelMembershipSpaceList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseMsgToChannel(ref reader);

            first.Combine
            (
                ParseListSuffix
                (
                    _ParseMsgToChannel,
                    TokenType.Space,
                    TokenType.ChannelMembershipSpaceListSuffix,
                    TokenType.ChannelMembershipSpaceListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.ChannelMembershipSpaceList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        private static Token ParseMsgToChannel
        (
            ref SequenceReader<byte> reader
        )
        {
            if(!TryParseMsgToChannel(ref reader, out Token channel))
            {
                throw new ParserException("Channel expected");
            }

            return channel;
        }

        //parse a list of RPL_USERHOST (302)
        public static Token ParseUserHostList
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseUserHostListItem(ref reader);

            first.Combine
            (
                ParseListSuffix
                (
                    _ParseUserHostListItem,
                    TokenType.Space,
                    TokenType.UserHostListSuffix,
                    TokenType.UserHostListItems,
                    ref reader
                )
            );

            return Token.Create
            (
                TokenType.UserHostList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        private static Token ParseUserHostListItem
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //first parse the nickname
            Token nickname = ParseNickname(ref reader);

            //next check if this nickname is op
            Token op = ParseUserHostListOp(ref reader);

            nickname.Combine(op);

            if(!TryParseTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out Token equals
            ))
            {
                throw new ParserException("Equality sign expected");
            }

            op.Combine(equals);

            //check if this nickname is away or not
            Token away = ParseUserHostListAway(ref reader);

            equals.Combine(away);

            //parse the hostname or username@hostname
            Token hostname = ParseUserHostListHostname(ref reader);

            away.Combine(hostname);

            return Token.Create
            (
                TokenType.UserHostListItem,
                reader.Sequence.Slice(startPosition, reader.Position),
                nickname
            );
        }

        //check for asterisk to signify op or return empty
        private static Token ParseUserHostListOp
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.Asterisk,
                ref reader,
                out Token asterisk
            ))
            {
                return Token.Create
                (
                    TokenType.UserHostListOp,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    asterisk
                );
            }

            return Token.Create
            (
                TokenType.UserHostListOp
            );
        }

        private static Token ParseUserHostListAway
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token away;

            if(TryParseTerminal
            (
                TokenType.Plus,
                ref reader,
                out away
            )
                || TryParseTerminal
                (
                    TokenType.Minus,
                    ref reader,
                    out away
                ))
            {
                return Token.Create
                (
                    TokenType.UserHostListAway,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    away
                );
            }

            throw new ParserException("Plus or minus expected");
        }

        private static Token ParseUserHostListHostname
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token prefix = ParseUserHostListHostnamePrefix(ref reader);
            prefix.Combine(ParseUserHostListHostnameSuffix(ref reader));

            return Token.Create
            (
                TokenType.UserHostListHostname,
                reader.Sequence.Slice(startPosition, reader.Position),
                prefix
            );
        }

        //parse the first part which might contain a username or a host
        private static Token ParseUserHostListHostnamePrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            while(IsUTF8WithoutNullCrLfSpaceAt(ref reader, out _))
            {
                reader.Advance(1);
            }

            return Token.Create
            (
                TokenType.UserHostListHostnamePrefix,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }

        //parse the second part, which contains a host, making the first part a
        //username
        private static Token ParseUserHostListHostnameSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.AtSign,
                ref reader,
                out Token at
            ))
            {
                Token hostname = ParseHost(ref reader);

                at.Combine(hostname);

                return Token.Create
                (
                    TokenType.UserHostListHostnameSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    at
                );
            }
            //can return empty
            else
            {
                return Token.Create
                (
                    TokenType.UserHostListHostnameSuffix
                );
            }
        }

        //parse a who reply - RPL_WHOREPLY (352)
        public static Token ParseWhoReply
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token spaces;

            Token prefix = ParseWhoReplyPrefix(ref reader);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            prefix.Combine(spaces);

            Token username = ParseUsername(ref reader);
            spaces.Combine(username);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            username.Combine(spaces);

            Token hostname = ParseHost(ref reader);
            spaces.Combine(hostname);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            hostname.Combine(spaces);

            Token servername = ParseServerName(ref reader);
            spaces.Combine(servername);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            servername.Combine(spaces);

            Token nickname = ParseNickname(ref reader);
            spaces.Combine(nickname);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            nickname.Combine(spaces);

            Token flags = ParseWhoReplyFlags(ref reader);
            spaces.Combine(flags);

            if(!TryParseSpaces(ref reader, out spaces))
            {
                throw new ParserException("Space(s) expected");
            }
            flags.Combine(spaces);

            if(!TryParseTrailing(ref reader, out Token trailing))
            {
                throw new ParserException("Trailing expected");
            }
            spaces.Combine(trailing);

            return Token.Create
            (
                TokenType.WhoReply,
                reader.Sequence.Slice(startPosition, reader.Position),
                prefix
            );
        }

        //parse the prefix consisting of a channel or '*'
        private static Token ParseWhoReplyPrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.Asterisk,
                ref reader,
                out Token asterisk
            ))
            {
                return Token.Create
                (
                    TokenType.WhoReplyPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    asterisk
                );
            }
            else
            {
                Token channel = ParseChannel(ref reader);

                return Token.Create
                (
                    TokenType.WhoReplyPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    channel
                );
            }
        }

        private static Token ParseWhoReplyFlags
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token first = ParseWhoReplyAway(ref reader);

            //mode chars can return empty
            first
                .Combine(ParseWhoReplyFlagsOp(ref reader))
                .Combine(ParseWhoReplyChannelMembership(ref reader))
                .Combine(ParseModeChars(ref reader));

            return Token.Create
            (
                TokenType.WhoReplyFlags,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //try parse the away status (Gone or Here)
        private static Token ParseWhoReplyAway
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token away;

            if(TryParseTerminal
            (
                TokenType.G,
                ref reader,
                out away
            )
                || TryParseTerminal
                (
                    TokenType.H,
                    ref reader,
                    out away
                ))
            {
                return Token.Create
                (
                    TokenType.WhoReplyAway,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    away
                );
            }

            throw new ParserException("G or H expected");
        }

        //try to parse the op flag ('*') or return empty
        private static Token ParseWhoReplyFlagsOp
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(IsTerminal(TokenType.Asterisk, ref reader, out _))
            {
                reader.Advance(1);
                return Token.Create
                (
                    TokenType.WhoReplyFlagsOp,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );
            }
            else
            {
                return Token.Create
                (
                    TokenType.WhoReplyFlagsOp
                );
            }
        }

        //try to parse the channel membership or return empty
        private static Token ParseWhoReplyChannelMembership
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseChannelMembershipPrefix(ref reader, out Token prefix))
            {
                return Token.Create
                (
                    TokenType.WhoReplyChannelMembership,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    prefix
                );
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.WhoReplyChannelMembership
                );
            }
        }
    }
}
