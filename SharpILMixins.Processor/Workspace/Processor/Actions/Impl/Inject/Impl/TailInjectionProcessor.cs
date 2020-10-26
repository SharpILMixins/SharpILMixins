using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class TailInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Tail;

        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute,
            int location,
            Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public override IEnumerable<int> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            var bodyInstructions = action.TargetMethod.Body.Instructions;
            var instructions = bodyInstructions.Where(i => i.OpCode == OpCodes.Ret);

            yield return bodyInstructions.IndexOf(instructions.LastOrDefault() ??
                                                  throw new MixinApplyException(
                                                      $"Unable to find Return instruction"));
        }
    }
}