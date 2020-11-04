using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Annotations;

namespace SharpILMixins.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MixinNotInMixinWorkspaceAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Mixin Type not registered on the Mixin Workspace";
        private const string Message = "Mixin Type \"{0}\" needs to be registered on the Mixin Workspace.";

        private const string Description =
            "In order for this Type to be processed properly, it needs to be registered on the Mixin Workspace (mixins.json)";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(Utilities.GetMixinCode(2),
            Title, Message, Utilities.Category, DiagnosticSeverity.Error, true, Description);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(context =>
            {
                analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
                analysisContext.EnableConcurrentExecution();

                context.RegisterSyntaxNodeAction(c => AnalyzeType(c, context.Options.AdditionalFiles), SyntaxKind.ClassDeclaration);

            });

        }

        private void AnalyzeType(SyntaxNodeAnalysisContext context,
            ImmutableArray<AdditionalText> additionalFiles)
        {
            var declaration = (ClassDeclarationSyntax)context.Node;
            var model = context.SemanticModel;

            var declaredSymbol = model.GetDeclaredSymbol(declaration);
            var isMixinType = declaredSymbol.GetCustomAttributeRaw<MixinAttribute>() != null ||
                              declaredSymbol.GetCustomAttributeRaw<AccessorAttribute>() != null;
            if (!isMixinType) return;
            //Debugger.Launch();

            var configuration = Utilities.GetMixinConfiguration(additionalFiles, context.CancellationToken);
            if (configuration?.IsMixinIncludedInWorkspace(declaredSymbol.ToDisplayString()) != true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.Identifier.GetLocation(), declaration.Identifier.Value));
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    }
}