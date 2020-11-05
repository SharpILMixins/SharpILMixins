using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using SharpILMixins.Processor.Workspace;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpILMixins.Analyzer.Utils;

namespace SharpILMixins.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MixinNotInMixinWorkspaceCodeFixProvider)), Shared]
    public class MixinNotInMixinWorkspaceCodeFixProvider : CodeFixProvider
    {
        public const string Title = "Register Mixin Type to Mixin Workspace ";

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

                bool IsVisible(MixinConfiguration c) => c.Targets.Length > 0;

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

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Utilities.GetMixinCode(2));
    }
}