using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpILMixins.Analyzer.Utils;
using SharpILMixins.Annotations.Inline;
using AttributeListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax;

namespace SharpILMixins.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InvalidInlineAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string IsRequestingInlineKey = "IsRequestingInline";
        public static readonly string DiagnosticId = Utilities.GetMixinCode(2);
        private const string Title = "Usage of [InlineMethod] Attribute is invalid";
        private const string InvalidMessage = "Method \"{0}\" cannot be inlined because it has ref/out parameters.";
        private const string UselessMessage = "Method \"{0}\" cannot is already not going to be inlined because it has ref/out parameters.";

        private const string Description =
            "This method cannot be inlined because it has a ByRef/Out parameter that requires explicit non-inlining";

        private static readonly DiagnosticDescriptor InvalidRule = new DiagnosticDescriptor(DiagnosticId,
            Title, InvalidMessage, Utilities.Category, DiagnosticSeverity.Error, true, Description);

        private static readonly DiagnosticDescriptor UselessRule = new DiagnosticDescriptor(DiagnosticId,
            Title, UselessMessage, Utilities.Category, DiagnosticSeverity.Error, true, Description,
            customTags: WellKnownDiagnosticTags.Unnecessary);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(RunAnalysis, SyntaxKind.MethodDeclaration);
        }

        private void RunAnalysis(SyntaxNodeAnalysisContext context)
        {
            var declaration = (MethodDeclarationSyntax) context.Node;
            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);

            var inlineOptionRaw = declaredSymbol.GetCustomAttributeRaw<MethodInlineOptionAttribute>();
            var inlineOption = declaredSymbol.GetCustomAttribute<MethodInlineOptionAttribute>();

            var isRequestingInline = inlineOption == null || inlineOption.Setting == InlineSetting.DoInline;

            if (inlineOptionRaw == null || !declaration.ParameterList.Parameters.Any(p =>
                p.Modifiers.Any(SyntaxKind.RefKeyword) || p.Modifiers.Any(SyntaxKind.OutKeyword))) return;
            var attribute = inlineOptionRaw.ApplicationSyntaxReference
                .GetSyntax(context.CancellationToken);
            var parent = (AttributeListSyntax) attribute.Parent;

            var isInvalidList = parent.Attributes.Count == 1;
                
            var builder = ImmutableDictionary.CreateBuilder<string, string>();

            builder.Add(nameof(InvalidAttributeType),
                (isInvalidList ? InvalidAttributeType.AttributeList : InvalidAttributeType.AttributeSyntax)
                .ToString());
            builder.Add(IsRequestingInlineKey, isRequestingInline.ToString());

            var diagnostic = Diagnostic.Create(isRequestingInline ? InvalidRule : UselessRule,
                (isInvalidList ? parent : attribute).GetLocation(), builder.ToImmutable(),
                declaration.Identifier.ValueText);

            context.ReportDiagnostic(diagnostic);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UselessRule, InvalidRule);
    }
}