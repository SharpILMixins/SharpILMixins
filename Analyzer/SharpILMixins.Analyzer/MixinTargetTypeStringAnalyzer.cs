using System.Collections.Immutable;
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
        private const string Title = "Targeting Type with a string constant instead of Type constant 2";

        private const string Message =
            "Using String constant to target type \"{0}\" instead of using a Type Reference constant.";

        private const string Description =
            "Using a String constant to target a Type on Mixins is discouraged because the type can change at any point, breaking your code and causing issues.\n" +
            "Consider using a Type Reference of the Target Type or making an Accessor instead of targeting it with a String.";

        public static readonly string DiagnosticId = Utilities.GetMixinCode(1);

        private static readonly DiagnosticDescriptor Rule = Utilities.ProcessRuleForRider(new DiagnosticDescriptor(
            DiagnosticId,
            Title, Message, Utilities.Category, DiagnosticSeverity.Warning, true, Description));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var declaration = (ClassDeclarationSyntax) context.Node;
            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);

            var mixinAttributeRaw = declaredSymbol.GetCustomAttributeRaw<MixinAttribute>(true);

            if (mixinAttributeRaw == null) return;

            var argumentList =
                (mixinAttributeRaw.ApplicationSyntaxReference.GetSyntax(cancellationToken) as AttributeSyntax)
                ?.ArgumentList;

            var firstArgument = mixinAttributeRaw.ConstructorArguments.FirstOrDefault();
            if (argumentList != null && firstArgument.Kind == TypedConstantKind.Primitive)
                context.ReportDiagnostic(Diagnostic.Create(Rule,
                    (argumentList.Arguments.FirstOrDefault() ??
                     mixinAttributeRaw.ApplicationSyntaxReference.GetSyntax()).GetLocation(), firstArgument.Value));
        }
    }
}