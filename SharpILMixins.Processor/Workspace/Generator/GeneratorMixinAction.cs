using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpILMixins.Processor.Workspace.Generator
{
    public class GeneratorMixinAction
    {
        private readonly SyntaxTokenList _publicStaticModifiers =
            TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

        public GeneratorMixinAction(MethodDef targetMethod, TypeDef targetType)
        {
            TargetMethod = targetMethod;
            TargetType = targetType;
            SimpleTargetMethodName = ComputeSimpleMethodName(TargetMethod, TargetType);
        }

        public MethodDef TargetMethod { get; }
        public TypeDef TargetType { get; }

        public string SimpleTargetMethodName { get; set; }

        private static string ComputeSimpleMethodName(IMethodDefOrRef? targetMethod, TypeDef? targetType,
            bool includeDeclaringTypeName = false)
        {
            Debug.Assert(targetMethod != null, nameof(targetMethod) + " != null");
            var resolveTypeDef = targetType ??
                                 throw new MixinApplyException(
                                     $"Unable to resolve type definition for {targetMethod.DeclaringType}");
            var count = resolveTypeDef.Methods
                .Count(m => m.Name.ToString().Equals(targetMethod.Name));
            if (includeDeclaringTypeName || count > 1)
            {
                var strings = new List<string> {targetMethod.Name.ToString()};
                if (includeDeclaringTypeName) strings.Insert(0, targetMethod.DeclaringType.ReflectionName);

                return string.Join("_",
                    strings.Concat(targetMethod.GetParams()
                        .Select(c => c.ReflectionName)));
            }

            return targetMethod.Name;
        }

        public IEnumerable<MemberDeclarationSyntax> ToSyntax()
        {
            var memberDeclarationSyntax = GetStringLiteralField(SimpleTargetMethodName,
                TargetMethod.FullName);
            if (memberDeclarationSyntax != null) yield return memberDeclarationSyntax;

            var injectMembers = new List<MemberDeclarationSyntax>();

            AddInvokeMembers(injectMembers);
            AddFieldMembers(injectMembers);
            AddNewObjMembers(injectMembers);

            if (injectMembers.Count != 0)
                yield return ClassDeclaration($"{SimpleTargetMethodName}Injects")
                    .AddMembers(injectMembers.ToArray())
                    .WithModifiers(_publicStaticModifiers);
        }

        private void AddNewObjMembers(List<MemberDeclarationSyntax> injectMembers)
        {
            injectMembers.AddRange(
                TargetMethod.Body.Instructions
                    .Where(NewObjInjectionProcessor.IsNewObjInstruction)
                    .Select(i => i.Operand)
                    .OfType<IMethodDefOrRef>()
                    .DistinctBy(i => ComputeSimpleMethodName(i, TargetType, true))
                    .Select(i => GetStringLiteralField(ComputeSimpleMethodName(i, TargetType, true), i.FullName))
                    .Where(i => i is not null)
                    .Select(i => i!).ToList()
            );
        }

        private void AddFieldMembers(List<MemberDeclarationSyntax> injectMembers)
        {
            injectMembers.AddRange(
                TargetMethod.Body.Instructions
                    .Where(i => FieldInjectionProcessor.IsFieldOpCode(i.OpCode))
                    .Select(i => i.Operand)
                    .OfType<IField>()
                    .DistinctBy(i => i.Name.ToString())
                    .Select(i => GetStringLiteralField(i.Name, i.FullName))
                    .Where(i => i is not null)
                    .Select(i => i!).ToList()
            );
        }

        private void AddInvokeMembers(List<MemberDeclarationSyntax> injectMembers)
        {
            injectMembers.AddRange(TargetMethod.Body.Instructions
                .Where(i => InvokeInjectionProcessor.IsCallOpCode(i.OpCode))
                .Select(i => i.Operand)
                .OfType<IMethodDefOrRef>()
                .DistinctBy(i => ComputeSimpleMethodName(i, TargetType, true))
                .Select(i => GetStringLiteralField(ComputeSimpleMethodName(i, TargetType, true), i.FullName))
                .Where(i => i is not null)
                .Select(i => i!)
                .Distinct().ToList());
        }

        private readonly Regex _multipleUnderscoreMatcher = new("_{2,}", RegexOptions.Compiled);
        
        private MemberDeclarationSyntax? GetStringLiteralField(string name, string stringLiteral)
        {
            name = string.Join("", name.Select(c => SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_'));
            name = _multipleUnderscoreMatcher.Replace(name, "_");
            
            return FieldDeclaration(VariableDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    Identifier(name))
                                .WithInitializer(
                                    EqualsValueClause(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(stringLiteral)))))))
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)));
        }
    }
}