using System.Collections.Generic;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject
{
    public abstract class BaseInjectionProcessor
    {
        public abstract AtLocation Location { get; }

        public virtual IEnumerable<Instruction>
            GetInstructionsForAction(MixinAction action, InjectAttribute attribute, int location)
        {
            throw new MixinApplyException($"No implementation found for {nameof(GetInstructionsForAction)} of type {GetType().FullName}");
        }

        public virtual IEnumerable<Instruction>
            GetInstructionsForAction(MixinAction action, InjectAttribute attribute, int location,
                Instruction? nextInstruction)
        {
            return GetInstructionsForAction(action, attribute, location);
        }

        public abstract IEnumerable<int> FindInjectionPoints(MixinAction action, InjectAttribute attribute);
    }
}