using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public void RegisterScopeRedirect(IMemberDef scopeMember, IMemberRef originalMember, IMemberRef newMember,
            int? ordinal = null)
        {
            var localDict =
                LocalMemberRedirectDictionary.GetValueOrDefault(scopeMember,
                    new Dictionary<IMemberRef, RedirectMapping>());

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
                    PerformOperandResolveIfNeeded(instruction);
                }

                if (instruction.Operand is MethodSpec { Method: IMemberRef memberSpecRef })
                {
                    PerformOperandReplacement(method, memberSpecRef, instruction, index);
                    PerformOperandResolveIfNeeded(instruction);
                }

                if (instruction.Operand is ITypeDefOrRef typeDefOrRef)
                    instruction.Operand = ResolveTypeDefIfNeeded(typeDefOrRef, method.DeclaringType.DefinitionAssembly)
                        ;
                if (instruction.Operand is IMethodDefOrRef methodDefOrRef)
                    instruction.Operand =
                        ResolveMethodDefIfNeeded(methodDefOrRef, method.DeclaringType.DefinitionAssembly);

                if (instruction.Operand is MethodSpec methodSpec)
                {
                    instruction.Operand = new MethodSpecUser(
                        ResolveMethodDefIfNeeded(methodSpec.Method, methodSpec.Method.DeclaringType.DefinitionAssembly),
                        new GenericInstMethodSig(methodSpec.GenericInstMethodSig.GenericArguments
                            .Select(ProcessTypeRedirect).ToList())
                        );
                }
            }
        }

        private void PerformOperandResolveIfNeeded(Instruction instruction)
        {
            if (instruction.Operand is not MemberRef memberRef) return;
            var resolved = memberRef.Resolve();
            if (resolved?.DeclaringType.DefinitionAssembly?.Name.Equals(Workspace.CurrentTargetModule?.Assembly.Name) ==
                true)
            {
                instruction.Operand = resolved;
            }
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
            return (T)ProcessMemberRedirect(memberRef, out _, null, memberRedirectDictionary);
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

        public TypeSig? ProcessTypeRedirect(TypeSig? parameterType)
        {
            return ProcessTypeRedirect(parameterType, parameterType?.DefinitionAssembly);
        }

        public TypeSig? ProcessTypeRedirect(TypeSig? parameterType, IAssembly? definitionAssembly)
        {
            switch (parameterType)
            {
                case ClassSig classSig:
                    return new ClassSig(TypeRedirectDictionary.GetValueOrDefault(classSig.TypeDefOrRef.FullName) ??
                                        ResolveTypeDefIfNeeded(classSig.TypeDefOrRef,
                                            classSig.TypeDefOrRef.DefinitionAssembly));
                case ByRefSig byRefSig:
                    return new ByRefSig(ProcessTypeRedirect(byRefSig.Next, definitionAssembly));
                case GenericInstSig genericInstSig:
                    return new GenericInstSig(
                        ProcessTypeRedirect(genericInstSig.GenericType, genericInstSig.GenericType.DefinitionAssembly)
                            .ToClassOrValueTypeSig(),
                        genericInstSig.GenericArguments.Select(t => ProcessTypeRedirect(t, t.DefinitionAssembly))
                            .ToList());
                case ArraySig arraySig:
                    return new ArraySig(ProcessTypeRedirect(arraySig.Next, arraySig.Next.DefinitionAssembly),
                        arraySig.Rank,
                        arraySig.Sizes, arraySig.LowerBounds);
                case SZArraySig szArraySig:
                    return new SZArraySig(ProcessTypeRedirect(szArraySig.Next, szArraySig.Next.DefinitionAssembly));
                case ValueArraySig valueArraySig:
                    return new ValueArraySig(
                        ProcessTypeRedirect(valueArraySig.Next, valueArraySig.Next.DefinitionAssembly),
                        valueArraySig.Size);
                case ValueTypeSig valueTypeSig:
                    return new ValueTypeSig(
                        TypeRedirectDictionary.GetValueOrDefault(valueTypeSig.TypeDefOrRef.FullName) ??
                        ResolveTypeDefIfNeeded(valueTypeSig.TypeDefOrRef,
                            valueTypeSig.TypeDefOrRef.DefinitionAssembly));
                case CorLibTypeSig:
                    return parameterType;
            }

            if (parameterType != null)
                Logger.Warn(
                    $"Skipped translating type redirect for type {parameterType} ({parameterType.GetType().Name})");

            return parameterType;
        }

        public ITypeDefOrRef ResolveTypeDefIfNeeded(ITypeDefOrRef defOrRef, IAssembly? definitionAssembly)
        {
            if (definitionAssembly == null) return defOrRef;
            var isReferencingTargetAssembly = IsReferencingTargetAssembly(defOrRef);

            //Create a Type Reference if it isn't one
            ITypeDefOrRef defaultTypeRef = CreateTypeRef(defOrRef);

            // If we are given an array, try to handle it as best as we can
            if (defOrRef.IsTypeSpec)
            {
                var resultTypeRedirect = ProcessTypeRedirect(defOrRef.ToTypeSig(), defOrRef.DefinitionAssembly)
                    .ToTypeDefOrRef();
                return isReferencingTargetAssembly ? resultTypeRedirect.ResolveTypeDefThrow() : resultTypeRedirect;
            }

            //Only create references to other assemblies. Our Target assembly needs TypeDefs so it doesn't reference itself.
            if (isReferencingTargetAssembly)
                return defOrRef.ResolveTypeDef() ?? defaultTypeRef;

            return defaultTypeRef;
        }

        private static ITypeDefOrRef CreateTypeRef(ITypeDefOrRef defOrRef)
        {
            return defOrRef.IsTypeRef
                ? defOrRef
                : new TypeRefUser(defOrRef.Module, defOrRef.Namespace, defOrRef.Name,
                    defOrRef.DefinitionAssembly.ToAssemblyRef());
        }

        private bool IsReferencingTargetAssembly(ITypeDefOrRef defOrRef)
        {
            var targetAssemblyFullName = Workspace.CurrentTargetModule?.Assembly.FullName;
            var mixinAssemblyFullName = Workspace.MixinModule?.Assembly.FullName;
            var definitionAssemblyFullName = defOrRef.DefinitionAssembly?.FullName;

            //Is referencing target assembly if the target assembly or original mixins module are referenced
            return targetAssemblyFullName?.Equals(definitionAssemblyFullName) == true ||
                   mixinAssemblyFullName?.Equals(definitionAssemblyFullName) == true;
        }

        public IMethodDefOrRef ResolveMethodDefIfNeeded(IMethodDefOrRef defOrRef, IAssembly? definitionAssembly)
        {
            if (definitionAssembly == null) return defOrRef;
            if (defOrRef.DeclaringType.NumberOfGenericParameters > 0 && defOrRef.IsMemberRef)
            {
                defOrRef = new MemberRefUser(defOrRef.Module, defOrRef.Name, ProcessSignature(defOrRef.MethodSig),
                    ProcessTypeRedirect(defOrRef.DeclaringType.ToTypeSig()).ToTypeDefOrRef());
            }

            //Only create references to methods in other assemblies. Methods in our Target assembly needs MethodDefs so we don't reference our own assembly.
            if (IsReferencingTargetAssembly(defOrRef.DeclaringType))
                return defOrRef.ResolveMethodDef() ?? defOrRef;

            if (defOrRef.Module == null || defOrRef.MethodSig == null || defOrRef.DeclaringType == null ||
                defOrRef.DeclaringType.DefinitionAssembly == null) return defOrRef;
            return new MemberRefUser(defOrRef.Module, defOrRef.Name, defOrRef.MethodSig,
                ResolveTypeDefIfNeeded(defOrRef.DeclaringType, defOrRef.DeclaringType.DefinitionAssembly));
        }

        public void ProcessField(FieldDef field)
        {
            field.FieldType = ProcessTypeRedirect(field.FieldType, field.FieldType.DefinitionAssembly);
        }

        public MethodSig ProcessSignature(MethodSig sig)
        {
            return new MethodSig(sig.CallingConvention, sig.GenParamCount, ProcessTypeRedirect(sig.RetType),
                sig.Params?.Select(ProcessTypeRedirect)?.ToList(),
                sig.ParamsAfterSentinel?.Select(ProcessTypeRedirect)?.ToList());
        }
    }
}