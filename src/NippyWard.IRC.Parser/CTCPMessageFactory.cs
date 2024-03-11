using NippyWard.IRC.Parser.Tokens;
using NippyWard.Text;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NippyWard.IRC.Parser
{
    internal class CTCPMessageFactory : ICTCPMessageFactory
    {
        private Token _ctcp;

        private static readonly byte[] _Seperators = new byte[]
        {
            0x01, //CTCP data marker
            0x20 //space
        };

        private static ReadOnlyMemory<byte> Delimiter
            => new ReadOnlyMemory<byte>(_Seperators, 0, 1);

        private static ReadOnlyMemory<byte> Space
            => new ReadOnlyMemory<byte>(_Seperators, 1, 1);

        public CTCPMessageFactory()
        {
            this._ctcp = this.CreateNewMessage();
        }

        private Token CreateNewMessage()
        {
            Token delim = Token.Create
            (
                TokenType.CTCP,
                Delimiter
            );

            delim
                .Combine(Token.Create(TokenType.CTCPCommand))
                .Combine(Token.Create(TokenType.CTCPParams))
                .Combine
                (
                    Token.Create
                    (
                        TokenType.CTCPMessageSuffix,
                        Token.Create(TokenType.CTCP, Delimiter)
                    )
                );

            return Token.Create
            (
                TokenType.CTCPMessage,
                delim
            );
        }

        public ICTCPMessageFactory Command(string command)
            => this.Command((Utf8String)command);

        public ICTCPMessageFactory Command(Utf8String command)
        {
            if (command is null
                || command.IsEmpty)
            {
                return this;
            }

            if (!this._ctcp.TryGetFirstTokenOfType
            (
                TokenType.CTCPCommand,
                out Token cmd
            ))
            {
                throw new InvalidOperationException
                (
                    "No CTCP command token found!"
                );
            }

            if (!cmd.IsEmpty)
            {
                throw new ArgumentException
                (
                    "CTCP Command has already been defined"
                );
            }

            SequenceReader<byte> reader = command.CreateSequenceReader();
            if(!IRCParser.TryParseCTCPMiddle
            (
                ref reader,
                out Token middle
            ))
            {
                throw new NotSupportedException
                (
                    "CTCP command could not be parsed"
                );
            }

            //link the new command
            cmd.Child = middle;

            return this;
        }

        public ICTCPMessageFactory Parameter(string parameter)
            => this.Parameter((Utf8String)parameter);

        public ICTCPMessageFactory Parameter(Utf8String parameter)
        {
            if (parameter is null)
            {
                return this;
            }

            //find the CTCPParams
            if (!this._ctcp.TryGetFirstTokenOfType
            (
                TokenType.CTCPParams,
                out Token pars
            ))
            {
                throw new InvalidOperationException
                (
                    "No CTCP parameter token found!"
                );
            }

            //create a CTCPParamsSuffix if necessary
            Token suffix;
            if (!pars.TryGetFirstTokenOfType
            (
                TokenType.CTCPParamsSuffix,
                out suffix
            ))
            {
                Token space = Token.Create
                (
                    TokenType.Space,
                    Space
                );

                suffix = Token.Create
                (
                    TokenType.CTCPParamsSuffix
                );

                space.Combine(suffix);

                pars.Child = space;
            }

            SequenceReader<byte> reader = parameter.CreateSequenceReader();
            //first check if empty, then leave CTCPParamsSuffix empty
            if (parameter.IsEmpty)
            {
                //NOP
            }
            else if (IRCParser.TryParseCTCPParamsMiddle
            (
                ref reader,
                out Token middleParam
            ))
            {
                if (!suffix.TryGetFirstTokenOfType
                (
                    TokenType.CTCPParamsMiddle,
                    out Token paramsMiddle
                ))
                {
                    //first parameter, link in the newly parsed middleParam
                    suffix.Child = middleParam;
                }
                else
                {
                    //get the first child of the newly parsed middleParam
                    Token firstChild = middleParam.Child;

                    //reset the child for correct disposal
                    middleParam.Child = null;
                    middleParam.Dispose();

                    if (paramsMiddle.IsEmpty)
                    {
                        paramsMiddle.Child = firstChild;
                    }
                    else
                    {
                        //seperate tokens by space
                        Token space = Token.Create
                        (
                            TokenType.Space,
                            Space
                        );

                        //link the space with the newly parsed middle token
                        space.Combine(firstChild);

                        //find the last child of paramsMiddle
                        Token lastChild = paramsMiddle.Child.GetLastToken();

                        //link in the newly parsed middleParam
                        lastChild.Combine(space);
                    }
                }
            }
            else
            {
                throw new ArgumentException("CTCP parameter could not be parsed");
            }

            return this;
        }

        internal Token Verify()
        {
            //a CTCP command can not be empty
            if (this._ctcp.TryGetFirstTokenOfType(TokenType.CTCPCommand, out Token cmd)
                && cmd.IsEmpty)
            {
                ThrowCommandNotAssigned();
            }

            return this._ctcp;
        }

        [DoesNotReturn]
        protected static void ThrowCommandNotAssigned()
            => throw new ArgumentException
            (
                "No command has been assigned to the CTCP message"
            );
    }
}
