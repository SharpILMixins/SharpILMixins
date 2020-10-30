using System;
using System.Collections.Generic;
using System.Linq;
using Dynamitey.DynamicObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpILMixins.Processor.Workspace.Generator
{
    public class GeneratorMixinRelation
    {
        public MixinRelation MixinRelation { get; }

        public string SimpleTargetName { get; }

        public List<GeneratorMixinAction> MixinActions { get; }

        public GeneratorMixinRelation(MixinRelation mixinRelation)
        {
            MixinRelation = mixinRelation;
            MixinActions = mixinRelation.MixinActions
                .Select(a => new GeneratorMixinAction(a))
                .Where(i => i.MixinAction.GetIsValid())
                .DistinctBy(a => a.SimpleTargetMethodName).ToList();
            var targetName = mixinRelation.GetTargetName();
            SimpleTargetName = targetName.Substring(Math.Max(0, targetName.LastIndexOf('.') + 1));
        }


        private readonly SyntaxTokenList _publicStaticModifiers =
            TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

        public ClassDeclarationSyntax? ToSyntax()
        {
            var methodConstants = MixinActions.SelectMany(c => c.ToSyntax()).ToArray();
            if (methodConstants.Length == 0)
                return null;

            return ClassDeclaration($"{SimpleTargetName}Targets")
                .AddMembers(
                    ClassDeclaration("Methods")
                        .AddMembers(methodConstants.ToArray())
                        .WithModifiers(_publicStaticModifiers)
                )
                .WithModifiers(_publicStaticModifiers)
                .NormalizeWhitespace();
        }
    }
}