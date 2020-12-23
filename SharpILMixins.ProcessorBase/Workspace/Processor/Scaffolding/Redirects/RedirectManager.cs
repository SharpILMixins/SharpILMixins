
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Obfuscation;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding.Redirects
{
    public record RedirectMapping(IMemberRef Member, int? Ordinal = null);
    
    public class RedirectManager
    {
        public RedirectManager(CopyScaffoldingHandler copyScaffoldingHandler)
        {
            CopyScaffoldingHandler = copyScaffoldingHandler;
            Workspace = copyScaffoldingHandler.Workspace;
            SigComparer = new SigComparer();
            ObfuscationMapManager = new ObfuscationMapManager(Workspace, this);
        }

        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(RedirectManager));

        public CopyScaffoldingHandler CopyScaffoldingHandler { get; }

        public MixinWorkspace Workspace { get; }

        public SigComparer SigComparer { get; }

        public ObfuscationMapManager ObfuscationMapManager { get; }

        public Dictionary<IMemberRef, RedirectMapping> GlobalMemberRedirectDictionary { get; } = new();

        public Dictionary<IMemberDef, Dictionary<IMemberRef, RedirectMapping>> LocalMemberRedirectDictionary { get; } =
            new();

        public Dictionary<string, TypeDef> TypeRedirectDictionary { get; } = new();

        public void RegisterRedirect(IMemberRef originalMember, IMemberRef newMember)
        {
            GlobalMemberRedirectDictionary.Remove(originalMember);
            GlobalMemberRedirectDictionary.Add(originalMember, new RedirectMapping(newMember));
        }

        public void RegisterScopeRedirect(IMemberDef scopeMember, IMemberRef originalMember, IMemberRef newMember, int? ordinal = null)
        {
            var localDict =
                LocalMemberRedirectDictionary.GetValueOrDefault(scopeMember, new Dictionary<IMemberRef, RedirectMapping>());

            localDict.Remove(originalMember);
            localDict.Add(originalMember, new RedirectMapping(newMember, ordinal));

            LocalMemberRedirectDictionary[scopeMember] = localDict;
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
            if (!method.HasBody) return;

            Workspace.PlaceholderManager.ProcessPlaceholders(body);
            foreach (var bodyVariable in body.Variables)
                bodyVariable.Type = ProcessTypeRedirect(bodyVariable.Type, method.DeclaringType.DefinitionAssembly);

            //body.KeepOldMaxStack = true;
            for (var index = 0; index < body.Instructions.Count; index++)
            {
                var instruction = body.Instructions[index];
                if (instruction.Operand is IMemberRef memberRef)
                {
                    PerformOperandReplacement(method, memberRef, instruction, index);
                }

                if (instruction.Operand is ITypeDefOrRef typeDefOrRef)
                    instruction.Operand = ResolveTypeDefIfNeeded(typeDefOrRef, method.DeclaringType.DefinitionAssembly);
            }
            
            //Optimize branches 
            method.Body.OptimizeBranches();
        }

        private void PerformOperandReplacement(MethodDef method, IMemberRef memberRef, Instruction instruction,
            int index)
        {
            var operandReplacement = memberRef;
            var hasMemberRedirectDictionary =
                LocalMemberRedirectDictionary.TryGetValue(method, out var memberRedirectDictionary);
            
            if (hasMemberRedirectDictionary)
            {
                operandReplacement = ProcessMemberRedirect(memberRef, out var modifiedScoped,
                    index, memberRedirectDictionary);

                if (modifiedScoped)
                {
                    Logger.Debug(
                        $"Performed scoped replacement of {instruction.Operand} with {operandReplacement} in {method}");
                    instruction.Operand = operandReplacement;
                }
            }

            operandReplacement = ProcessMemberRedirect(operandReplacement, out var modified, index,
                GlobalMemberRedirectDictionary);
            if (modified)
            {
                Logger.Debug($"Performed replacement of {instruction.Operand} with {operandReplacement}");
                instruction.Operand = operandReplacement;
            }
        }

        public T ProcessMemberRedirect<T>(T memberRef,
            IDictionary<IMemberRef, RedirectMapping>? memberRedirectDictionary = null) where T : IMemberRef
        {
            return (T) ProcessMemberRedirect(memberRef, out _, null, memberRedirectDictionary);
        }

        public IMemberRef ProcessMemberRedirect(IMemberRef memberRef, out bool modified,
            int? index = null, IDictionary<IMemberRef, RedirectMapping>? memberRedirectDictionary = null)
        {
            modified = false;
            var result =
                (memberRedirectDictionary ?? GlobalMemberRedirectDictionary).FirstOrDefault(m =>
                    m.Key.FullName.Equals(memberRef.FullName));
            if (result.IsDefault()) return memberRef;
            if (result.Value.Ordinal != null && result.Value.Ordinal != index) return memberRef;
            modified = true;
            return result.Value.Member;
        }

        public string RedirectType(string type)
        {
            var pair = TypeRedirectDictionary.FirstOrDefault(m => Equals(m.Key, type));
            return pair.IsDefault() ? type : pair.Value.FullName;
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

                case GenericInstSig genericInstSig:
                    return new GenericInstSig(
                        ProcessTypeRedirect(genericInstSig.GenericType, definitionAssembly).ToClassOrValueTypeSig(),
                        genericInstSig.GenericArguments.Select(t => ProcessTypeRedirect(t, definitionAssembly))
                            .ToList());

                case SZArraySig szArraySig:
                    return new SZArraySig(ProcessTypeRedirect(szArraySig.Next, definitionAssembly));

                case ValueTypeSig valueTypeSig:
                    return new ValueTypeSig(
                        TypeRedirectDictionary.GetValueOrDefault(valueTypeSig.TypeDefOrRef.FullName) ??
                        ResolveTypeDefIfNeeded(valueTypeSig.TypeDefOrRef, definitionAssembly));

                //Pass-through the corlib type signature.
                case CorLibTypeSig:
                    return parameterType;
            }

            if (parameterType != null)
                Logger.Warn(
                    $"Skipped translating type redirect for type {parameterType} ({parameterType.GetType().Name})");

            return parameterType;
        }

        public static ITypeDefOrRef ResolveTypeDefIfNeeded(ITypeDefOrRef defOrRef, IAssembly? definitionAssembly)
        {
            if (definitionAssembly == null) return defOrRef;

            //This is needed because otherwise we'll be referencing the target assembly
            if (definitionAssembly.FullName.Equals(defOrRef.DefinitionAssembly.FullName))
                return defOrRef.ResolveTypeDef() ?? defOrRef;
            return defOrRef;
        }
    }
}