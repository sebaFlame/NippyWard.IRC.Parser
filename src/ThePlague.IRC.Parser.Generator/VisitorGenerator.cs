using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThePlague.IRC.Parser.Generator
{
    [Generator]
    public class VisitorGenerator : ISourceGenerator
    {
        private const string _VisitorByTypeClassName = "BaseTokenVisitorByType";
        private const string _VisitTokenByTypeName = "VisitTokenByType";
        private const string _VisitTokenDefaultName = "VisitTokenDefault";
        private const string _VisitTokenName = "VisitToken";
        private const string _ParameterName = "token";
        private const string _ParameterType = "Token";
        private const string _ParameterTypeMember = "TokenType";

        private SyntaxReceiver _syntaxReceiver;

        public void Initialize(GeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif 
            context.RegisterForSyntaxNotifications(this.GetSyntaxReceiver);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            SourceText sourceText = GenerateVisitorByTypeClass
            (
                this._syntaxReceiver.TokenType,
                this._syntaxReceiver.VisitorBase,
                context.ParseOptions
            );

            context.AddSource($"{_VisitorByTypeClassName}.g.cs", sourceText);
        }

        private ISyntaxReceiver GetSyntaxReceiver()
        {
            this._syntaxReceiver = new SyntaxReceiver();
            return this._syntaxReceiver;
        }

        private static SourceText GenerateVisitorByTypeClass
        (
            EnumDeclarationSyntax tokenType,
            ClassDeclarationSyntax visitorBase,
            ParseOptions parseOptions
        )
        {
            ClassDeclarationSyntax classDeclaration
                = SyntaxFactory.ClassDeclaration
                (
                    SyntaxFactory.Identifier(_VisitorByTypeClassName)
                )
                .AddModifiers
                (
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                )
                .AddBaseListTypes
                (
                    SyntaxFactory.SimpleBaseType
                    (
                        SyntaxFactory.ParseTypeName(visitorBase.Identifier.Text)
                    )
                )
                .NormalizeWhitespace()
                .AddMembers
                (
                    SyntaxFactory.ConstructorDeclaration
                    (
                        SyntaxFactory.Identifier(_VisitorByTypeClassName)
                    )
                        .AddModifiers
                        (
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                        )
                        .WithInitializer
                        (
                            SyntaxFactory.ConstructorInitializer
                            (
                                SyntaxKind.BaseConstructorInitializer
                            )
                            .NormalizeWhitespace()
                        )
                        .WithBody
                        (
                            SyntaxFactory.Block()
                        ),
                    CreateVisitTokenMethod(),
                    CreateSwitchTokenTypeMethod(tokenType)
                )
                .NormalizeWhitespace()
                .AddMembers
                (
                    tokenType.Members
                        .Select
                        (
                            x => CreateTokenTypeMethod(x)
                        )
                        .ToArray()
                )
                .NormalizeWhitespace();

            //fetch the namespace name from the base
            NameSyntax ns = (visitorBase.Parent as NamespaceDeclarationSyntax)
                .Name;

            //then create the compilation unit
            SyntaxTree syntaxTree = CSharpSyntaxTree.Create
            (
                SyntaxFactory.CompilationUnit()
                    .AddUsings
                    (
                        SyntaxFactory.UsingDirective
                        (
                            SyntaxFactory.ParseName("System")
                        )
                    )
                    .NormalizeWhitespace()
                    .AddMembers
                    (
                        SyntaxFactory.NamespaceDeclaration(ns)
                            .AddMembers(classDeclaration)
                    )
                    .NormalizeWhitespace(),
                    parseOptions as CSharpParseOptions,
                    "",
                    Encoding.Unicode
            );

            return syntaxTree.GetText();
        }

        private static MethodDeclarationSyntax CreateVisitTokenMethod()
        {
            return SyntaxFactory.MethodDeclaration
            (
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList
                (
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
                ),
                SyntaxFactory.ParseTypeName("void"),
                null,
                SyntaxFactory.Identifier(_VisitTokenName),
                null,
                SyntaxFactory.ParameterList
                (
                    SyntaxFactory.SeparatedList
                    (
                        new ParameterSyntax[]
                        {
                            SyntaxFactory.Parameter
                            (
                                SyntaxFactory.Identifier(_ParameterName)
                            )
                                .WithType
                                (
                                    SyntaxFactory.ParseTypeName
                                    (
                                        _ParameterType
                                    )
                                )
                        }
                    )
                ),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block
                (
                    SyntaxFactory.IfStatement
                    (
                        SyntaxFactory.IsPatternExpression
                        (
                            SyntaxFactory.IdentifierName(_ParameterName),
                            SyntaxFactory.ConstantPattern
                            (
                                SyntaxFactory.LiteralExpression
                                (
                                    SyntaxKind.NullLiteralExpression
                                )
                            )
                        ),
                        SyntaxFactory.ReturnStatement()
                    ),
                    SyntaxFactory.ExpressionStatement
                    (
                        SyntaxFactory.InvocationExpression
                        (
                            SyntaxFactory.IdentifierName
                            (
                                _VisitTokenByTypeName
                            ),
                            SyntaxFactory.ArgumentList
                            (
                                SyntaxFactory.SeparatedList
                                (
                                    new ArgumentSyntax[]
                                    {
                                        SyntaxFactory.Argument
                                        (
                                            SyntaxFactory.IdentifierName
                                            (
                                                _ParameterName
                                            )
                                        )
                                    }
                                )
                            )
                        )
                    )
                )
                    .NormalizeWhitespace(),
                null
            );
        }

        private static MethodDeclarationSyntax CreateSwitchTokenTypeMethod
        (
            EnumDeclarationSyntax enumDeclaration
        )
        {
            return SyntaxFactory.MethodDeclaration
            (
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList
                (
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                ),
                SyntaxFactory.ParseTypeName("void"),
                null,
                SyntaxFactory.Identifier(_VisitTokenByTypeName),
                null,
                SyntaxFactory.ParameterList
                (
                    SyntaxFactory.SeparatedList
                    (
                        new ParameterSyntax[]
                        {
                            SyntaxFactory.Parameter
                            (
                                SyntaxFactory.Identifier(_ParameterName)
                            )
                                .WithType
                                (
                                    SyntaxFactory.ParseTypeName
                                    (
                                        _ParameterType
                                    )
                                )
                        }
                    )
                ),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block
                (
                    SyntaxFactory.SwitchStatement
                    (
                        SyntaxFactory.MemberAccessExpression
                        (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName
                                (
                                    _ParameterName
                                ),
                                SyntaxFactory.IdentifierName
                                (
                                    _ParameterTypeMember
                                )
                        )
                    )
                        .WithSections
                        (
                            CreateSwitchSections
                            (
                                enumDeclaration
                            )
                        )
                ),
                null
            );
        }

        private static SyntaxList<SwitchSectionSyntax> CreateSwitchSections
        (
            EnumDeclarationSyntax enumDeclaration
        )
        {
            IEnumerable<EnumMemberDeclarationSyntax> members
                = enumDeclaration.Members;

            IEnumerable<SwitchSectionSyntax> sections = members
                .Select(x => CreateSwitchSection(x))
                .Concat
                (
                    new SwitchSectionSyntax[]
                    {
                        CreateDefaultSwitchSection()
                    }
                );

            return SyntaxFactory.List(sections);
        }

        private static SwitchSectionSyntax CreateSwitchSection
        (
            EnumMemberDeclarationSyntax enumMember
        )
        {
            string name = $"Visit{enumMember.Identifier.ValueText}";

            return SyntaxFactory.SwitchSection
            (
                SyntaxFactory.List
                (
                    new SwitchLabelSyntax[]
                    {
                        SyntaxFactory.CaseSwitchLabel
                        (
                            SyntaxFactory.MemberAccessExpression
                            (
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName
                                (
                                    SyntaxReceiver._TokenTypeName
                                ),
                                SyntaxFactory.IdentifierName
                                (
                                    enumMember.Identifier.ValueText
                                )
                            )
                        )
                    }
                ),
                SyntaxFactory.List
                (
                    new StatementSyntax[]
                    {
                        SyntaxFactory.ExpressionStatement
                        (
                            SyntaxFactory.InvocationExpression
                            (
                                SyntaxFactory.IdentifierName
                                (
                                    name
                                ),
                                SyntaxFactory.ArgumentList
                                (
                                    SyntaxFactory.SeparatedList
                                    (
                                        new ArgumentSyntax[]
                                        {
                                            SyntaxFactory.Argument
                                            (
                                                SyntaxFactory.IdentifierName
                                                (
                                                    _ParameterName
                                                )
                                            )
                                        }
                                    )
                                )
                            )
                        ),
                        SyntaxFactory.BreakStatement()
                    }
                )
            );
        }

        private static SwitchSectionSyntax CreateDefaultSwitchSection()
        {
            return SyntaxFactory.SwitchSection
            (
                SyntaxFactory.List
                (
                    new SwitchLabelSyntax[]
                    {
                        SyntaxFactory.DefaultSwitchLabel()
                    }
                ),
                SyntaxFactory.List
                (
                    new StatementSyntax[]
                    {
                        SyntaxFactory.ExpressionStatement
                        (
                            SyntaxFactory.InvocationExpression
                            (
                                SyntaxFactory.IdentifierName
                                (
                                    _VisitTokenDefaultName
                                ),
                                SyntaxFactory.ArgumentList
                                (
                                    SyntaxFactory.SeparatedList
                                    (
                                        new ArgumentSyntax[]
                                        {
                                            SyntaxFactory.Argument
                                            (
                                                SyntaxFactory.IdentifierName
                                                (
                                                    _ParameterName
                                                )
                                            )
                                        }
                                    )
                                )
                            )
                        ),
                        SyntaxFactory.BreakStatement()
                    }
                )
            );
        }

        private static MethodDeclarationSyntax CreateTokenTypeMethod
        (
            EnumMemberDeclarationSyntax enumMember
        )
        {
            string name = $"Visit{enumMember.Identifier.ValueText}";

            return SyntaxFactory.MethodDeclaration
            (
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList
                (
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.VirtualKeyword)
                ),
                SyntaxFactory.ParseTypeName("void"),
                null,
                SyntaxFactory.Identifier(name),
                null,
                SyntaxFactory.ParameterList
                (
                    SyntaxFactory.SeparatedList
                    (
                        new ParameterSyntax[]
                        {
                            SyntaxFactory.Parameter
                            (
                                SyntaxFactory.Identifier(_ParameterName)
                            )
                                .WithType
                                (
                                    SyntaxFactory.ParseTypeName
                                    (
                                        _ParameterType
                                    )
                                )
                        }
                    )
                ),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                null,
                SyntaxFactory.ArrowExpressionClause
                (
                    SyntaxFactory.InvocationExpression
                    (
                        SyntaxFactory.IdentifierName(_VisitTokenDefaultName),
                        SyntaxFactory.ArgumentList
                        (
                            SyntaxFactory.SeparatedList
                            (
                                new ArgumentSyntax[]
                                {
                                    SyntaxFactory.Argument
                                    (
                                        SyntaxFactory.IdentifierName
                                        (
                                            _ParameterName
                                        )
                                    )
                                }
                            )
                        )
                    )
                ),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
            );
        }
    }
}
