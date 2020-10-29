using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class InvokeInjectionProcessor : InterestingOperandProcessor<IMethodDefOrRef>
    {
        public override AtLocation Location => AtLocation.Invoke;

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

        public override bool IsInterestingOpCode(OpCode code)
        {
            return IsCallOpCode(code);
        }
    }
}