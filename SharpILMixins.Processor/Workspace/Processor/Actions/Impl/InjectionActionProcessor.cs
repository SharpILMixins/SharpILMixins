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
        public InjectionActionProcessor(MixinWorkspace workspace) : base(workspace)
        {
        }

        public override void ProcessAction(MixinAction action, InjectAttribute attribute)
        {
            var targetMethod = action.TargetMethod;

            var injectionProcessor = InjectionProcessorManager.InjectionProcessors.GetValueOrDefault(attribute.At) ??
                                     throw new MixinApplyException(
                                         $"Unable to find injection processor for location {attribute.At}");

            var points = injectionProcessor.FindInjectionPoints(action, attribute);

            foreach (var injectionPoint in points.OrderByDescending(c => c.BeforePoint))
            {
                var finalInjectionPoint = injectionPoint;
                var shiftAttribute = action.MixinMethod.GetCustomAttribute<ShiftAttribute>() ?? new ShiftAttribute
                    {Shift = attribute.Shift, ByAmount = attribute.ShiftByAmount};

                finalInjectionPoint += shiftAttribute.ByAmount;

                var index = finalInjectionPoint.BeforePoint;
                switch (shiftAttribute.Shift)
                {
                    case Shift.Before:
                        index = finalInjectionPoint.BeforePoint;
                        break;
                    case Shift.After:
                        index = finalInjectionPoint.AfterPoint;
                        break;
                    case Shift.By:
                        index += shiftAttribute.ByAmount;
                        break;
                }

                var instructions = injectionProcessor.GetInstructionsForAction(action, attribute, finalInjectionPoint,
                    targetMethod.Body.Instructions.ElementAtOrDefault(index));
                foreach (var instruction in instructions.Reverse())
                    targetMethod.Body.Instructions.Insert(index, instruction);
            }
        }
    }
}