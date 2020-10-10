using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
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
        public Dictionary<string, IMemberRefParent> TypeRedirectDictionary { get; } = new Dictionary<string, IMemberRefParent>();

        public void RegisterRedirect(IMemberRef originalMember, IMemberRef newMember)
        {
            Dictionary.Add(originalMember, newMember);
        }
        public void RegisterTypeRedirect(TypeDef originalMember, TypeDef newMember)
        {
            foreach (var accessorMethod in originalMember.Methods)
            {
                var targetMethod = MixinAction.GetTargetMethod(accessorMethod, accessorMethod.GetCustomAttribute<BaseMixinAttribute>(),
                    newMember, Workspace);
                if (targetMethod != null)
                {
                    Logger.Debug($"Found target method {targetMethod.FullName} for accessor method {accessorMethod.FullName}");
                    RegisterRedirect(accessorMethod, targetMethod);
                }
            }
            TypeRedirectDictionary.Add(originalMember.FullName, newMember);
        }

        public void ProcessRedirects(CilBody body)
        {
            //body.KeepOldMaxStack = true;
            foreach (var instruction in body.Instructions)
            {
                if (instruction.Operand is IMemberRef memberRef)
                {
                    var operandReplacement = Dictionary.FirstOrDefault(m => m.Key.FullName.Equals(memberRef.FullName));
                    if (!operandReplacement.IsDefault())
                    {
                        instruction.Operand = operandReplacement.Value;
                    }

                    //PerformTypeReplacement(memberRef, instruction);
                }
            }
        }

        private void PerformTypeReplacement(IMemberRef iMemberRef, Instruction instruction)
        {
            if (iMemberRef.DeclaringType == null) return;
            var typeReplacement =
                TypeRedirectDictionary.FirstOrDefault(m => SigComparer.Equals(m.Key, iMemberRef.DeclaringType.FullName));
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
                        var targetMethod = MixinAction.GetTargetMethod(methodDef, baseMixinAttribute, typeDef, Workspace);
                        
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
    }
}