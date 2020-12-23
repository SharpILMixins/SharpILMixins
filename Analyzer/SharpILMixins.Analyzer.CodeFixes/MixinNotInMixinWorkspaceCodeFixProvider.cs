using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Processor.Workspace;

namespace SharpILMixins.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MixinNotInMixinWorkspaceCodeFixProvider))]
    [Shared]
    public class MixinNotInMixinWorkspaceCodeFixProvider : CodeFixProvider
    {
        public const string Title = "Register Mixin Type to Mixin Workspace ";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MixinNotInMixinWorkspaceAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!FixableDiagnosticIds.Contains(diagnostic.Id)) continue;

                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<TypeDeclarationSyntax>().First();

                bool IsVisible(MixinConfiguration c)
                {
                    return c.Mixins.Length > 0;
                }

                RegisterCodeFix("(At the beginning)", IsVisible,
                    (c, type) => { MixinWorkspaceHelper.AddMixin(ref c, type, true); }, context,
                    semanticModel.GetDeclaredSymbol(declaration), diagnostic);

                void ModifyAddToEnd(MixinConfiguration c, string type)
                {
                    MixinWorkspaceHelper.AddMixin(ref c, type, false);
                }

                RegisterCodeFix("(At the end)", IsVisible,
                    ModifyAddToEnd, context,
                    semanticModel.GetDeclaredSymbol(declaration),
                    diagnostic);

                RegisterCodeFix("", c => !IsVisible(c),
                    ModifyAddToEnd, context,
                    semanticModel.GetDeclaredSymbol(declaration),
                    diagnostic);
            }
        }

        private static void RegisterCodeFix(string suffix, Func<MixinConfiguration, bool> isVisibleFunc,
            Action<MixinConfiguration, string> modifyMixins,
            CodeFixContext context,
            ISymbol declaration, Diagnostic diagnostic)
        {
            var (configurationDocument, existingConfiguration) =
                Utilities.GetMixinConfiguration(context.Document.Project.AdditionalDocuments);

            if (configurationDocument == null || existingConfiguration == null) return;

            if (isVisibleFunc(existingConfiguration))
                context.RegisterCodeFix(
                    CodeAction.Create(Title + suffix,
                        c => DoMixinWorkspaceFix(modifyMixins, declaration, context),
                        nameof(MixinNotInMixinWorkspaceCodeFixProvider) + suffix), diagnostic);
        }

        private static async Task<Solution> DoMixinWorkspaceFix(Action<MixinConfiguration, string> modifyMixins,
            ISymbol declaration, CodeFixContext context)
        {
            return await Utilities.ModifyMixinWorkspace(c => { modifyMixins.Invoke(c, declaration.ToDisplayString()); },
                context.Document.Project.Solution,
                context.Document.Project.AdditionalDocuments);
        }
    }
}