using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpILMixins.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvalidAttributeCodeFixProvider))]
    [Shared]
    public class InvalidAttributeCodeFixProvider : CodeFixProvider
    {
        public const string Title = "Remove {0} annotation";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(InvalidInlineAttributeAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!FixableDiagnosticIds.Contains(diagnostic.Id)) continue;

                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var enumTypeResult = Enum.TryParse<InvalidAttributeType>(
                    diagnostic.Properties.GetValueOrDefault(nameof(InvalidAttributeType),
                        InvalidAttributeType.AttributeList.ToString()), out var type);

                if (!enumTypeResult) return;


                var requestingInlineResult = bool.TryParse(
                    diagnostic.Properties.GetValueOrDefault(InvalidInlineAttributeAnalyzer.IsRequestingInlineKey,
                        InvalidAttributeType.AttributeList.ToString()), out var isRequestingInline);

                var parent = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf();

                var result = GetCorrectSyntaxNode(type, parent);

                if (result == null)
                    return;

                context.RegisterCodeFix(
                    CodeAction.Create(string.Format(Title, isRequestingInline ? "invalid" : "useless"),
                        token => RemoveUselessAnnotation(context.Document, token, result),
                        nameof(MixinNotInMixinWorkspaceCodeFixProvider) + ""), diagnostic);
            }
        }

        private static SyntaxNode GetCorrectSyntaxNode(InvalidAttributeType type, IEnumerable<SyntaxNode> parent)
        {
            SyntaxNode result;
            switch (type)
            {
                case InvalidAttributeType.AttributeList:
                    result = parent.OfType<AttributeListSyntax>().FirstOrDefault();
                    break;
                case InvalidAttributeType.AttributeSyntax:
                    result = parent.OfType<AttributeSyntax>().FirstOrDefault();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private async Task<Document> RemoveUselessAnnotation(Document document, CancellationToken token,
            SyntaxNode declaration)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(token);

            syntaxRoot = syntaxRoot.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(syntaxRoot);
        }
    }
}