using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using ThePlague.IRC.Parser.Tokens;

namespace ThePlague.IRC.Parser.Tests
{
    public class FormattingTests
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

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedMiddle = null;

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                Assert.Equal(TokenType.BoldFormat, formats[index++].Child.TokenType);
                Assert.Equal
                (
                    TokenType.ItalicsFormat,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.MonospaceFormat,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.UnderlineFormat,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.StrikethroughFormat,
                    formats[index++].Child.TokenType
                );
                Assert.Equal
                (
                    TokenType.ResetFormat,
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

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedTrailing = null;

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "1"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "99"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "1"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundColor,
                    "99"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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

            using(Token token = AssertHelpers.AssertParsed(message))
            {
                //Verify prefix
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.SourcePrefixTarget,
                    "User"
                );

                //check command name
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    token,
                    TokenType.CommandName,
                    "PRIVMSG"
                );

                Token formattedTrailing = null;

                AssertHelpers.AssertInNthChildOfTokenTypeInTokenType
                (
                    token,
                    TokenType.Params,
                    TokenType.ParamsPrefix,
                    new Action<Token>[]
                    {
                        (Token t)
                        => AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "000000"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "FFFFFF"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "09FA60"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
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
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
                (
                    formats[index].Child,
                    TokenType.ForegroundHexColor,
                    "09FA60"
                );
                AssertHelpers.AssertFirstOfTokenTypeIsEqualTo
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
                AssertHelpers.AssertFirstOfTokenTypeIsEmpty
                (
                    formats[index].Child,
                    TokenType.HexColorCombination
                );
            }
        }
    }
}
