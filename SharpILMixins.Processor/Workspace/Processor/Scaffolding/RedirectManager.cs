using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ninject.Injection;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class RedirectManager
    {
        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(RedirectManager));

        public CopyScaffoldingHandler CopyScaffoldingHandler { get; }

        public MixinWorkspace Workspace { get; }

        public RedirectManager(CopyScaffoldingHandler copyScaffoldingHandler)
        {
            CopyScaffoldingHandler = copyScaffoldingHandler;
            Workspace = copyScaffoldingHandler.Workspace;
            SigComparer = new SigComparer();
        }

        public SigComparer SigComparer { get; }

        public Dictionary<IMemberRef, IMemberRef> Dictionary { get; } = new Dictionary<IMemberRef, IMemberRef>();
        public Dictionary<string, TypeDef> TypeRedirectDictionary { get; } = new Dictionary<string, TypeDef>();

        public void RegisterRedirect(IMemberRef originalMember, IMemberRef newMember)
        {
            Dictionary.Add(originalMember, newMember);
        }

        public void RegisterTypeRedirect(TypeDef originalMember, TypeDef newMember)
        {
            foreach (var accessorMethod in originalMember.Methods)
            {
                var targetMethod = MixinAction.GetTargetMethod(accessorMethod,
                    accessorMethod.GetCustomAttribute<BaseMixinAttribute>(),
                    newMember, Workspace);
                if (targetMethod != null)
                {
                    Logger.Debug(
                        $"Found target method {targetMethod.FullName} for accessor method {accessorMethod.FullName}");
                    RegisterRedirect(accessorMethod, targetMethod);
                }
            }

            foreach (var accessorField in originalMember.Fields)
            {
                var targetField = newMember.FindField(accessorField.Name);
                if (targetField != null)
                {
                    Logger.Debug(
                        $"Found target field {targetField.FullName} for accessor field {accessorField.FullName}");
                    RegisterRedirect(accessorField, targetField);
                }
            }

            TypeRedirectDictionary.Add(originalMember.FullName, newMember);
        }

        public void ProcessRedirects(MethodDef method, CilBody body)
        {
            Workspace.PlaceholderManager.ProcessPlaceholders(body);
            foreach (var bodyVariable in body.Variables)
            {
                bodyVariable.Type = ProcessTypeRedirect(bodyVariable.Type, method.DeclaringType.DefinitionAssembly);
            }

            //body.KeepOldMaxStack = true;
            foreach (var instruction in body.Instructions)
            {
                if (instruction.Operand is IMemberRef memberRef)
                {
                    var operandReplacement = Dictionary.FirstOrDefault(m => m.Key.FullName.Equals(memberRef.FullName));
                    if (!operandReplacement.IsDefault())
                    {
                        Logger.Debug($"Performed replacement of {instruction.Operand} with {operandReplacement.Value}");
                        instruction.Operand = operandReplacement.Value;
                    }

                    //PerformTypeReplacement(memberRef, instruction);
                }

                if (instruction.Operand is ITypeDefOrRef typeDefOrRef && typeDefOrRef.DefinitionAssembly.FullName.Equals(method.DeclaringType.DefinitionAssembly.FullName))
                {
                    instruction.Operand = typeDefOrRef.ResolveTypeDef() ?? typeDefOrRef;
                }
            }
        }

        private void PerformTypeReplacement(IMemberRef iMemberRef, Instruction instruction)
        {
            if (iMemberRef.DeclaringType == null) return;
            var typeReplacement =
                TypeRedirectDictionary.FirstOrDefault(m =>
                    SigComparer.Equals(m.Key, iMemberRef.DeclaringType.FullName));
            if (!typeReplacement.IsDefault())
            {
                var replacementValue = typeReplacement.Value;

                if (iMemberRef is MemberRef memberRef)
                {
                    memberRef.Class = replacementValue;
                }
                else if (iMemberRef is MethodDef methodDef && replacementValue is TypeDef typeDef)
                {
                    var baseMixinAttribute = methodDef.GetCustomAttribute<BaseMixinAttribute>();
                    if (baseMixinAttribute != null)
                    {
                        var targetMethod =
                            MixinAction.GetTargetMethod(methodDef, baseMixinAttribute, typeDef, Workspace);

                        Debugger.Break();
                    }
                }
                else
                {
                    throw new MixinApplyException(
                        $"Unable to apply type replacement for member ref of type \"{iMemberRef.GetType().Name}\"");
                }
            }
        }

        public string RedirectType(string type)
        {
            var pair = TypeRedirectDictionary.FirstOrDefault(m => Equals(m.Key, type));
            return pair.IsDefault() ? type : pair.Value.FullName;
        }

        public IMemberRefParent? RedirectTypeMember(string type)
        {
            var pair = TypeRedirectDictionary.FirstOrDefault(m => Equals(m.Key, type));
            return pair.IsDefault() ? null : pair.Value;
        }

        public TypeSig? ProcessTypeRedirect(TypeSig? parameterType, IAssembly? definitionAssembly)
        {
            switch (parameterType)
            {
                case ClassSig classSig:
                    return new ClassSig(TypeRedirectDictionary.GetValueOrDefault(classSig.TypeDefOrRef.FullName) ??
                                        ResolveTypeDefIfNeeded(classSig.TypeDefOrRef, definitionAssembly));

                case ByRefSig byRefSig:
                    return new ByRefSig(ProcessTypeRedirect(byRefSig.Next, definitionAssembly));

                case ValueTypeSig valueTypeSig:
                    return new ValueTypeSig(
                        TypeRedirectDictionary.GetValueOrDefault(valueTypeSig.TypeDefOrRef.FullName) ??
                        ResolveTypeDefIfNeeded(valueTypeSig.TypeDefOrRef, definitionAssembly));
            }

            if (parameterType != null)
            {
                Logger.Warn(
                    $"Skipped translating type redirect for type {parameterType} ({parameterType.GetType().Name})");
            }

            return parameterType;
        }

        private static ITypeDefOrRef ResolveTypeDefIfNeeded(ITypeDefOrRef defOrRef, IAssembly? definitionAssembly)
        {
            if (definitionAssembly == null) return defOrRef;

            //This is needed because otherwise we'll be referencing the target assembly
            if (definitionAssembly.FullName.Equals(defOrRef.DefinitionAssembly.FullName))
                return defOrRef.ResolveTypeDef() ?? defOrRef;
            return defOrRef;
        }
    }
}