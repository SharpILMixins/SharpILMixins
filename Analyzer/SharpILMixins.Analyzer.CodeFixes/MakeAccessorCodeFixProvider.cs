using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Annotations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

        public const string CreateAccessorTitle = "Create Accessor for this Type and replace with Reference";
        public const string UseTypeReferenceTitle = "Convert into Type Reference";

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<TypeDeclarationSyntax>().First();

                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
                var declaredSymbol = semanticModel.GetDeclaredSymbol(declaration);
                var compilation = await context.Document.Project.GetCompilationAsync();
                if (compilation == null) return;

                var attribute = declaredSymbol.GetCustomAttribute<MixinAttribute>();
                if (attribute == null) return;

                var typeInfo = compilation.GetTypeByMetadataName(attribute.Target);
                var document = context.Document;

                var requiresAccessor = typeInfo == null;
                if (requiresAccessor)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(CreateAccessorTitle,
                            c => MakeAccessorAsync(document, declaration, semanticModel, declaredSymbol),
                            nameof(MakeAccessorCodeFixProvider)), diagnostic);
                }
                else
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(UseTypeReferenceTitle,
                            c => ReplaceWithTypeReferenceAsync(document, declaration, semanticModel, typeInfo),
                            nameof(MakeAccessorCodeFixProvider)), diagnostic);
                }
            }
        }

        private async Task<Document> ReplaceWithTypeReferenceAsync(Document document, TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel, INamedTypeSymbol targetType)
        {
            await Task.Yield();

            //Modify the attribute
            var attributeSyntax =
                typeDecl.GetCustomAttributesSyntax<MixinAttribute>(semanticModel).FirstOrDefault();

            if (attributeSyntax == null || !document.TryGetSyntaxRoot(out var root))
                return document;

            var argumentList = attributeSyntax.ArgumentList;
            var firstArgument = argumentList.Arguments.First();
            var newArgument = AttributeArgument(TypeOfExpression(IdentifierName(targetType.ToDisplayString())));
            var newRoot = root.ReplaceNode(attributeSyntax,
                attributeSyntax.WithArgumentList(argumentList.ReplaceNode(
                    firstArgument, newArgument)));

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Solution> MakeAccessorAsync(Document document, TypeDeclarationSyntax typeDecl,
            SemanticModel semanticModel, INamedTypeSymbol declaredSymbol)
        {
            var originalSolution = document.Project.Solution;

            var attributeRaw = declaredSymbol.GetCustomAttributeRaw<MixinAttribute>();
            var attribute = declaredSymbol.GetCustomAttribute<MixinAttribute>();
            if (attributeRaw == null || attribute == null)
                return originalSolution;

            var fullName = attribute.Target;

            //Create accessor type
            var accessorForTarget = AccessorCreator.CreateAccessorForTarget(fullName, document.Project.DefaultNamespace,
                out var _, out var targetFileName, out var fullAccessorName);
            var solution = originalSolution.AddDocument(DocumentId.CreateNewId(document.Project.Id),
                targetFileName + ".cs",
                accessorForTarget);

            //Modify the Mixin workspace
            solution = await Utilities.ModifyMixinWorkspace(
                c => { MixinWorkspaceHelper.AddMixin(ref c, fullAccessorName, true); }, solution,
                document.Project.AdditionalDocuments);

            //Modify the attribute
            var attributeSyntax =
                typeDecl.GetCustomAttributesSyntax<MixinAttribute>(semanticModel).FirstOrDefault();

            if (attributeSyntax == null || !document.TryGetSyntaxRoot(out var root))
                return originalSolution;

            var argumentList = attributeSyntax.ArgumentList;
            var firstArgument = argumentList.Arguments.First();
            var newArgument = AttributeArgument(TypeOfExpression(IdentifierName(fullAccessorName)));
            var newRoot = root.ReplaceNode(attributeSyntax,
                attributeSyntax.WithArgumentList(argumentList.ReplaceNode(
                    firstArgument, newArgument)));

            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            return solution;
        }
    }
}