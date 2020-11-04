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
        public const string Title = "Add Mixin Type to Mixin Workspace ";

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<TypeDeclarationSyntax>().First();

                bool IsVisible(MixinConfiguration c) => c.Targets.Length > 0;

                RegisterCodeFix("(At the beginning)", IsVisible,
                    (c, type) => { c.Mixins = new[] {type}.Concat(c.Mixins).ToArray(); }, context,
                    semanticModel.GetDeclaredSymbol(declaration), diagnostic);

                void ModifyAddToEnd(MixinConfiguration c, string type)
                {
                    c.Mixins = c.Mixins.Concat(new[] {type}).ToArray();
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
            context.RegisterCodeFix(
                CodeAction.Create(Title + suffix, c => DoMixinWorkspaceFix(modifyMixins, declaration, context),
                    nameof(MixinNotInMixinWorkspaceCodeFixProvider) + suffix), diagnostic);
        }

        private static async Task<Solution> DoMixinWorkspaceFix(Action<MixinConfiguration, string> modifyMixins,
            ISymbol declaration,
            CodeFixContext context)
        {
            await Task.Yield();
            var (configurationDocument, existingConfiguration) =
                GetMixinConfiguration(context.Document.Project.AdditionalDocuments);
            if (configurationDocument != null && existingConfiguration != null)
            {
                modifyMixins.Invoke(existingConfiguration, declaration.ToDisplayString());

                return context.Document.Project.Solution
                    .WithAdditionalDocumentText(
                        configurationDocument.Id,
                        SourceText.From(JsonConvert.SerializeObject(existingConfiguration, Formatting.Indented))
                    );
            }

            return context.Document.Project.Solution;
        }

        public static (TextDocument, MixinConfiguration) GetMixinConfiguration(
            IEnumerable<TextDocument> additionalFiles)
        {
            var firstOrDefault =
                additionalFiles.FirstOrDefault(t => Path.GetFileName(t.FilePath).Equals("mixins.json"));
            SourceText sourceText = null;
            firstOrDefault?.TryGetText(out sourceText);
            return sourceText == null
                ? (null, null)
                : (firstOrDefault, JsonConvert.DeserializeObject<MixinConfiguration>(sourceText.ToString()));
        }

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Utilities.GetMixinCode(2));
    }
}