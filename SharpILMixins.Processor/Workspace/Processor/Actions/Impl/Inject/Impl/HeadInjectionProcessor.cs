using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class HeadInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Head;

        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute, int location, Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public override IEnumerable<int> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            yield return 0;
        }
    }
}