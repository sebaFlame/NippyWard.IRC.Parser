using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser
{
    public static partial class IRCParser
    {
        //Parse a single message up until CR/LF
        private static Token ParseMessage(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //parse the tag prefix
            Token tagPrefix = ParseTagPrefix(ref reader);

            Combine //parse CRL/LF as message end and add to children
            (
                Combine //parse the command and add to children
                (
                    Combine //parse the target prefix and add to children
                    (
                        tagPrefix,
                        ParseSourcePrefix(ref reader)
                    ),
                    ParseCommand(ref reader)
                ),
                ParseCrLf(ref reader)
            );

            return new Token
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

            //match the '@' as start of the tags
            if(MatchTerminal(Terminal.AtSign, ref reader))
            {
                //parse all tags
                Token tags = ParseTags(ref reader);

                //parse trailing space(s)
                if(MatchSpaces(ref reader))
                {
                    return new Token
                    (
                        TokenType.TagPrefix,
                        reader.Sequence.Slice(startPosition, reader.Position),
                        tags
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
                return new Token
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
            Combine(tag, tagsSuffix);

            return new Token
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
                return new Token
                (
                     TokenType.TagsSuffix
                );
            }
            //else return the parsed tags list
            else
            {
                return new Token
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
            Token previous = null, first = null;

            //multiple tags are seperated by a semicolon
            while(MatchTerminal(Terminal.Semicolon, ref reader))
            {
                //parse tag and add to children
                previous = Combine(previous, ParseTag(ref reader));

                if(first is null)
                {
                    first = previous;
                }
            }

            //did not find a semicolon
            if(first is null)
            {
                tagsList = null;
                return false;
            }

            tagsList = new Token
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

            Combine(tagKey, tagSuffix);

            return new Token
            (
                TokenType.Tag,
                reader.Sequence.Slice(startPosition, reader.Position),
                tagKey
            );
        }

        //parse a single tag key
        private static Token ParseTagKey
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            /* It can start with a client prefix (+)
             * ignore if it doesn't */
            MatchTerminal(Terminal.Plus, ref reader);

            //parse atleast a single shortname
            Token shortName = ParseShortName(ref reader);

            //parse a possible suffix, if not empty, first shortname is the
            //vendor
            Token tagKeySuffix = ParseTagKeySuffix(ref reader);

            Combine(shortName, tagKeySuffix);

            return new Token
            (
                TokenType.TagKey,
                reader.Sequence.Slice(startPosition, reader.Position),
                shortName
            );
        }

        //parse a single tag key
        private static Token ParseTagKeySuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can be empty
            if(!MatchTerminal(Terminal.Slash, ref reader))
            {
                return new Token
                (
                    TokenType.TagKeySuffix
                );
            }
            else
            {
                Token shortName = ParseShortName(ref reader);

                return new Token
                (
                    TokenType.TagKeySuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    shortName
                );
            }
        }

        //parse a short name
        private static Token ParseShortName
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //a short name start with an alphanumeric
            if(MatchAlphaNumeric(ref reader))
            {
                //parse the shortname suffix
                Token shortNameSuffix = ParseShortNameSuffix(ref reader);

                return new Token
                (
                    TokenType.ShortName,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    shortNameSuffix
                );
            }
            else
            {
                throw new ParserException("Alphanumeric expected");
            }
        }

        //parse a shortname suffix, or return empty
        private static Token ParseShortNameSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //can return an empty token
            if(!TryParseShortNameList(ref reader, out Token shortNameList))
            {
                return new Token
                (
                    TokenType.ShortNameSuffix
                );
            }
            //or return a list of shortname terminals
            else
            {
                return new Token
                (
                    TokenType.ShortNameSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    shortNameList
                );
            }
        }

        //parse a list of terminals as shortname
        private static bool TryParseShortNameList
        (
            ref SequenceReader<byte> reader,
            out Token shortNameList
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;
            bool found = false;

            //Match 0-9, a-z, A-Z or hyphen and advance if found
            while(IsAlphaNumeric(ref reader, out value)
               || IsTerminal(Terminal.Minus, value))
            {
                found = true;
                reader.Advance(1);
            }

            if(!found)
            {
                shortNameList = null;
                return false;
            }

            shortNameList = new Token
            (
                TokenType.ShortNameSuffix,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse the tag suffix, which can contaref a value or empty
        private static Token ParseTagSuffix(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //If it does not start with an equal sign, return empty
            if(!MatchTerminal(Terminal.EqualitySign, ref reader))
            {
                return new Token
                (
                    TokenType.TagSuffix
                );
            }
            //or parse the value
            else
            {
                Token tagValue = ParseTagValue(ref reader);

                return new Token
                (
                    TokenType.TagSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    tagValue
                );
            }
        }

        //try parse tag value or return empty
        private static Token ParseTagValue(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //try parse a tag value as a list of terminals
            if(TryParseTagValueList(ref reader, out Token tagValueList))
            {
                return new Token
                (
                    TokenType.TagValue,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    tagValueList
                );
            }
            //or return empty
            else
            {
                return new Token
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

            //parse as long as it's UTF-8 excluding NUL, CR, LF, Semicolon and
            //space
            while(IsUTF8WithoutNullCrLfSemiColonSpace(ref reader, out _))
            {
                found = true;
                reader.Advance(1);
            }

            if(!found)
            {
                tagValuelist = null;
                return false;
            }

            tagValuelist = new Token
            (
                TokenType.TagValueList,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse the source prefix, or return empty token
        private static Token ParseSourcePrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //source prefix always starts with a colon
            if(MatchTerminal(Terminal.Colon, ref reader))
            {
                Token targetPrefixTarget = ParseSourcePrefixTarget(ref reader);

                if(!MatchSpaces(ref reader))
                {
                    throw new ParserException("Space expected");
                }

                return new Token
                (
                    TokenType.SourcePrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    targetPrefixTarget
                );

            }
            //or return an empty token
            else
            {
                return new Token
                (
                    TokenType.SourcePrefix
                );
            }
        }

        //parse the actual source prefix target
        private static Token ParseSourcePrefixTarget
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;

            //parse atleast 1 alphanumeric or special byte
            if(!(IsAlphaNumeric(ref reader, out value)
                || IsSpecial(value)))
            {
                throw new ParserException("Alphanumeric or special expected"!);
            }
            else
            {
                reader.Advance(1);
            }

            Token sourcePrefixTargetTargetSuffix =
                ParseSourcePrefixTargetSuffix(ref reader);

            return new Token
            (
                TokenType.SourcePrefixTarget,
                reader.Sequence.Slice(startPosition, reader.Position),
                sourcePrefixTargetTargetSuffix
            );
        }

        //parse rest of the source prefix target or return empty
        private static Token ParseSourcePrefixTargetSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parse sourcePrefixList
            if(TryParseSourcePrefixList(ref reader, out Token sourcePrefixList))
            {
                return new Token
                (
                    TokenType.SourcePrefixTargetSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    sourcePrefixList
                );
            }
            //or return empty
            else
            {
                return new Token
                (
                    TokenType.SourcePrefixTargetSuffix
                );
            }
        }

        //parse a list of terminals valid for 2nd to nth byte of the source
        //prefix target
        private static bool TryParseSourcePrefixList
        (
            ref SequenceReader<byte> reader,
            out Token sourcePrefixList
        )
        {
            SequencePosition startPosition = reader.Position;
            byte value;
            bool found = false;

            //check if the current byte is alphanumeric, special,
            //exclamationmark, at, minus or a period (host)
            while(IsAlphaNumeric(ref reader, out value)
                || IsSpecial(value)
                || IsTerminal(Terminal.ExclamationMark, value)
                || IsTerminal(Terminal.AtSign, value)
                || IsTerminal(Terminal.Minus, value)
                || IsTerminal(Terminal.Period, value)
                || IsTerminal(Terminal.Tilde, value))
            {
                found = true;
                reader.Advance(1);
            }

            if(!found)
            {
                sourcePrefixList = null;
                return false;
            }

            sourcePrefixList = new Token
            (
                TokenType.SourcePrefixTarget,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse a command or return empty
        private static Token ParseCommand(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            //try parse unknown message
            if(TryParseErroneousMessage(ref reader, out Token message))
            {
                return new Token
                (
                    TokenType.Command,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    message
                );
            }
            else
            {
                //can return empty
                return new Token
                (
                    TokenType.Command
                );
            }
        }

        //parse an unknown command/code
        private static bool TryParseErroneousMessage
        (
            ref SequenceReader<byte> reader,
            out Token message
        )
        {
            SequencePosition startPosition = reader.Position;
            Token cmd;

            //try parsing the command or code
            if(!(TryParseCommandName(ref startPosition, ref reader, out cmd)
                 || TryParseCommandCode(ref startPosition, ref reader, out cmd)))
            {
                message = null;
                return false;
            }

            //parse the message parameters and add to children
            Token par = ParseParams(ref reader);

            Combine(cmd, par);

            message = new Token
            (
                TokenType.ErroneousMessage,
                reader.Sequence.Slice(startPosition, reader.Position),
                cmd
            );

            return true;
        }

        //parse a command name / continue a command name
        private static bool TryParseCommandName
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

            commandName = new Token
            (
                TokenType.CommandName,
                reader.Sequence.Slice(startPosition, reader.Position)
            );

            return true;
        }

        //parse a command code / continue a command code
        private static bool TryParseCommandCode
        (
            ref SequencePosition startPosition,
            ref SequenceReader<byte> reader,
            out Token commandCode,
            int recognizedCount = 0
        )
        {
            //TODO: compute recognizedcount from SequencePosition

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

            commandCode = new Token
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
                return new Token
                (
                    TokenType.Params,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    paramsPrefix
                );
            }
            else
            {
                //return empty
                return new Token
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
            if(MatchSpaces(ref reader))
            {
                //get the parameter suffix containing one or more paramaters,
                //the called method recurses back on the current to check for
                //multiple paramaters
                Token paramsSuffix = ParseParamsSuffix(ref reader);

                paramsPrefix = new Token
                (
                    TokenType.ParamsPrefix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    paramsSuffix
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
        private static Token ParseParamsSuffix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //This is always the last parameter, do not continue params parsing
            if(TryParseTrailing(ref reader, out Token trailing))
            {
                return new Token
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
                    Combine(middle, paramsPrefix);
                }

                return new Token
                (
                    TokenType.ParamsSuffix,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    middle
                );
            }
            else
            {
                return new Token
                (
                    TokenType.ParamsSuffix
                );
            }
        }

        //try parse a middle
        private static bool TryParseMiddle
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

                Combine(middlePrefix, middleSuffix);

                middle = new Token
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
        private static bool TryParseMiddlePrefixList
        (
            ref SequenceReader<byte> reader,
            out Token middlePrefix
        )
        {
            SequencePosition startPosition = reader.Position;
            Token firstMiddlePrefixBase = null, previous = null,
                      middlePrefixBase;
            bool found = false;
            byte value;

            while(true)
            {
                if(IsTerminal(Terminal.Space, ref reader, out value)
                   || IsTerminal(Terminal.Colon, value))
                {
                    break;
                }
                //try match a digit, hex letter or comma
                else if(IsDigit(value)
                    || IsUpperCaseHexLetter(value)
                    || IsLowerCaseHexLetter(value)
                    || IsTerminal(Terminal.Comma, value))
                {
                    reader.Advance(1);
                }
                //try match format, UTF-8 without space, colon, NUL, CR, LF,
                //hex letter, digit or comma
                else if(TryParseMiddlePrefixBase
                        (ref reader, out middlePrefixBase))
                {
                    if(firstMiddlePrefixBase is null)
                    {
                        firstMiddlePrefixBase = middlePrefixBase;
                    }

                    Combine(previous, middlePrefixBase);

                    previous = middlePrefixBase;
                }
                else
                {
                    break;
                }

                found = true;
            }

            if(!found)
            {
                middlePrefix = null;
                return false;
            }

            middlePrefix = new Token
            (
                TokenType.MiddlePrefixList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstMiddlePrefixBase
            );

            return true;
        }

        //try match format, UTF-8 without space, colon, NUL, CR, LF,
        //hex letter, digit or comma
        private static bool TryParseMiddlePrefixBase
        (
            ref SequenceReader<byte> reader,
            out Token middlePrefixBase
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstChild = null, previous = null, child = null;
            byte value;

            while(true)
            {
                if(IsTerminal(Terminal.Space, ref reader, out value)
                   || IsTerminal(Terminal.Colon, value))
                {
                    break;
                }
                //try match a subset of UTF-8 with CTCP
                else if(IsUTF8WithoutAlphaNumericNullCrLfSpaceCommaColon(value)
                   || IsLowerCaseLetter(value)
                   || IsUpperCaseLetter(value)
                   || IsTerminal(Terminal.CTCP, value))
                {
                    reader.Advance(1);
                }
                //or try to match a format
                else if(TryParseFormat(ref reader, out child))
                {
                    if(firstChild is null)
                    {
                        firstChild = child;
                    }

                    Combine(previous, child);

                    previous = child;
                }
                else
                {
                    break;
                }

                found = true;
            }

            if(!found)
            {
                middlePrefixBase = null;
                return false;
            }

            middlePrefixBase = new Token
            (
                TokenType.MiddlePrefixBaseList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstChild
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
                return new Token
                (
                    TokenType.MiddleSuffix
                );
            }
            //or return a middle prefix WITH colon
            else
            {
                return new Token
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
            Token firstChild = null, previous = null, child = null;
            byte value;

            while(true)
            {
                if(IsTerminal(Terminal.Space, ref reader, out value))
                {
                    break;
                }
                //try match a subset of UTF-8 with CTCP
                else if(IsTerminal(Terminal.Colon, value))
                {
                    reader.Advance(1);
                }
                else if(TryParseMiddlePrefixList
                        (ref reader, out child))
                {
                    if(firstChild is null)
                    {
                        firstChild = child;
                    }

                    Combine(previous, child);

                    previous = child;
                }
                else
                {
                    break;
                }

                found = true;
            }

            if(!found)
            {
                middlePrefixWithColonList = null;
                return false;
            }

            middlePrefixWithColonList = new Token
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
            if(MatchTerminal(Terminal.Colon, ref reader))
            {
                //a trailing prefix can be empty
                Token trailingPrefix = ParseTrailingPrefix(ref reader);

                trailing = new Token
                (
                    TokenType.Trailing,
                    reader.Sequence.Slice(startPosition, reader.Position),
                    trailingPrefix
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
        private static Token ParseTrailingPrefix
        (
            ref SequenceReader<byte> reader
        )
        {
            SequencePosition startPosition = reader.Position;

            //try parsing the trailing list
            if(TryParseTrailingList(ref reader, out Token trailingList))
            {
                return new Token
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
                return new Token
                (
                    TokenType.TrailingPrefix
                );
            }
        }

        //try parsing the trailing list consiting of middlePrefixColon and space
        private static bool TryParseTrailingList
        (
            ref SequenceReader<byte> reader,
            out Token trailingSuffix
        )
        {
            SequencePosition startPosition = reader.Position;
            bool found = false;
            Token firstMiddlePrefix = null, previous = null,
                  middlePrefixWithColon;

            while(true)
            {
                if(IsTerminal(Terminal.Space, ref reader, out _))
                {
                    reader.Advance(1);
                }
                else if(TryParseMiddlePrefixWithColonList
                    (ref reader, out middlePrefixWithColon))
                {
                    //else add the middlePrefixColonList to the children
                    if(firstMiddlePrefix is null)
                    {
                        firstMiddlePrefix = middlePrefixWithColon;
                    }

                    Combine(previous, middlePrefixWithColon);

                    previous = middlePrefixWithColon;
                }
                else
                {
                    break;
                }

                found = true;
            }

            if(!found)
            {
                trailingSuffix = null;
                return false;
            }

            trailingSuffix = new Token
            (
                TokenType.TrailingList,
                reader.Sequence.Slice(startPosition, reader.Position),
                firstMiddlePrefix
            );

            return true;
        }

        //parse the CR LF ath the end of the message
        private static Token ParseCrLf(ref SequenceReader<byte> reader)
        {
            SequencePosition startPosition = reader.Position;

            if(MatchTerminal(Terminal.CarriageReturn, ref reader))
            {
                if(MatchTerminal(Terminal.LineFeed, ref reader))
                {
                    return new Token
                    (
                        TokenType.CrLf,
                        reader.Sequence.Slice(startPosition, reader.Position)
                    );
                }
                else
                {
                    throw new ParserException("CR LF expected");
                }
            }
            else if(MatchTerminal(Terminal.LineFeed, ref reader))
            {
                return new Token
                (
                    TokenType.CrLf,
                    reader.Sequence.Slice(startPosition, reader.Position)
                );
            }

            throw new ParserException("CR LF expected");
        }

    }
}
