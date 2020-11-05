using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpILMixins.Annotations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpILMixins.Analyzer
{
    public class AccessorCreator
    {
        public static SourceText CreateAccessorForTarget(string targetType, string targetNamespace, out string targetTypeName, out string targetFileName, out string fullName)
        {
            targetTypeName = targetType;
            if (targetTypeName.LastIndexOfAny(new[] {'.', '/'}) + 1 >= 0 &&
                targetTypeName.Length > targetTypeName.LastIndexOfAny(new[] {'.', '/'}) + 1)
                targetTypeName = targetTypeName.Substring(targetTypeName.LastIndexOfAny(new[] {'.', '/'}) + 1);

            targetFileName = $"{targetTypeName}Accessor";
            fullName = targetFileName;
            if (!string.IsNullOrEmpty(targetNamespace))
                fullName = $"{targetNamespace}.{fullName}";
            return SourceText.From(CompilationUnit()
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                                IdentifierName(targetNamespace)
                            )
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ClassDeclaration(targetFileName)
                                        .WithAttributeLists(
                                            SingletonList(
                                                AttributeList(
                                                    SingletonSeparatedList(
                                                        Attribute(
                                                                IdentifierName(typeof(AccessorAttribute).FullName)
                                                            )
                                                            .WithArgumentList(
                                                                AttributeArgumentList(
                                                                    SingletonSeparatedList(
                                                                        AttributeArgument(
                                                                            LiteralExpression(
                                                                                SyntaxKind.StringLiteralExpression,
                                                                                Literal(targetType)
                                                                            )
                                                                        )
                                                                    )
                                                                )
                                                            )
                                                    )
                                                )
                                            )
                                        )
                                        .WithModifiers(
                                            TokenList(
                                                Token(SyntaxKind.PublicKeyword)
                                            )
                                        )
                                )
                            )
                    )
                )
                .NormalizeWhitespace().ToFullString());
        }
    }
}