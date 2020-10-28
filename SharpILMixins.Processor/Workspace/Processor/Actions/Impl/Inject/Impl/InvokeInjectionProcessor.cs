using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class InvokeInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Invoke;

        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute,
            InjectionPoint location,
            Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public override IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            var instructions = action.TargetMethod.Body.Instructions;

            foreach (Instruction i in instructions)
            {
                if (!IsCallOpCode(i.OpCode)) continue;
                if (i.Operand is IMethodDefOrRef method && IsTargetMethod(attribute, method))
                    yield return new InjectionPoint(instructions.IndexOf(i));
            }
        }

        private static bool IsTargetMethod(InjectAttribute attribute, IMemberRef method)
        {
            return method.FullName.Equals(attribute.Target);
        }

        public static bool IsCallOpCode(OpCode code)
        {
            return code.Code switch
            {
                Code.Call => true,
                Code.Calli => true,
                Code.Callvirt => true,
                _ => false
            };
        }
    }
}