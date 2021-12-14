using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

using ThePlague.IRC.Parser;
using ThePlague.IRC.Parser.Tokens;
using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    public class IRCFormattingTests
    {
        [Fact]
        public void FormattingMiddleTest()
        {
            string message = ":User PRIVMSG #format "
                + "\u0002Bold"
                + "-"
                + "\u001DItalics"
                + "-"
                + "\u0011Monospace"
                + "-"
                + "\u001FUnderline"
                + "-"
                + "\u001EStrikethrough"
                + "\u000F"
                + "\r\n";

            using(Token token = IRCMessageTests.AssertParsed(message))
            {
                //Verify prefix
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedMiddle = null;

                IRCMessageTests.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#format"
                        ),
                        (Token t) => formattedMiddle = t
                    }
                );

                List<Token> formats = formattedMiddle
                    .GetAllTokensOfType(TokenType.Format)
                    .ToList();

                Assert.Equal(6, formats.Count);

                int index = 0;
                Assert.Equal(TokenType.Bold, formats[index++].Child.TokenType);
                Assert.Equal
                (
                    TokenType.Italics,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.Monospace,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.Underline,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.Strikethrough,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.Reset,
                    formats[index++].Child.TokenType
                );
            }
        }

        [Fact]
        public void ColourTrailingTest()
        {
            string message = ":User PRIVMSG #format :"
                + "\u0003"
                + "\u00031"
                + "\u000399"
                + "\u00031,1"
                + "\u000399,99"
                + "\u0003,"
                + "\r\n";

            using(Token token = IRCMessageTests.AssertParsed(message))
            {
                //Verify prefix
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedTrailing = null;

                IRCMessageTests.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#format"
                        ),
                        (Token t) => formattedTrailing = t
                    }
                );

                List<Token> formats = formattedTrailing
                    .GetAllTokensOfType(TokenType.Format)
                    .ToList();

                Assert.Equal(6, formats.Count);

                int index = 0;
                //empty color
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.ColorCombination
                );

                //single digit fg colour
                index++;
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "1"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.ColorCombinationSuffix
                );

                //double digit fg colour
                index++;
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "99"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.ColorCombinationSuffix
                );

                //single digit fg/bg colour
                index++;
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "1"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.BackgroundColor,
                    "1"
                );

                //double digit fg/bg colour
                index++;
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "99"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.BackgroundColor,
                    "99"
                );

                //empty color with trailing comma
                index++;
                Assert.Equal
                (
                    TokenType.ColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.ColorCombination
                );
            }
        }

        [Fact]
        public void HexColourTrailingTest()
        {
            string message = ":User PRIVMSG #format :"
                + "\u0004"
                + "\u0004000000"
                + "\u0004FFFFFF"
                + "\u000409FA60"
                + "\u000409FA60,8212C6"
                + "\u0004,"
                + "\r\n";

            using(Token token = IRCMessageTests.AssertParsed(message))
            {
                //Verify prefix
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedTrailing = null;

                IRCMessageTests.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                        (
                            t,
                            TokenType.Middle,
                            "#format"
                        ),
                        (Token t) => formattedTrailing = t
                    }
                );

                List<Token> formats = formattedTrailing
                    .GetAllTokensOfType(TokenType.Format)
                    .ToList();

                Assert.Equal(6, formats.Count);

                int index = 0;
                //empty color
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombination
                );

                //digit fg colour
                index++;
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "000000"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombinationSuffix
                );

                //letter fg colour
                index++;
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "FFFFFF"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombinationSuffix
                );

                //hex fg colour
                index++;
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "09FA60"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombinationSuffix
                );

                //hex fg/bg colour
                index++;
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "09FA60"
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.BackgroundHexColor,
                    "8212C6"
                );

                //empty color with trailing comma
                index++;
                Assert.Equal
                (
                    TokenType.HexColorFormat,
                    formats[index].Child.TokenType
                );
                IRCMessageTests.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombination
                );
            }
        }
    }
}
