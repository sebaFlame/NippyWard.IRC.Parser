using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NippyWard.IRC.Parser.Generator
{
    public class SyntaxReceiver : ISyntaxReceiver
    {
        internal const string _TokenTypeName = "TokenType";
        internal const string _VisitorBaseClassName = "BaseTokenVisitor";

        internal EnumDeclarationSyntax TokenType { get; private set; }
        internal ClassDeclarationSyntax VisitorBase { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if(syntaxNode is EnumDeclarationSyntax enumDeclaration)
            {
                if(string.Equals
                (
                    _TokenTypeName,
                    enumDeclaration.Identifier.Text
                ))
                {
                    this.TokenType = enumDeclaration;
                }
            }

            if(syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                if(string.Equals
                (
                    _VisitorBaseClassName,
                    classDeclaration.Identifier.Text
                ))
                {
                    this.VisitorBase = classDeclaration;
                }
            }
        }
    }
}
