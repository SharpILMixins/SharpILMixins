using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject
{
    public abstract class BaseInjectionProcessor
    {
        protected IEnumerable<Instruction> GetInstructionsWithOpCode(IList<Instruction> instructions, OpCode opCode)
        {
            return instructions
                .SelectMany(ResolveInstructions)
                .Where(i => i.code == opCode)
                .Select(c => c.instruction);
        }

        private IEnumerable<(Instruction instruction, OpCode code)> ResolveInstructions(Instruction arg)
        {
            yield return (arg, arg.OpCode);
            if (arg.Operand is Instruction instruction)
            {
                yield return (arg, instruction.OpCode);
            }
        }

        public abstract AtLocation Location { get; }

        public virtual IEnumerable<Instruction>
            GetInstructionsForAction(MixinAction action, InjectAttribute attribute, InjectionPoint location,
                Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public abstract IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute);
    }
}