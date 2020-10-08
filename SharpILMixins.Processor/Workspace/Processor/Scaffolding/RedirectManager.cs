using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class RedirectManager
    {
        public CopyScaffoldingHandler CopyScaffoldingHandler { get; }

        public MixinWorkspace Workspace { get; }

        public RedirectManager(CopyScaffoldingHandler copyScaffoldingHandler)
        {
            CopyScaffoldingHandler = copyScaffoldingHandler;
            Workspace = copyScaffoldingHandler.Workspace;
            SigComparer = new SigComparer(SigComparerOptions.DontCompareTypeScope);
        }

        public SigComparer SigComparer { get; }

        public Dictionary<IMemberRef, IMemberRef> Dictionary { get; } = new Dictionary<IMemberRef, IMemberRef>();
        public Dictionary<string, IMemberRefParent> TypeRedirectDictionary { get; } = new Dictionary<string, IMemberRefParent>();

        public void RegisterRedirect(IMemberRef originalMember, IMemberRef newMember)
        {
            Dictionary.Add(originalMember, newMember);
        }
        public void RegisterTypeRedirect(IMemberRefParent originalMember, IMemberRefParent newMember)
        {
            TypeRedirectDictionary.Add(originalMember.FullName, newMember);
        }

        public void ProcessRedirects(CilBody body)
        {
            //body.KeepOldMaxStack = true;
            foreach (var instruction in body.Instructions)
            {
                if (instruction.Operand is IMemberRef memberRef)
                {
                    var operandReplacement = Dictionary.FirstOrDefault(m => SigComparer.Equals(m.Key, memberRef));
                    if (!operandReplacement.IsDefault())
                    {
                        instruction.Operand = operandReplacement.Value;
                    }

                    PerformTypeReplacement(memberRef);
                }
            }
        }

        private void PerformTypeReplacement(IMemberRef iMemberRef)
        {
            if (iMemberRef.DeclaringType == null) return;
            var typeReplacement =
                TypeRedirectDictionary.FirstOrDefault(m => SigComparer.Equals(m.Key, iMemberRef.DeclaringType.FullName));
            if (!typeReplacement.IsDefault())
            {
                if (iMemberRef is MemberRef memberRef)
                {
                    memberRef.Class = typeReplacement.Value;
                }
                else
                {
                    throw new MixinApplyException($"Unable to apply type replacement for member ref of type \"{iMemberRef.GetType().Name}\"");
                }
            }
        }
    }
}