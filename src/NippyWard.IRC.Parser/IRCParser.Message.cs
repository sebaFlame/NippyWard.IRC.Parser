using System;
using System.Buffers;

using NippyWard.IRC.Parser.Tokens;

namespace NippyWard.IRC.Parser
{
    public static partial class IRCParser
    {
        //Parse a single message up until CR/LF
        private static Token ParseMessage(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //parse the tag prefix
            Token tagPrefix = ParseTagPrefix(ref reader);

            /* parse the target prefix and add to children
             * parse the command and add to children
             * parse CRL/LF as message end and add to children */
            tagPrefix
                .Combine(ParseSourcePrefix(ref reader))
                .Combine(ParseVerb(ref reader))
                .Combine(ParseParams(ref reader))
                .Combine(ParseCrLf(ref reader));

            return Token.Create
            (
                TokenType.Message,
                reader.Sequence.Slice(startPosition, reader.Position),
                tagPrefix
            );
        }

        //try parse IRCv3 tags or return an empty token if not found
        private static Token ParseTagPrefix(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;
            Token at, tags, space;

            //match the '@' as start of the tags
            if(TryParseTerminal(TokenType.AtSign, ref reader, out at))
            {
                //parse all tags
                tags = ParseTags(ref reader);

                //create linked list
                at.Combine(tags);

                //parse trailing space(s)
                if(TryParseSpaces(ref reader, out space))
                {
                    //add trailings space(s) to linked list
                    tags.Combine(space);

                    return Token.Create
                    (
                        TokenType.TagPrefix,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        at
                    );
                }
                //if no spaces: parsing error!
                else
                {
                    throw new ParserException("Space(s) expected");
                }
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.TagPrefix
                );
            }
        }

        //parse one or more tags
        private static Token ParseTags(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //parse single tag
            Token tag = ParseTag(ref reader);

            //parse following (if any)
            Token tagsSuffix = ParseTagsSuffix(ref reader);

            //combine the children
            tag.Combine(tagsSuffix);

            return Token.Create
            (
                TokenType.Tags,
                reader.Sequence.Slice(startPosition, reader.Position),
                tag
            );
        }

        private static Token ParseTagsSuffix(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //if no list found, return empty
            if(!TryParseTagsList(ref reader, out Token tagsList))
            {
                return Token.Create
                (
                     TokenType.TagsSuffix
                );
            }
            //else return the parsed tags list
            else
            {
                return Token.Create
                (
                    TokenType.TagsSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    tagsList
                );
            }
        }

        //parse multiple tags
        private static bool TryParseTagsList
        (
            ref SequenceReader<byte> reader,
            out Token tagsList
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null, semicolon;

            //multiple tags are seperated by a semicolon
            while(TryParseTerminal
            (
                TokenType.Semicolon,
                ref reader,
                out semicolon
            ))
            {
                //add semicolon to linked list
                previous = previous.Combine(semicolon);

                if(first is null)
                {
                    first = previous;
                }

                //parse tag and add to children
                previous = previous.Combine(ParseTag(ref reader));
            }

            //did not find a semicolon
            if(first is null)
            {
                tagsList = null;
                return false;
            }

            tagsList = Token.Create
            (
                TokenType.TagsList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
            return true;
        }

        //parse a single tag 
        private static Token ParseTag(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //parse the key
            Token tagKey = ParseTagKey(ref reader);

            //parse the value or return empty
            Token tagSuffix = ParseTagSuffix(ref reader);

            tagKey.Combine(tagSuffix);

            return Token.Create
            (
                TokenType.Tag,
                reader.Sequence.Slice(startPosition, reader.Position),
                tagKey
            );
        }

        //parse a single tag key
        internal static Token ParseTagKey
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token previous = null, first = null;

            /* It can start with a client prefix (+)
             * ignore if it doesn't */
            if(TryParseTerminal(TokenType.Plus, ref reader, out Token plus))
            {
                first = previous = plus;
            }

            //parse atleast a single shortname
            Token shortName = ParseHost(ref reader);

            if(first is null)
            {
                first = previous = shortName;
            }
            else
            {
                previous = previous.Combine(shortName);
            }

            //parse a possible suffix, if not empty, first shortname is the
            //vendor
            Token tagKeySuffix = ParseTagKeySuffix(ref reader);

            previous.Combine(tagKeySuffix);

            return Token.Create
            (
                TokenType.TagKey,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse a single tag key
        private static Token ParseTagKeySuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.Slash,
                ref reader,
                out Token slash))
            {
                Token shortName = ParseShortName(ref reader);

                //add shortName to linked list
                slash.Combine(shortName);

                return Token.Create
                (
                    TokenType.TagKeySuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    slash
                );

            }
            //can be empty
            else
            {
                return Token.Create
                (
                    TokenType.TagKeySuffix
                );
            }
        }

        //parse the tag suffix, which can be an equaliyt signe followed by a
        //value or empty
        private static Token ParseTagSuffix(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseTerminal
            (
                TokenType.EqualitySign,
                ref reader,
                out Token equality))
            {
                Token tagValue = ParseTagValue(ref reader);

                equality.Combine(tagValue);

                return Token.Create
                (
                    TokenType.TagSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    equality
                );

            }
            //If it does not start with an equal sign, return empty
            else
            {
                return Token.Create
                (
                    TokenType.TagSuffix
                );
            }
        }

        //try parse tag value or return empty
        internal static Token ParseTagValue(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //try parse a tag value as a list of terminals
            if(TryParseTagValueList(ref reader, out Token tagValueList))
            {
                return Token.Create
                (
                    TokenType.TagValue,
                    reader.Sequence.Slice(startPosition, reader.Position),
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

        //try parse tag value as list of terminals
        private static bool TryParseTagValueList
        (
            ref SequenceReader<byte> reader,
            out Token tagValuelist
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child;

            //parse as long as it's UTF-8 excluding NUL, CR, LF, Semicolon,
            //space and backspace
            while(TryParseUTF8WithoutNullCrLfSemiColonSpace
            (
                ref reader,
                out child
            ) || TryParseTagValueEscapeList(ref reader, out child))
            {
                if(firstChild is null)
                {
                    firstChild = child;
                }

                previous = previous.Combine(child);

                found = true;
            }

            if(!found)
            {
                tagValuelist = null;
                return false;
            }

            tagValuelist = Token.Create
            (
                TokenType.TagValueList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
            );

            return true;
        }

        internal static bool TryParseUTF8WithoutNullCrLfSemiColonSpace
        (
            ref SequenceReader<byte> reader,
            out Token token
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;

            while(IsUTF8WithoutNullCrLfSemiColonSpace(ref reader, out _))
            {
                reader.Advance(1);
                found = true;
            }

            if(!found)
            {
                token = null;
                return false;
            }

            token = Token.Create
            (
                TokenType.UTF8WithoutNullCrLfSemiColonSpace,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        private static bool TryParseTagValueEscapeList
        (
            ref SequenceReader<byte> reader,
            out Token tagValueEscapeList
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child;

            while(TryParseTagValueEscape(ref reader, out child))
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
                tagValueEscapeList = null;
                return false;
            }

            tagValueEscapeList = Token.Create
            (
                TokenType.TagValueEscapeList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
            );

            return true;
        }

        public static bool TryParseTagValueEscape
        (
            ref SequenceReader<byte> reader,
            out Token tagValueEscape
        )
        {
            SequencePosition startPosition = reader.Position;

            if(!TryParseTerminal
            (
                TokenType.Backslash,
                ref reader,
                out Token backslash
            ))
            {
                tagValueEscape = null;
                return false;
            }

            Token tagValueEscapeSuffix = ParseTagValueEscapeSuffix
            (
                ref reader
            );

            backslash.Combine(tagValueEscapeSuffix);

            tagValueEscape = Token.Create
            (
                TokenType.TagValueEscape,
                reader.Sequence.Slice(startPosition, reader.Position),
                backslash
            );

            return true;
        }

        private static Token ParseTagValueEscapeSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;
            TokenType tokenType;

            if(IsTerminal(TokenType.N, ref reader, out value)
                || IsTerminal(TokenType.n, value))
            {
                tokenType = TokenType.TagValueEscapeLf;
            }
            else if(IsTerminal(TokenType.R, value)
                || IsTerminal(TokenType.r, value))
            {
                tokenType = TokenType.TagValueEscapeCr;
            }
            else if(IsTerminal(TokenType.S, value)
                || IsTerminal(TokenType.s, value))
            {
                tokenType = TokenType.TagValueEscapeSpace;
            }
            else if(IsTerminal(TokenType.Colon, value))
            {
                tokenType = TokenType.TagValueEscapeSemicolon;
            }
            else if(IsTerminal(TokenType.Backslash, value))
            {
                tokenType = TokenType.TagValueEscapeBackslash;
            }
            //invalid slash (NOT IN GRAMMAR)
            else
            {
                tokenType = TokenType.TagValueEscapeInvalid;
            }

            if(tokenType != TokenType.TagValueEscapeInvalid)
            {
                //advance the recognised terminal
                reader.Advance(1);
            }

            Token child = Token.Create
            (
                tokenType,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return Token.Create
            (
                TokenType.TagValueEscapeSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                child
            );
        }

        //parse the source prefix, or return empty token
        private static Token ParseSourcePrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //source prefix always starts with a colon
            if(TryParseTerminal
            (
                TokenType.Colon,
                ref reader,
                out Token colon))
            {
                Token targetPrefixTarget = ParseSourcePrefixTarget(ref reader);

                colon.Combine(targetPrefixTarget);

                if(!TryParseSpaces
                (
                    ref reader,
                    out Token spaces
                ))
                {
                    throw new ParserException("Space expected");
                }

                targetPrefixTarget.Combine(spaces);

                return Token.Create
                (
                    TokenType.SourcePrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    colon
                );

            }
            //or return an empty token
            else
            {
                return Token.Create
                (
                    TokenType.SourcePrefix
                );
            }
        }

        //parse the actual source prefix target
        internal static Token ParseSourcePrefixTarget
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token sourcePrefixTargetTargetPrefix =
                ParseSourcePrefixTargetPrefix(ref reader);

            Token sourcePrefixTargetTargetSuffix =
                ParseSourcePrefixTargetSuffix(ref reader);

            //link prefix and suffix together
            sourcePrefixTargetTargetPrefix.Combine
            (
                sourcePrefixTargetTargetSuffix
            );

            return Token.Create
            (
                TokenType.SourcePrefixTarget,
                reader.Sequence.Slice(startPosition, reader.Position),
                sourcePrefixTargetTargetPrefix
            );
        }

        private static Token ParseSourcePrefixTargetPrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token targetPrefixPrefix = ParseSourcePrefixTargetPrefixPrefix
            (
                ref reader
            );

            Token TargetPrefixSuffix = ParseSourcePrefixTargetPrefixSuffix
            (
                ref reader
            );

            targetPrefixPrefix.Combine(TargetPrefixSuffix);

            return Token.Create
            (
                TokenType.SourcePrefixTargetPrefix,
                reader.Sequence.Slice(startPosition, reader.Position),
                targetPrefixPrefix
            );
        }

        private static Token ParseSourcePrefixTargetPrefixPrefix
        (
             ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;

            //parse atleast 1 alphanumeric or special byte
            if(IsAlphaNumeric(ref reader, out value)
                || IsSpecial(value))
            {
                reader.Advance(1);
            }
            else
            {
                throw new ParserException("Alphanumeric or special expected"!);
            }

            return Token.Create
            (
                TokenType.SourcePrefixTargetPrefixPrefix,
                reader.Sequence.Slice(startPosition, reader.Position)
            );
        }


        //parse rest of the source prefix target or return empty
        private static Token ParseSourcePrefixTargetPrefixSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse sourcePrefixList
            if(TryParseSourcePrefixTargetPrefixTargetList
            (
                ref reader,
                out Token sourcePrefixList
            ))
            {
                return Token.Create
                (
                    TokenType.SourcePrefixTargetPrefixSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    sourcePrefixList
                );
            }
            //or return empty
            else
            {
                return Token.Create
                (
                    TokenType.SourcePrefixTargetPrefixSuffix
                );
            }
        }

        //parse a list of terminals valid for 2nd to nth byte of the source
        //prefix target
        private static bool TryParseSourcePrefixTargetPrefixTargetList
        (
            ref SequenceReader<byte> reader,
            out Token sourcePrefixList
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;
            bool found = false;

            while(IsAlphaNumeric(ref reader, out value)
                || IsSpecial(value)
                || IsTerminal(TokenType.Minus, value)
                || IsTerminal(TokenType.Period, value))
            {
                found = true;
                reader.Advance(1);
            }

            if(!found)
            {
                sourcePrefixList = null;
                return false;
            }

            sourcePrefixList = Token.Create
            (
                TokenType.SourcePrefixTargetPrefixTargetList,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        private static Token ParseSourcePrefixTargetSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            Token sourcePrefixUsername =
                ParseSourcePrefixUsername(ref reader);

            Token sourcePrefixHostname =
                ParseSourcePrefixHostname(ref reader);

            //link prefix and suffix together
            sourcePrefixUsername.Combine
            (
                sourcePrefixHostname
            );

            return Token.Create
            (
                TokenType.SourcePrefixTargetSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                sourcePrefixUsername
            );

        }

        private static Token ParseSourcePrefixUsername
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //username start with an exclamation mark
            if(TryParseUserHostUsername
            (
                ref reader,
                out Token userName
            ))
            {
                return Token.Create
                (
                    TokenType.SourcePrefixUsername,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    userName
                );
            }
            //return empty
            else
            {
                return Token.Create
                (
                    TokenType.SourcePrefixUsername
                );
            }
        }

        private static Token ParseSourcePrefixHostname
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //username start with an exclamation mark
            if(TryParseUserHostHostname
            (
                ref reader,
                out Token hostname
            ))
            {
                return Token.Create
                (
                    TokenType.SourcePrefixHostname,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    hostname
                );
            }
            //return empty
            else
            {
                return Token.Create
                (
                    TokenType.SourcePrefixHostname
                );
            }
        }


        //parse a command or return empty
        private static Token ParseVerb(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;
            Token cmd;

            //try parsing the command or code
            if(TryParseCommandName(ref startPosition, ref reader, out cmd)
                 || TryParseCommandCode(ref startPosition, ref reader, out cmd))
            {
                return Token.Create
                (
                    TokenType.Verb,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    cmd
                );
            }

            //can return empty
            return Token.Create
            (
                TokenType.Verb
            );
        }

        //parse a command name / continue a command name
        internal static bool TryParseCommandName
        (
            ref SequencePosition startPosition,
            ref SequenceReader<byte> reader,
            out Token commandName
        )
        {
            bool found = false;

            //match an uppercase or lowercase letter
            while(MatchLetter(ref reader))
            {
                found = true;
            }

            if(!found)
            {
                commandName = null;
                return false;
            }

            commandName = Token.Create
            (
                TokenType.CommandName,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse a command code / continue a command code
        internal static bool TryParseCommandCode
        (
            ref SequencePosition startPosition,
            ref SequenceReader<byte> reader,
            out Token commandCode,
            int recognizedCount = 0
        )
        {
            //parse 3 digits
            for(int i = 0; i < 3 - recognizedCount; i++)
            {
                //match a digit and advance
                if(!MatchDigit(ref reader))
                {
                    commandCode = null;
                    return false;
                }
            }

            commandCode = Token.Create
            (
                TokenType.CommandCode,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse paramaters or return empty
        private static Token ParseParams(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            if(TryParseParamsPrefix(ref reader, out Token paramsPrefix))
            {
                return Token.Create
                (
                    TokenType.Params,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    paramsPrefix
                );
            }
            else
            {
                //return empty
                return Token.Create
                (
                    TokenType.Params
                );
            }
        }

        //try parse paramaters prefix
        private static bool TryParseParamsPrefix
        (
            ref SequenceReader<byte> reader,
            out Token paramsPrefix
        )
        {
            SequencePosition startPosition = reader.Position;

            //match one or more spaces and advance
            if(TryParseSpaces
            (
                ref reader,
                out Token spaces
            ))
            {
                //get the parameter suffix containing one or more paramaters,
                //the called method recurses back on the current to check for
                //multiple paramaters
                Token paramsSuffix = ParseParamsSuffix(ref reader);

                //There can be multiple spaces
                spaces.Combine(paramsSuffix);

                paramsPrefix = Token.Create
                (
                    TokenType.ParamsPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    spaces
                );

                return true;
            }
            else
            {
                paramsPrefix = null;
                return false;
            }
        }

        //parse a single paramter, either a middle or a trailing, and try
        //parsing next paramter ref case of a middle
        public static Token ParseParamsSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //This is always the last parameter, do not continue params parsing
            if(TryParseTrailing(ref reader, out Token trailing))
            {
                return Token.Create
                (
                    TokenType.ParamsSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    trailing
                );
            }
            //or parse middle
            else if(TryParseMiddle(ref reader, out Token middle))
            {
                //try get next parameter only when a new middle is found
                if(TryParseParamsPrefix(ref reader, out Token paramsPrefix))
                {
                    middle.Combine(paramsPrefix);
                }

                return Token.Create
                (
                    TokenType.ParamsSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    middle
                );
            }
            //can be a CTCP message outside of a trailing
            else if(TryParseCTCPMessage(ref reader, out Token ctcpMessage))
            {
                return Token.Create
                (
                    TokenType.ParamsSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    ctcpMessage
                );

            }
            //else return empty
            else
            {
                return Token.Create
                (
                    TokenType.ParamsSuffix
                );
            }
        }

        //try parse a middle
        public static bool TryParseMiddle
        (
            ref SequenceReader<byte> reader,
            out Token middle
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing atleast 1 byte of the middle prefix
            if(TryParseMiddlePrefixList(ref reader, out Token middlePrefix))
            {
                Token middleSuffix = ParseMiddleSuffix(ref reader);

                middlePrefix.Combine(middleSuffix);

                middle = Token.Create
                (
                    TokenType.Middle,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    middlePrefix
                );

                return true;
            }
            else
            {
                middle = null;
                return false;
            }
        }

        //try parsing a list of terminals valid for the first byte of the
        //middle: UTF-8 without space, colon, NUL, CR, LF
        public static bool TryParseMiddlePrefixList
        (
            ref SequenceReader<byte> reader,
            out Token middlePrefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token first = null, previous = null,
                      child;
            bool found = false;
            byte value;

            //first check for space of colon
            while(!(IsTerminal(TokenType.Space, ref reader, out value)
                    || IsTerminal(TokenType.Colon, value))
                && (TryParseMiddlePrefixListTerminals(ref reader, out child)
                    || TryParseMiddlePrefixListFormatBase
                    (
                        ref reader,
                        out child
                    )))
            {
                if(first is null)
                {
                    first = child;
                }

                previous = previous.Combine(child);

                found = true;
            }

            if(!found)
            {
                middlePrefix = null;
                return false;
            }

            middlePrefix = Token.Create
            (
                TokenType.MiddlePrefixList,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        private static bool TryParseMiddlePrefixListTerminals
        (
            ref SequenceReader<byte> reader,
            out Token midllePrefixListTerminals
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            byte value;

            while(IsDigit(ref reader, out value)
                || IsUpperCaseHexLetter(value)
                || IsLowerCaseHexLetter(value)
                || IsTerminal(TokenType.Comma, value))
            {
                reader.Advance(1);
                found = true;
            }

            if(!found)
            {
                midllePrefixListTerminals = null;
                return false;
            }

            midllePrefixListTerminals = Token.Create
            (
                TokenType.MiddlePrefixListTerminals,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //try match format, UTF-8 without space, colon, NUL, CR, LF,
        //hex letter, digit or comma
        private static bool TryParseMiddlePrefixListFormatBase
        (
            ref SequenceReader<byte> reader,
            out Token middlePrefixListFormatBase
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child;
            byte value;

            while(!(IsTerminal(TokenType.Space, ref reader, out value)
                    || IsTerminal(TokenType.Colon, value))
                && (TryParseMiddlePrefixListFormatBaseTerminals
            (
                ref reader,
                out child
            ) || TryParseFormat(ref reader, out child)))
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
                middlePrefixListFormatBase = null;
                return false;
            }

            middlePrefixListFormatBase = Token.Create
            (
                TokenType.MiddlePrefixListFormatBase,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
            );

            return true;
        }

        private static bool TryParseMiddlePrefixListFormatBaseTerminals
        (
            ref SequenceReader<byte> reader,
            out Token midllePrefixListFormatBaseTerminals
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            byte value;

            while(IsUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon
            (
                ref reader,
                out value
            )
                   || IsLowerCaseLetter(value)
                   || IsUpperCaseLetter(value))
            {
                reader.Advance(1);
                found = true;
            }

            if(!found)
            {
                midllePrefixListFormatBaseTerminals = null;
                return false;
            }

            midllePrefixListFormatBaseTerminals = Token.Create
            (
                TokenType.MiddlePrefixListFormatBaseTerminals,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //Try parse the middle suffix: everything that can match the middle
        //prefix, including colon. Or return empty
        private static Token ParseMiddleSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token middlePrefix;

            //return an empty token if not matched
            if(!TryParseMiddlePrefixWithColonList(ref reader, out middlePrefix))
            {
                return Token.Create
                (
                    TokenType.MiddleSuffix
                );
            }
            //or return a middle prefix WITH colon
            else
            {
                return Token.Create
                (
                    TokenType.MiddleSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    middlePrefix
                );
            }
        }

        private static bool TryParseMiddlePrefixWithColonList
        (
            ref SequenceReader<byte> reader,
            out Token middlePrefixWithColonList
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child;

            while(!IsTerminal(TokenType.Space, ref reader, out _)
                && (TryParseTerminal(TokenType.Colon, ref reader, out child)
                    || TryParseMiddlePrefixList(ref reader, out child)
                    || TryParseTerminal(TokenType.CTCP, ref reader, out child)))
            {
                if(firstChild is null)
                {
                    firstChild = child;
                }

                previous = previous.Combine(child);

                found = true;
            }

            if(!found)
            {
                middlePrefixWithColonList = null;
                return false;
            }

            middlePrefixWithColonList = Token.Create
            (
                TokenType.MiddlePrefixWithColonList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
            );

            return true;
        }

        //try parse trailing: middle prefixed with a colon and containing colon
        //and space
        private static bool TryParseTrailing
        (
            ref SequenceReader<byte> reader,
            out Token trailing
        )
        {
            SequencePosition startPosition = reader.Position;

            //trailing is always prefixed with a colon
            if(TryParseTerminal
            (
                TokenType.Colon,
                ref reader,
                out Token colon
            ))
            {
                //a trailing prefix can be empty
                Token trailingPrefix = ParseTrailingPrefix(ref reader);

                colon.Combine(trailingPrefix);

                trailing = Token.Create
                (
                    TokenType.Trailing,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    colon
                );

                return true;
            }
            else
            {
                trailing = null;
                return false;
            }
        }

        //parse the trailing prefix or return empty
        internal static Token ParseTrailingPrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing a trailing CTCP message
            if(TryParseCTCPMessage(ref reader, out Token ctcpMessage))
            {
                return Token.Create
                (
                    TokenType.TrailingPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    ctcpMessage
                );

            }
            //try parsing the trailing list
            else if(TryParseTrailingList(ref reader, out Token trailingList))
            {
                return Token.Create
                (
                    TokenType.TrailingPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    trailingList
                );
            }
            //or return an empty token
            else
            {
                //return empty
                return Token.Create
                (
                    TokenType.TrailingPrefix
                );
            }
        }

        //try parsing the trailing list consiting of middlePrefixColon and space
        private static bool TryParseTrailingList
        (
            ref SequenceReader<byte> reader,
            out Token trailingList
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing atleast 1 byte of the middle prefix
            if(!TryParseTrailingListPrefix
            (
                ref reader,
                out Token trailingPrefix
            ))
            {
                trailingList = null;
                return false;
            }

            //parse rest of the trailing
            trailingPrefix.Combine(ParseTrailingListSuffix(ref reader));

            trailingList = Token.Create
            (
                TokenType.Middle,
                reader.Sequence.Slice(startPosition, reader.Position),
                trailingPrefix
            );

            return true;
        }

        //try parsing the trailing list prefix with space and colon
        private static bool TryParseTrailingListPrefix
        (
            ref SequenceReader<byte> reader,
            out Token trailingSuffix
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token first = null, previous = null, child;

            while(TryParseTerminal(TokenType.Space, ref reader, out child)
                || TryParseTerminal(TokenType.Colon, ref reader, out child)
                || TryParseMiddlePrefixList(ref reader, out child))
            {
                if(first is null)
                {
                    first = child;
                }

                previous = previous.Combine(child);

                found = true;
            }

            if(!found)
            {
                trailingSuffix = null;
                return false;
            }

            trailingSuffix = Token.Create
            (
                TokenType.TrailingListPrefix,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );

            return true;
        }

        private static Token ParseTrailingListSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            Token first = null, previous = null, child;
            bool found = false;

            while(TryParseTerminal(TokenType.Space, ref reader, out child)
                || TryParseTerminal(TokenType.Colon, ref reader, out child)
                || TryParseMiddlePrefixList(ref reader, out child))
            {
                if(first is null)
                {
                    first = child;
                }

                previous = previous.Combine(child);

                found = true;
            }

            //can be empty
            if(!found)
            {
                return Token.Create
                (
                    TokenType.TrailingListSuffix
                );
            }

            return Token.Create
            (
                TokenType.TrailingListSuffix,
                reader.Sequence.Slice(startPosition, reader.Position),
                first
            );
        }

        //parse the CR LF ath the end of the message
        private static Token ParseCrLf(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;
            Token cr, lf;

            if(TryParseTerminal
            (
                TokenType.CarriageReturn,
                ref reader,
                out cr))
            {
                if(TryParseTerminal
                (
                    TokenType.LineFeed,
                    ref reader,
                    out lf))
                {
                    cr.Combine(lf);

                    return Token.Create
                    (
                        TokenType.CrLf,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        cr
                    );
                }
                else
                {
                    throw new ParserException("CR LF expected");
                }
            }
            else if(TryParseTerminal
            (
                TokenType.LineFeed,
                ref reader,
                out lf))
            {
                return Token.Create
                (
                    TokenType.CrLf,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    lf
                );
            }

            throw new ParserException("CR LF expected");
        }
    }
}
