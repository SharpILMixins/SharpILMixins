using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class ReturnInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Return;

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
            if (attribute.Ordinal == -1)
            {
                foreach (var x in instructions.Select(bodyInstructions.IndexOf)) yield return x;
                yield break;
            }

            yield return bodyInstructions.IndexOf(instructions.ElementAtOrDefault(attribute.Ordinal) ??
                                                  throw new MixinApplyException(
                                                      $"Unable to find Return instruction with ordinal {attribute.Ordinal}"));
        }
    }
}