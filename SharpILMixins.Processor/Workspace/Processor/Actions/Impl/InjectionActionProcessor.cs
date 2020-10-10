using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JetBrains.Annotations;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public class InjectionActionProcessor : BaseMixinActionProcessor<InjectAttribute>
    {
        public override void ProcessAction(MixinAction action, InjectAttribute attribute)
        {
            var targetMethod = action.TargetMethod;

            var injectionProcessor = InjectionProcessorManager.InjectionProcessors.GetValueOrDefault(attribute.At) ??
                                     throw new MixinApplyException(
                                         $"Unable to find injection processor for location {attribute.At}");

            var points = injectionProcessor.FindInjectionPoints(action, attribute);

            foreach (var injectionPoint in points.OrderByDescending(c => c))
            {
                var instructions = injectionProcessor.GetInstructionsForAction(action, attribute, injectionPoint,
                    targetMethod.Body.Instructions.ElementAtOrDefault(injectionPoint));
                foreach (var instruction in instructions.Reverse())
                {
                    targetMethod.Body.Instructions.Insert(injectionPoint, instruction);
                }
            }
        }

        public InjectionActionProcessor([NotNull] MixinWorkspace workspace) : base(workspace)
        {
        }
    }
}