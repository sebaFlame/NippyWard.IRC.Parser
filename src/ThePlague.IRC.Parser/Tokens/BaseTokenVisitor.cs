using System;
using System.Collections.Generic;

namespace ThePlague.IRC.Parser.Tokens
{
    public abstract class BaseTokenVisitor : IDisposable
    {
        protected Token Current => this._current;
        protected Token Previous => this._previous;
        protected Token Parent
        {
            get
            {
                if(this._ancestors.TryPeek(out Token t))
                {
                    return t;
                }

                return null;
            }
        }

        private Token _current;
        private readonly Stack<Token> _ancestors;
        private Token _previous;

        public BaseTokenVisitor()
        {
            this._ancestors = new Stack<Token>();
        }

        ~BaseTokenVisitor()
        {
            this.Dispose(false);
        }

        public void VisitToken(Token token)
        {
            if(token is null)
            {
                return;
            }

            this._current = token;

            //visit token
            this.VisitTokenByType(token);
        }

        protected virtual void VisitTokenDefault(Token token)
        {
            //visit children
            this.VisitChild(token);

            //visit next token
            this.VisitNext(token);
        }

        //visit child of token
        protected bool VisitChild(Token token)
        {
            if(token.Child is null)
            {
                return false;
            }

            Token previousToken = this._previous;

            this._previous = null;

            //then visit its children
            this._ancestors.Push(token);
            this.VisitToken(token.Child);
            this._ancestors.Pop();

            this._previous = previousToken;

            return true;
        }

        //visit next tokens in linked list
        protected bool VisitNext(Token token)
        {
            if(token.Next is null)
            {
                return false;
            }

            Token previousToken = this._previous;

            this._previous = token;

            this.VisitToken(token.Next);

            this._previous = previousToken;

            return true;
        }

        private void VisitTokenByType(Token token)
        {
            switch(token.TokenType)
            {
                case TokenType.Message:
                    this.VisitMessageToken(token);
                    break;
                case TokenType.TagPrefix:
                    this.VisitTagPrefix(token);
                    break;
                case TokenType.Tags:
                    this.VisitTags(token);
                    break;
                case TokenType.Tag:
                    this.VisitTag(token);
                    break;
                case TokenType.TagsSuffix:
                    this.VisitTagsSuffix(token);
                    break;
                case TokenType.TagsList:
                    this.VisitTagsList(token);
                    break;
                case TokenType.TagKey:
                    this.VisitTagKey(token);
                    break;
                case TokenType.TagKeySuffix:
                    this.VisitTagKeySuffix(token);
                    break;
                case TokenType.TagSuffix:
                    this.VisitTagSuffix(token);
                    break;
                case TokenType.TagValue:
                    this.VisitTagValue(token);
                    break;
                case TokenType.TagValueList:
                    this.VisitTagValueList(token);
                    break;
                case TokenType.TagValueEscapeList:
                    this.VisitTagValueEscapeList(token);
                    break;
                case TokenType.TagValueEscape:
                    this.VisitTagValueEscape(token);
                    break;
                case TokenType.TagValueEscapeSuffix:
                    this.VisitTagValueEscapeSuffix(token);
                    break;
                case TokenType.TagValueEscapeBackslash:
                    this.VisitTagValueEscapeBackslash(token);
                    break;
                case TokenType.TagValueEscapeSemicolon:
                    this.VisitTagValueEscapeSemicolon(token);
                    break;
                case TokenType.TagValueEscapeSpace:
                    this.VisitTagValueEscapeSpace(token);
                    break;
                case TokenType.TagValueEscapeCr:
                    this.VisitTagValueEscapeCr(token);
                    break;
                case TokenType.TagValueEscapeLf:
                    this.VisitTagValueEscapeLf(token);
                    break;
                case TokenType.TagValueEscapeInvalid:
                    this.VisitTagValueEscapeInvalid(token);
                    break;
                case TokenType.SourcePrefix:
                    this.VisitSourcePrefix(token);
                    break;
                case TokenType.SourcePrefixTarget:
                    this.VisitSourcePrefixTarget(token);
                    break;
                case TokenType.SourcePrefixTargetPrefix:
                    this.VisitSourcePrefixTargetPrefix(token);
                    break;
                case TokenType.SourcePrefixTargetSuffix:
                    this.VisitSourcePrefixTargetSuffix(token);
                    break;
                case TokenType.SourcePrefixList:
                    this.VisitSourcePrefixList(token);
                    break;
                case TokenType.Command:
                    this.VisitCommand(token);
                    break;
                case TokenType.ErroneousMessage:
                    this.VisitErroneousMessage(token);
                    break;
                case TokenType.CommandName:
                    this.VisitCommandName(token);
                    break;
                case TokenType.CommandCode:
                    this.VisitCommandCode(token);
                    break;
                case TokenType.Params:
                    this.VisitParams(token);
                    break;
                case TokenType.ParamsPrefix:
                    this.VisitParamsPrefix(token);
                    break;
                case TokenType.ParamsSuffix:
                    this.VisitParamsSuffix(token);
                    break;
                case TokenType.Middle:
                    this.VisitMiddle(token);
                    break;
                case TokenType.MiddlePrefixList:
                    this.VisitMiddlePrefixList(token);
                    break;
                case TokenType.MiddleSuffix:
                    this.VisitMiddleSuffix(token);
                    break;
                case TokenType.MiddlePrefixListFormatBase:
                    this.VisitMiddlePrefixBaseList(token);
                    break;
                case TokenType.MiddlePrefixWithColonList:
                    this.VisitMiddlePrefixWithColonList(token);
                    break;
                case TokenType.Trailing:
                    this.VisitTrailing(token);
                    break;
                case TokenType.TrailingPrefix:
                    this.VisitTrailingPrefix(token);
                    break;
                case TokenType.TrailingList:
                    this.VisitTrailingList(token);
                    break;
                case TokenType.ShortName:
                    this.VisitShortName(token);
                    break;
                case TokenType.ShortNamePrefix:
                    this.VisitShortNamePrefix(token);
                    break;
                case TokenType.ShortNameSuffix:
                    this.VisitShortNameSuffix(token);
                    break;
                case TokenType.ShortNameList:
                    this.VisitShortNameList(token);
                    break;
                case TokenType.Format:
                    this.VisitFormat(token);
                    break;
                case TokenType.BoldFormat:
                    this.VisitBold(token);
                    break;
                case TokenType.ItalicsFormat:
                    this.VisitItalics(token);
                    break;
                case TokenType.UnderlineFormat:
                    this.VisitUnderLine(token);
                    break;
                case TokenType.StrikethroughFormat:
                    this.VisitStrikethrough(token);
                    break;
                case TokenType.MonospaceFormat:
                    this.VisitMonospace(token);
                    break;
                case TokenType.ResetFormat:
                    this.VisitReset(token);
                    break;
                case TokenType.ColorFormat:
                    this.VisitColorFormat(token);
                    break;
                case TokenType.ColorFormatSuffix:
                    this.VisitColorFormatSuffix(token);
                    break;
                case TokenType.ColorCombination:
                    this.VisitColorCombination(token);
                    break;
                case TokenType.ColorCombinationSuffix:
                    this.VisitColorCombinationSuffix(token);
                    break;
                case TokenType.ForegroundColor:
                    this.VisitForegroundColor(token);
                    break;
                case TokenType.BackgroundColor:
                    this.VisitBackgroundColor(token);
                    break;
                case TokenType.ColorNumber:
                    this.VisitColor(token);
                    break;
                case TokenType.ColorSuffix:
                    this.VisitColorSuffix(token);
                    break;
                case TokenType.HexDecimal:
                    this.VisitHexDecimal(token);
                    break;
                case TokenType.HexColorTriplet:
                    this.VisitHexColor(token);
                    break;
                case TokenType.HexColorFormat:
                    this.VisitHexColorFormat(token);
                    break;
                case TokenType.HexColorCombination:
                    this.VisitHexColorCombination(token);
                    break;
                case TokenType.HexColorCombinationSuffix:
                    this.VisitHexColorCombinationSuffix(token);
                    break;
                case TokenType.ForegroundHexColor:
                    this.VisitForegroundHexColor(token);
                    break;
                case TokenType.BackgroundHexColor:
                    this.VisitBackgroundHexColor(token);
                    break;
                case TokenType.UTF8WithoutNullCrLfSemiColonSpace:
                    this.VisitUTF8WithoutNullCrLfSemiColonSpace(token);
                    break;
                case TokenType.MiddlePrefixListTerminals:
                    this.VisitMiddlePrefixListTerminals(token);
                    break;
                case TokenType.MiddlePrefixListFormatBaseTerminals:
                    this.VisitMiddlePrefixListFormatBaseTerminals(token);
                    break;
                case TokenType.CrLf:
                    this.VisitCrLf(token);
                    break;
                case TokenType.ConstructedMessage:
                    this.VisitConstructedMessage(token);
                    break;
                default:
                    this.VisitTokenDefault(token);
                    break;
            }
        }

        protected virtual void VisitMiddlePrefixListFormatBaseTerminals
        (
            Token token
        )
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddlePrefixListTerminals(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitUTF8WithoutNullCrLfSemiColonSpace
        (
            Token token
        )
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeInvalid(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeLf(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeCr(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeSpace(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeSemicolon(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeBackslash(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscape(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueEscapeList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitConstructedMessage(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitCrLf(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitBackgroundHexColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitForegroundHexColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitHexColorCombinationSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitHexColorCombination(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitHexColorFormat(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitHexColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitHexDecimal(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColorSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitBackgroundColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitForegroundColor(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColorCombinationSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColorCombination(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColorFormatSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitColorFormat(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitReset(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMonospace(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitStrikethrough(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitUnderLine(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitItalics(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitBold(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitFormat(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitShortNameList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitShortNamePrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitShortNameSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitShortName(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTrailingList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTrailingPrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTrailing(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddlePrefixWithColonList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddlePrefixBaseList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddleSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddlePrefixList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMiddle(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitParamsSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitParamsPrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitParams(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitCommandCode(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitCommandName(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitErroneousMessage(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitCommand(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitSourcePrefixList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitSourcePrefixTargetPrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitSourcePrefixTargetSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitSourcePrefixTarget(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitSourcePrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValueList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagValue(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagKeySuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagKey(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagsList(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagsSuffix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTag(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitMessageToken(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTagPrefix(Token token)
            => this.VisitTokenDefault(token);

        protected virtual void VisitTags(Token token)
            => this.VisitTokenDefault(token);

        public virtual void Reset()
        {
            this._ancestors.Clear();
            //this._preceding.Clear();

            this._previous = null;
            this._current = null;
            //this._parent = null;
        }

        public void Dispose()
            => this.Dispose(true);

        public virtual void Dispose(bool isDisposing)
        {
            this._current = null;
            this.Reset();

            if(!isDisposing)
            {
                return;
            }

            GC.SuppressFinalize(this);
        }
    }
}
