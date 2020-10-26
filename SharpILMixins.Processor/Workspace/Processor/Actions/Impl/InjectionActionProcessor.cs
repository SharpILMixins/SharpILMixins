using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public class InjectionActionProcessor : BaseMixinActionProcessor<InjectAttribute>
    {
        public InjectionActionProcessor([NotNull] MixinWorkspace workspace) : base(workspace)
        {
        }

        public override void ProcessAction(MixinAction action, InjectAttribute attribute)
        {
            var targetMethod = action.TargetMethod;

            var injectionProcessor = InjectionProcessorManager.InjectionProcessors.GetValueOrDefault(attribute.At) ??
                                     throw new MixinApplyException(
                                         $"Unable to find injection processor for location {attribute.At}");

            var points = injectionProcessor.FindInjectionPoints(action, attribute);

            foreach (var injectionPoint in points.OrderByDescending(c => c))
            {
                var finalInjectionPoint = injectionPoint;
                finalInjectionPoint += action.MixinMethod.GetCustomAttribute<ShiftAttribute>()?.ByAmount ?? 0;

                var instructions = injectionProcessor.GetInstructionsForAction(action, attribute, finalInjectionPoint,
                    targetMethod.Body.Instructions.ElementAtOrDefault(finalInjectionPoint));
                foreach (var instruction in instructions.Reverse())
                    targetMethod.Body.Instructions.Insert(finalInjectionPoint, instruction);
            }
        }
    }
}