using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class NewObjInjectionProcessor : InterestingOperandProcessor<IMethodDefOrRef>
    {
        public override AtLocation Location => AtLocation.NewObj;
        
        public override bool IsInterestingInstruction(Instruction instruction, InjectAttribute injectAttribute)
        {
            return IsNewObjInstruction(instruction);
        }

        public static bool IsNewObjInstruction(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Newobj;
        }
    }
}