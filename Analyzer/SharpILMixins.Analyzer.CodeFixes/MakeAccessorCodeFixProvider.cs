using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Annotations;

namespace SharpILMixins.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeAccessorCodeFixProvider)), Shared]
    public class MakeAccessorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MixinTargetTypeStringAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public const string Title = "Hey";

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<TypeDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(Title, c => MakeUppercaseAsync(context.Document, declaration, c),
                        nameof(MakeAccessorCodeFixProvider)), diagnostic);
            }
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl,
            CancellationToken cancellationToken)
        {
            var originalSolution = document.Project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var declaredSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
            var attribute = declaredSymbol.GetCustomAttribute<MixinAttribute>();
            if (attribute == null)
                return originalSolution;

            var fullName = attribute.Target;
            fullName = fullName.Substring(fullName.LastIndexOfAny(new[] {'.', '/'}));
            
            //originalSolution.WithAdditionalDocumentText(DocumentId.CreateNewId(document.Project), SourceText.From(), )
            return originalSolution;
        }
    }
}