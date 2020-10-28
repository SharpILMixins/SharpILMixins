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
            GetInstructionsForAction(MixinAction action, InjectAttribute attribute, InjectionPoint location,
                Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public abstract IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute);
    }
}