using System.Collections.Generic;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class ReturnInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Return;

        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute, int location,
            Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public override IEnumerable<int> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            for (var index = 0; index < action.TargetMethod.Body.Instructions.Count; index++)
            {
                var instruction = action.TargetMethod.Body.Instructions[index];
                if (instruction.OpCode == OpCodes.Ret) yield return index;
            }
        }
    }
}