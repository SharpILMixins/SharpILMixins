using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class FieldInjectionProcessor : InterestingOperandProcessor<IField>
    {
        public override AtLocation Location => AtLocation.Field;

        public override bool IsInterestingOpCode(OpCode code) => IsFieldOpCode(code);
        
        public static bool IsFieldOpCode(OpCode opCode)
        {
            return IsLoadFieldOpCode(opCode) || IsSetFieldOpCode(opCode);
        }

        private static bool IsSetFieldOpCode(OpCode opCode)
        {
            return opCode.Code switch
            {
                Code.Stfld => true,
                Code.Stsfld => true,
                _ => false
            };
        }

        /*public override int GetOpCodeInstructionOffset(MixinAction action, IList<Instruction> instructions,
            Instruction instruction)
        {
            var code = instruction.OpCode.Code;
            if (code != Code.Ldsfld && code != Code.Stsfld && action.HasCancelParameter)
            {
                var maxStack = MaxStackCalculator.GetMaxStack(instructions.Take(instructions.IndexOf(instruction) + 1).ToList(), action.TargetMethod.Body.ExceptionHandlers);
                return (int) -maxStack;
            }

            return base.GetOpCodeInstructionOffset(action, instructions, instruction);
        }*/

        public static bool IsLoadFieldOpCode(OpCode opCode)
        {
            return opCode.Code switch
            {
                Code.Ldfld => true,
                Code.Ldflda => true,
                Code.Ldsfld => true,
                Code.Ldsflda => true,
                _ => false
            };
        }
    }
}
