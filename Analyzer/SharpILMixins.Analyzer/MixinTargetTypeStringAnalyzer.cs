using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Annotations;

namespace SharpILMixins.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MixinTargetTypeStringAnalyzer : DiagnosticAnalyzer
    {
        public static readonly string DiagnosticId = Utilities.GetMixinCode(1);

        private const string Category = "Mixin";
        private const string Title = "Targeting Type with a string constant instead of Type constant";
        private const string Message = Title;
        private const string Description =
            "Using String constant for target type instead of Type constant.\n" +
            "Consider using a Type Reference of the Target Type or making an Accessor instead of targeting \"{0}\" with a String.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            Title, Message, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var declaration = context.Node as ClassDeclarationSyntax;
            Debug.Assert(declaration != null, nameof(declaration) + " != null");
            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);

            var mixinAttributeRaw = declaredSymbol.GetCustomAttributeRaw<MixinAttribute>();
            var mixinAttribute = declaredSymbol.GetCustomAttribute<MixinAttribute>();

            if (mixinAttributeRaw == null || mixinAttribute == null) return;

            var firstArgument = mixinAttributeRaw.ConstructorArguments.FirstOrDefault();
            if (firstArgument.Kind != TypedConstantKind.Type)
            {
                Debugger.Launch();
                context.ReportDiagnostic(Diagnostic.Create(Rule,
                    mixinAttributeRaw.ApplicationSyntaxReference.GetSyntax().GetLocation(), mixinAttribute.Target));
            }
        }
    }
}