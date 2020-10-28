using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public abstract class InterestingOperandProcessor<T> : BaseInjectionProcessor where T: IFullName
    {
        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute,
            InjectionPoint location,
            Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction, true);
        }


        public override IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            var instructions = action.TargetMethod.Body.Instructions;

            foreach (Instruction i in instructions)
            {
                if (!IsInterestingOpCode(i.OpCode)) continue;
                if (i.Operand is T target && IsTargetOperand(attribute, target))
                    yield return new InjectionPoint(instructions.IndexOf(i) + GetOpCodeInstructionOffset(action, instructions, i));
            }
        }

        private static bool IsTargetOperand(InjectAttribute attribute, T method)
        {
            return method.FullName.Equals(attribute.Target);
        }

        public virtual int GetOpCodeInstructionOffset(MixinAction action, IList<Instruction> instructions,
            Instruction instruction) => 0;

        public abstract bool IsInterestingOpCode(OpCode code);
    }
}