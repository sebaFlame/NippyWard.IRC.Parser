using System;
using System.Buffers;

using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser
{
    public abstract class BaseIRCMessageFactory : IDisposable
    {
        private Token _message;
        private bool IsConstructed
            => this._message.TokenType == TokenType.Message;
        private int _keepParamsCount;
        private bool _keepSourcePrefix;
        private bool _keepTags;
        private readonly int _defaultKeepParamsCount;
        private readonly bool _defaultKeepSourcePrefix;
        private readonly bool _defaultKeepTags;

        //max message lengths
        private const int _MaxMessageLength = 512;
        private const int _MaxTagsLength = 8192;
        private const int _MaxParameters = 15;
        private const int _AverageWordLength = 16;

        [ThreadStatic]
        private static FactoryTokenVisitor _FactoryTokenVisitor;

        [ThreadStatic]
        private static MessageLengthVisitor _MessageLengthVisitor;

        private static readonly byte[] _Seperators = new byte[]
        {

            0x3A, // ':'
            0x20, // '\s'
            0x40, // '@'
            0x0D, // '\r'
            0x0A, // '\n'
            0x3D, // '='
            0x3B, // ';'
        };

        private static ReadOnlyMemory<byte> Colon
            => new ReadOnlyMemory<byte>(_Seperators, 0, 1);

        private static ReadOnlyMemory<byte> Space
            => new ReadOnlyMemory<byte>(_Seperators, 1, 1);

        private static ReadOnlyMemory<byte> At
            => new ReadOnlyMemory<byte>(_Seperators, 2, 1);

        private static ReadOnlyMemory<byte> CrLf
            => new ReadOnlyMemory<byte>(_Seperators, 3, 2);

        private static ReadOnlyMemory<byte> EqualsSign
            => new ReadOnlyMemory<byte>(_Seperators, 5, 1);

        private static ReadOnlyMemory<byte> Semicolon
            => new ReadOnlyMemory<byte>(_Seperators, 6, 1);

        //reusable IRC message factory. Always call Reset before constructing a
        //message!
        public BaseIRCMessageFactory
        (
            int keepParamsCount,
            bool keepSourcePrefx,
            bool keepTags
        )
        {
            this._defaultKeepParamsCount = keepParamsCount;
            this._defaultKeepSourcePrefix = keepSourcePrefx;
            this._defaultKeepTags = keepTags;

            this.Reset();
        }

        public BaseIRCMessageFactory()
            : this(0, true, true)
        { }

        ~BaseIRCMessageFactory()
        {
            this.Dispose(false);
        }

        public Token ConstructMessage()
        {
            CheckIfConstructed(this);

            FactoryTokenVisitor tokenVisitor = GetFactoryTokenVisitor();
            Token constructedMessage;

            constructedMessage = tokenVisitor.ConstructMessage(this._message);

            return constructedMessage;
        }

        public BaseIRCMessageFactory KeepParamsOnNextMessage(int count)
        {
            CheckIfFirstMessage(this, this.GetMessage());

            if(count > _MaxParameters / 2)
            {
                throw new InvalidOperationException
                (
                    "Can not keep all parameters"
                );
            }

            this._keepParamsCount = count;

            return this;
        }

        public BaseIRCMessageFactory KeepSourcePrefixOnNextMessage()
        {
            CheckIfFirstMessage(this, this.GetMessage());

            this._keepSourcePrefix = true;

            return this;
        }

        public BaseIRCMessageFactory RemoveSourcePrefixOnNextMessage()
        {
            CheckIfFirstMessage(this, this.GetMessage());

            this._keepSourcePrefix = false;

            return this;
        }

        public BaseIRCMessageFactory KeepTagsOnNextMessage()
        {
            CheckIfFirstMessage(this, this.GetMessage());

            this._keepTags = true;

            return this;
        }

        public BaseIRCMessageFactory RemoveTagsOnNextMessage()
        {
            CheckIfFirstMessage(this, this.GetMessage());

            this._keepTags = false;

            return this;
        }

        private Token GetMessage()
            => this._message.GetLastToken();

        public BaseIRCMessageFactory SourcePrefix(string str)
            => this.SourcePrefix((Utf8String)str);

        //add (or replace!) a source prefix and return the new message
        public BaseIRCMessageFactory SourcePrefix(Utf8String str)
        {
            CheckIfConstructed(this);

            if(str is null
               || str.IsEmpty)
            {
                return this;
            }

            Token message = this.GetMessage();

            CheckIfFirstMessage(this, message);

            if(!message.TryGetFirstTokenOfType
            (
                TokenType.SourcePrefix,
                out Token sourcePrefix
            ))
            {
                throw new InvalidOperationException
                (
                    "No source prefix token found!"
                );
            }

            if(!sourcePrefix.IsEmpty)
            {
                throw new ArgumentException
                (
                    "Source prefix has already been defined"
                );
            }

            MessageLengthVisitor lengthVisitor = GetMessageLengthVisitor();

            int messageLength = lengthVisitor.ComputeTokenLength
            (
                message
            );

            if(MessageWillExceedMaxLength
            (
                message,
                messageLength,
                str.Length + 2, //account for ':' and ' '
                out _
            ))
            {
                throw new ArgumentOutOfRangeException
                (
                    $"Message exceeds {_MaxMessageLength} bytes"
                );
            }

            //parse source prefix with parser to validate
            SequenceReader<byte> reader = str.CreateSequenceReader();
            Token sourcePrefixTarget
                = IRCParser.ParseSourcePrefixTarget(ref reader);

            LinkConstructedSourcePrefix(sourcePrefix, sourcePrefixTarget);

            return this;
        }

        public BaseIRCMessageFactory Tag(string tagKey, string tagValue)
            => this.Tag((Utf8String)tagKey, (Utf8String)tagValue);

        public BaseIRCMessageFactory Tag(Utf8String tagKey, Utf8String tagValue)
        {
            //check if the message has already been constructed
            CheckIfConstructed(this);

            if((tagKey is null || tagKey.IsEmpty)
               && (tagValue is null || tagValue.IsEmpty))
            {
                return this;
            }

            Token message = this.GetMessage();

            //check if it's the first message in the chain
            CheckIfFirstMessage(this, message);

            //fetch the TagPrefix token
            message.TryGetFirstTokenOfType
            (
                TokenType.TagPrefix,
                out Token tagPrefix
            );

            //fetch the TagsSuffix token if there is any
            tagPrefix.TryGetFirstTokenOfType
            (
                TokenType.TagsSuffix,
                out Token tagsSuffix
            );

            //compute the length of all current tags
            MessageLengthVisitor lengthVisitor = GetMessageLengthVisitor();
            int tagLength = lengthVisitor.ComputeTokenLength
            (
                tagPrefix
            );

            if(TagsWillExceedMaxLength
            (
                tagLength,
                tagValue.IsEmpty
                    ? tagKey.Length
                    :
                    (
                        (tagsSuffix is null || tagsSuffix.IsEmpty)
                            ? tagKey.Length
                                + tagValue.Length + 1 //plus '='
                            : tagKey.Length
                                + tagValue.Length + 2 //plus '=' and ';'
                    ),
                 out _
            ))
            {
                throw new ArgumentOutOfRangeException
                (
                    $"Tags exceed {_MaxTagsLength} bytes"
                );
            }

            //parse tag key with parser to validate
            SequenceReader<byte> reader = tagKey.CreateSequenceReader();
            Token tagKeyToken = IRCParser.ParseTagKey
            (
                ref reader
            );

            //first create a new tag
            Token tag, tagSuffix;
            if(tagValue.IsEmpty)
            {
                tagSuffix = new Token(TokenType.TagSuffix);
            }
            else
            {
                //parse and escape the tag value
                Token tagValueToken = tagValue.TagEscape();

                //add an equality sign for the value
                Token equal = new Token
                (
                    TokenType.EqualitySign,
                    EqualsSign
                );

                equal.Combine(tagValueToken);

                //create tagsuffix with the tag value
                tagSuffix = new Token
                (
                    TokenType.TagSuffix,
                    equal
                );
            }

            //add tagSuffix with possible value
            tagKeyToken.Combine(tagSuffix);

            //create new tag with tag key and its suffix
            tag = new Token
            (
                TokenType.Tag,
                tagKeyToken
            );

            LinkConstructedTag(tagPrefix, tag);

            return this;
        }

        public BaseIRCMessageFactory Verb(string verb)
            => this.Verb((Utf8String)verb);

        //add a command or a code
        public BaseIRCMessageFactory Verb
        (
            Utf8String verb
        )
        {
            CheckIfConstructed(this);

            if(verb is null
               || verb.IsEmpty)
            {
                return this;
            }

            Token message = this.GetMessage();

            //only possible to set command on first command
            CheckIfFirstMessage(this, message);

            if(!message.TryGetFirstTokenOfType
            (
                TokenType.Verb,
                out Token command
            ))
            {
                throw new InvalidOperationException
                (
                    "No command token found!"
                );
            }

            if(!command.IsEmpty)
            {
                throw new ArgumentException
                (
                    "Command has already been defined"
                );
            }

            MessageLengthVisitor lengthVisitor = GetMessageLengthVisitor();

            int messageLength = lengthVisitor.ComputeTokenLength
            (
                message
            );

            if(MessageWillExceedMaxLength
            (
                message,
                messageLength,
                verb.Length,
                out _
            ))
            {
                throw new ArgumentOutOfRangeException
                (
                    $"Message exceeds {_MaxMessageLength} bytes"
                );
            }

            //parse source prefix with parser to validate
            SequenceReader<byte> reader = verb.CreateSequenceReader();
            SequencePosition startPosition = reader.Position;
            Token commandNameOrCode;
            if(!(IRCParser.TryParseCommandName
            (
                ref startPosition,
                ref reader,
                out commandNameOrCode
            )
                || IRCParser.TryParseCommandCode
                (
                    ref startPosition,
                    ref reader,
                    out commandNameOrCode
                )))
            {
                throw new NotSupportedException
                (
                    "Verb could not be parsed"
                );
            }

            LinkConstructedVerb(command, commandNameOrCode);

            return this;
        }

        public BaseIRCMessageFactory Parameter(string parameter)
            => this.Parameter((Utf8String)parameter);

        public BaseIRCMessageFactory Parameter(Utf8String parameter)
        {
            CheckIfConstructed(this);

            //paramter can be empty
            if(parameter is null)
            {
                return this;
            }

            Token message = this.GetMessage();

            if(!message.TryGetFirstTokenOfType
            (
                TokenType.Params,
                out Token parameters
            ))
            {
                throw new InvalidOperationException
                (
                    "No Params token found, no command has been defined yet"
                );
            }

            //check if too many parameters
            if(ParametersWillExceedMax(parameters))
            {
                return this.TooManyParameters(parameter);
            }

            //compute current length of whole message
            MessageLengthVisitor lengthVisitor = GetMessageLengthVisitor();
            int messageLength = lengthVisitor.ComputeTokenLength
            (
                message
            );

            int parameterExtraLength;
            Token space = new Token
            (
                TokenType.Space,
                Space
            );
            Token paramsSuffix = new Token(TokenType.ParamsSuffix);

            //link space and suffix together to form the parameter
            space.Combine(paramsSuffix);

            Token paramsPrefix = new Token
            (
                TokenType.ParamsPrefix,
                space
            );

            //compute length of future parameter
            SequenceReader<byte> reader = parameter.CreateSequenceReader();
            if(parameter.IsEmpty)
            {
                //initialize a colon for the trailing parameter
                Token colon = new Token
                (
                    TokenType.Colon,
                    Colon
                );

                //provide an empty trailing prefix
                Token trailingPrefix = new Token(TokenType.TrailingPrefix);

                colon.Combine(trailingPrefix);

                paramsSuffix.Child = new Token
                (
                    TokenType.Trailing,
                    colon
                );
                parameterExtraLength = 2; //account for ' ' and ':'
            }
            //always try to parse middle
            else
            {
                //parameter not fully parsed, it's a trailing parameter
                if(!IRCParser.TryParseMiddle(ref reader, out Token middle)
                   || middle.Length != parameter.Length)
                {
                    reader.Rewind(reader.Consumed);

                    //initialize a colon for the trailing parameter
                    Token colon = new Token
                    (
                        TokenType.Colon,
                        Colon
                    );

                    //parse the trailing prefix
                    Token trailingPrefix = IRCParser.ParseTrailingPrefix
                    (
                        ref reader
                    );

                    colon.Combine(trailingPrefix);

                    paramsSuffix.Child = new Token
                    (
                        TokenType.Trailing,
                        colon
                    );
                    parameterExtraLength = 2; //account for ' ' and ':'
                }
                //a middle parameter
                else
                {
                    paramsSuffix.Child = middle;
                    parameterExtraLength = 1; // account for ' '
                }
            }

            //there is already a treailing defined
            if(paramsSuffix.Child.TokenType == TokenType.Trailing
                && parameters.TryGetFirstTokenOfType
                (
                    TokenType.Trailing,
                    out Token _
                ))
            {
                paramsPrefix.Dispose();

                throw new InvalidProgramException
                (
                    "A trailing parameter has already been defined"
                );
            }

            if(MessageWillExceedMaxLength
            (
                message,
                messageLength,
                parameter.Length + parameterExtraLength,
                out int extraLength
            ))
            {
                paramsPrefix.Dispose();

                return this.ParameterTooLong(parameter, extraLength);
            }

            LinkConstructedParameter(parameters, paramsPrefix);

            return this;
        }

        protected abstract BaseIRCMessageFactory TooManyParameters
        (
            Utf8String parameter
        );

        protected abstract BaseIRCMessageFactory ParameterTooLong
        (
            Utf8String parameter,
            int extraLength //should be negative
        );


        protected BaseIRCMessageFactory AddParameterToNewMessage
        (
            Utf8String parameter
        )
        {
            this.CreateNewConstructedMessage();
            return this.Parameter(parameter);
        }

        protected BaseIRCMessageFactory SplitParameterAndAddToNewMessage
        (
            Utf8String parameter,
            int extraLength //should be negative
        )
        {
            SplitUtf8StringOnCharacter
            (
                parameter,
                0x20,
                parameter.Length + extraLength, //extraLength is negative
                out Utf8String firstParameter,
                out Utf8String secondParameter
            );

            this.Parameter(firstParameter);

            this.CreateNewConstructedMessage();

            return this.Parameter(secondParameter);
        }

        protected Token CreateNewConstructedMessage()
        {
            Token oldMessage = this._message?.GetLastToken();
            Token tagPrefix = new Token(TokenType.TagPrefix);

            tagPrefix
                .Combine(new Token(TokenType.SourcePrefix))
                .Combine(new Token(TokenType.Verb))
                .Combine(new Token(TokenType.Params))
                .Combine(new Token(TokenType.CrLf, CrLf));

            Token newMessage = new Token
            (
                TokenType.ConstructedMessage,
                tagPrefix
            );

            //if first message, return it
            if(oldMessage is null)
            {
                return newMessage;
            }

            //else combine with first message
            oldMessage.Combine(newMessage);

            if(this._keepTags
               && oldMessage.TryGetFirstTokenOfType
            (
                TokenType.TagPrefix,
                out Token oldTagPrefix
            ))
            {
                Token tagKey, tagValue;

                foreach(Token tag in oldTagPrefix
                        .GetAllTokensOfType(TokenType.Tag))
                {
                    tagKey = null;
                    tagValue = null;

                    tag.TryGetFirstTokenOfType
                    (
                        TokenType.TagKey,
                        out tagKey
                    );

                    tag.TryGetFirstTokenOfType
                    (
                        TokenType.TagValue,
                        out tagValue
                    );

                    AddTag(newMessage, tagKey, tagValue);
                }
            }

            if(this._keepSourcePrefix
               && oldMessage.TryGetFirstTokenOfType
            (
                TokenType.SourcePrefixTarget,
                out Token sourcePrefixTarget
            ))
            {
                AddSourcePrefix(newMessage, sourcePrefixTarget);
            }

            //verify if a verb has been set
            if(oldMessage.TryGetFirstTokenOfType
            (
                TokenType.Verb,
                out Token oldCommand
            )
                && !oldCommand.IsEmpty)
            {
                Token commandNameOrCode;
                if(!(oldCommand.TryGetLastOfTokenType
                (
                    TokenType.CommandName,
                    out commandNameOrCode
                ) || oldCommand.TryGetFirstTokenOfType
                    (
                        TokenType.CommandCode,
                        out commandNameOrCode
                    )
                ))
                {
                    throw new InvalidOperationException
                    (
                        "No correctly constructed command found"
                    );
                }

                AddVerb
                (
                    newMessage,
                    commandNameOrCode
                );
            }
            //no command AND no parameters
            else
            {
                return newMessage;
            }

            //verify if params have been set
            if(this._keepParamsCount > 0
                && oldMessage.TryGetFirstTokenOfType
                (
                    TokenType.Params,
                    out Token oldParameters
                )
                && !oldParameters.IsEmpty)
            {
                int length = this._keepParamsCount;

                foreach(Token t in oldParameters.GetAllTokensOfType
                (
                    TokenType.ParamsSuffix
                ))
                {
                    AddParameter
                    (
                        newMessage,
                        t
                    );

                    if(--length == 0)
                    {
                        break;
                    }
                }
            }

            return newMessage;
        }

        private static void AddTag
        (
            Token newMessage,
            Token oldTagKey,
            Token oldTagValue
        )
        {
            //fetch the TagPrefix token
            newMessage.TryGetFirstTokenOfType
            (
                TokenType.TagPrefix,
                out Token tagPrefix
            );

            //first create a new tag
            Token tag, tagSuffix, tagKey;

            tagKey = new Token
            (
                TokenType.TagKey,
                oldTagKey.Sequence
            );

            if(oldTagValue is null
               || oldTagValue.IsEmpty)
            {
                tagSuffix = new Token(TokenType.TagSuffix);
            }
            else
            {
                //create tagsuffix with the tag value
                tagSuffix = new Token
                (
                    TokenType.TagSuffix,
                    new Token
                    (
                        TokenType.TagValue,
                        oldTagValue.Sequence
                    )
                );
            }

            //add tagSuffix with possible value
            tagKey.Combine(tagSuffix);

            //create new tag with tag key and its suffix
            tag = new Token
            (
                TokenType.Tag,
                tagKey
            );

            LinkConstructedTag(tagPrefix, tag);
        }

        private static void AddSourcePrefix
        (
            Token newMessage,
            Token oldSourcePrefixTarget
        )
        {
            //fetch the new SourcePrefix token
            if(newMessage.TryGetFirstTokenOfType
            (
                TokenType.SourcePrefix,
                out Token sourcePrefix
            ))
            {
                Token sourcePrefixTarget = new Token
                (
                    TokenType.SourcePrefix,
                    oldSourcePrefixTarget.Sequence
                );

                LinkConstructedSourcePrefix(sourcePrefix, sourcePrefixTarget);
            }
        }

        private static void AddVerb
        (
            Token newMessage,
            Token oldVerb
        )
        {
            newMessage.TryGetFirstTokenOfType
            (
                TokenType.Verb,
                out Token command
            );

            Token newVerb = new Token
            (
                oldVerb.TokenType,
                oldVerb.Sequence
            );

            LinkConstructedVerb
            (
                command,
                newVerb
            );
        }

        private static void AddParameter
        (
            Token newMessage,
            Token oldParamsSuffix
        )
        {
            newMessage.TryGetFirstTokenOfType
            (
                TokenType.Params,
                out Token parameters
            );

            Token parameter;
            if(oldParamsSuffix.IsEmpty)
            {
                parameter = new Token(TokenType.ParamsSuffix);
            }
            else
            {
                //actual parameter should always be first child
                parameter = new Token
                (
                    TokenType.ParamsSuffix,
                    oldParamsSuffix.Child.Sequence
                );

                //next child can be null or a ParamsPrefix
            }

            Token parameterPrefix = new Token
            (
                TokenType.ParamsPrefix,
                parameter
            );

            LinkConstructedParameter
            (
                parameters,
                parameterPrefix
            );
        }

        private static void LinkConstructedSourcePrefix
        (
            Token sourcePrefix,
            Token sourcePrefixTarget
        )
        {
            Token colon = new Token
            (
                TokenType.Colon,
                Colon
            );

            //first link a colon and the target together
            colon.Combine(sourcePrefixTarget);

            Token space = new Token
            (
                TokenType.Space,
                Space
            );

            //then add a trailing space
            sourcePrefixTarget.Combine(space);

            //add new child to source prefix
            sourcePrefix.Child = colon;
        }

        private static void LinkConstructedTag(Token tagPrefix, Token tag)
        {
            //fetch the TagsSuffix token if there is any
            if(tagPrefix.TryGetFirstTokenOfType
            (
                TokenType.TagsSuffix,
                out Token tagsSuffix
            ))
            {
                //atleast 1 tag
                //ensure a tagslist is provided
                if(!tagsSuffix.TryGetFirstTokenOfType
                (
                    TokenType.TagsList,
                    out Token tagsList
                ))
                {
                    tagsSuffix.Child
                        = tagsList
                        = new Token(TokenType.TagsList);
                }

                Token semiColon = new Token
                (
                    TokenType.Semicolon,
                    Semicolon
                );

                //link in the new tag with a semicolon
                semiColon.Combine(tag);

                if(tagsList.Child is null)
                {
                    tagsList.Child = semiColon;
                }
                else
                {
                    tagsList.Child.Combine(semiColon);
                }
            }
            else
            {
                //first tag

                //initialize empty TagsSuffix token
                tagsSuffix = new Token(TokenType.TagsSuffix);

                //add an empty TagsSuffix to the tags
                tag.Combine(tagsSuffix);

                //create tags token
                Token tags = new Token
                (
                    TokenType.Tags,
                    tag
                );

                //first add the at prefix
                Token at = new Token
                (
                    TokenType.AtSign,
                    At
                );

                //link in the tag(s)
                at.Combine(tags);

                //add trailing space
                Token space = new Token
                (
                    TokenType.Space,
                    Space
                );

                tags.Combine(space);

                //add new child to tag prefix
                tagPrefix.Child = at;
            }
        }

        private static void LinkConstructedVerb
        (
            Token command,
            Token verb
        )
            => command.Child = verb;

        private static void LinkConstructedParameter
        (
            Token parameters,
            Token paramsPrefix
        )
        {
            if(parameters.Child is null)
            {
                parameters.Child = paramsPrefix;
            }
            else if(parameters.TryGetLastOfTokenType
            (
                TokenType.ParamsSuffix,
                out Token lastParamsSuffix
            ))
            {
                //should be the next of the child, which can only be middle
                lastParamsSuffix.Child.Combine(paramsPrefix);
            }
            else
            {
                throw new InvalidOperationException
                (
                    "Invalid message structure"
                );
            }
        }

        //MUST be called before every construction
        public virtual BaseIRCMessageFactory Reset()
        {
            this._message?.Dispose();
            this._message = this.CreateNewConstructedMessage();

            this._keepParamsCount = this._defaultKeepParamsCount;
            this._keepSourcePrefix = this._defaultKeepSourcePrefix;
            this._keepTags = this._defaultKeepTags;

            return this;
        }

        public void Dispose()
            => this.Dispose(true);

        public void Dispose(bool isDisposing)
        {
            this.Reset();

            if(!isDisposing)
            {
                return;
            }

            GC.SuppressFinalize(this);
        }

        #region helper methods
        private static FactoryTokenVisitor GetFactoryTokenVisitor()
        {
            if(_FactoryTokenVisitor is null)
            {
                _FactoryTokenVisitor = new FactoryTokenVisitor();
            }

            return _FactoryTokenVisitor;
        }

        private static MessageLengthVisitor GetMessageLengthVisitor()
        {
            if(_MessageLengthVisitor is null)
            {
                _MessageLengthVisitor = new MessageLengthVisitor();
            }

            return _MessageLengthVisitor;
        }

        private static bool MessageWillExceedMaxLength
        (
            Token message,
            int messageLength,
            int addedLength,
            out int lengthLeft //can be negative
        )
        {
            //remove tags length
            if(message.TryGetFirstTokenOfType
            (
                TokenType.TagPrefix,
                out Token tagPrefix
            ))
            {
                MessageLengthVisitor lengthVisitor = GetMessageLengthVisitor();
                int tagPrefixLength
                    = lengthVisitor.ComputeTokenLength(tagPrefix);

                messageLength -= tagPrefixLength;
            }

            //get current length left
            lengthLeft = _MaxMessageLength - messageLength;

            return (lengthLeft -= addedLength) < 0;
        }

        private static bool TagsWillExceedMaxLength
        (
            int tagsLength,
            int addedLength,
            out int lengthLeft
        )
        {
            int length = tagsLength;

            length += addedLength;

            lengthLeft = _MaxTagsLength - length;

            return (lengthLeft -= addedLength) < 0;
        }

        private static bool ParametersWillExceedMax
        (
            Token parameters
        )
        {
            //account for the extra parameter
            int count = 1;

            foreach(Token t
                    in parameters.GetAllTokensOfType(TokenType.ParamsSuffix))
            {
                count++;
            }

            return count > _MaxParameters;
        }

        private static void CheckIfConstructed(BaseIRCMessageFactory factory)
        {
            if(factory.IsConstructed)
            {
                throw new InvalidOperationException
                (
                    "Message has already been constructed"
                );
            }
        }

        private static void CheckIfFirstMessage
        (
            BaseIRCMessageFactory factory,
            Token message
        )
        {
            if(!object.ReferenceEquals(factory._message, message))
            {
                throw new ArgumentException
                (
                    "Not the first message in the sequence"
                );
            }
        }

        //split on a character OR do a hard split
        protected static void SplitUtf8StringOnCharacter
        (
            Utf8String str,
            byte character,
            int maxParamterLength,
            out Utf8String firstStr,
            out Utf8String secondStr
        )
        {
            ReadOnlySequence<byte> sequence;
            ReadOnlySequence<byte> buffer = str.Buffer;
            if(str.Length > _AverageWordLength)
            {
                sequence = buffer.Slice
                (
                    maxParamterLength - _AverageWordLength,
                    _AverageWordLength
                );
            }
            else
            {
                sequence = str.Buffer;
            }

            SequenceReader<byte> reader = new SequenceReader<byte>(sequence);
            SequencePosition position = default;
            ReadOnlySequence<byte> firstSequence, secondSequence;

            while(reader.TryReadTo(out firstSequence, character))
            {
                position = firstSequence.End;
            }

            if(position.Equals(default))
            {
                firstSequence = sequence;
                secondSequence = buffer.Slice(maxParamterLength);
            }
            else
            {
                long length = buffer.GetOffset(position) + 1;
                firstSequence = buffer.Slice(0, length);
                secondSequence = buffer.Slice(length);
            }

            firstStr = new Utf8String(firstSequence);
            secondStr = new Utf8String(secondSequence);
        }

        protected static Exception ThrowTooManyParameters()
            => new ArgumentException
            (
                "Too many parameters"
            );

        protected static Exception ThrowParameterTooLong()
            => new ArgumentException
            (
                "Parameter exceeds max length"
            );

        #endregion
    }
}