using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class ConstantInjectionProcessor : InterestingOperandProcessor<object>
    {
        public override AtLocation Location => AtLocation.Constant;

        public override bool IsInterestingInstruction(Instruction instruction, InjectAttribute injectAttribute)
        {
            var isLoad = instruction.OpCode.Code.ToString().StartsWith("Ld");
            return isLoad && FixValue(instruction.Operand)?.Equals(FixValue(injectAttribute.ConstantValue)) == true;
        }

        private static object? FixValue(object obj)
        {
            if (obj is UTF8String)
                return obj.ToString();
            return obj;
        }
    }
}