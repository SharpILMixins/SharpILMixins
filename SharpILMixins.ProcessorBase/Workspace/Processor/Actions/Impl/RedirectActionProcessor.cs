using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JetBrains.Annotations;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public class RedirectActionProcessor : BaseMixinActionProcessor<RedirectAttribute>
    {
        private static readonly AtLocation[] SupportedAtLocations =
        {
            AtLocation.Invoke,
            AtLocation.NewObj,
            AtLocation.Constant,
            AtLocation.Field
        };

        public RedirectActionProcessor([NotNull] MixinWorkspace workspace) : base(workspace)
        {
        }

        public override void ProcessAction(MixinAction action, RedirectAttribute attribute)
        {
            var targetMethod = action.TargetMethod;

            var atLocation = attribute.At;
            if (!SupportedAtLocations.Contains(atLocation))
            {
                throw new MixinApplyException(
                    $"Invalid [Redirect] for AtLocation {atLocation}. Possible values: [{string.Join(", ", SupportedAtLocations)}]");
            }

            if (!action.MixinMethod.IsStatic && RequiresStaticMixin(action, attribute))
                throw new MixinApplyException($"Redirect method [{action.MixinMethod}] needs to be static.");

            var injectionProcessor = InjectionProcessorManager.InjectionProcessors.GetValueOrDefault(atLocation) ??
                                     throw new MixinApplyException(
                                         $"Unable to find redirecting injection processor for location {atLocation}");

            var injectionPoints = injectionProcessor.FindInjectionPoints(action, new InjectAttribute
            {
                At = atLocation,
                ConstantValue = attribute.ConstantValue,
                Method = attribute.Method,
                Ordinal = attribute.Ordinal,
                Priority = attribute.Priority,
                Target = attribute.Target
            }).ToList();

            var ordinalNotFound =
                new MixinApplyException(
                    $"Unable to find injection point for Mixin Method {action.MixinMethod} with ordinal {attribute.Ordinal}");

            var bodyInstructions = targetMethod.Body.Instructions;
            if (injectionPoints.Count == -1)
            {
                throw new MixinApplyException($"Unable to find target for Redirect Mixin Method {action.MixinMethod}");
            }

            if (attribute.Ordinal > -1)
            {
                var injectionPointAtOrdinal = injectionPoints.ElementAt(attribute.Ordinal) ?? throw ordinalNotFound;
                injectionPoints = new List<InjectionPoint> {injectionPointAtOrdinal};
            }

            injectionPoints = injectionPoints.OrderByDescending(c => c.BeforePoint).ToList();

            foreach (var injectionPoint in injectionPoints)
            {
                var instruction = bodyInstructions.ElementAtOrDefault(injectionPoint.BeforePoint) ??
                                  throw ordinalNotFound;

                // Rewrite NewObj into method call
                if (instruction.OpCode == OpCodes.Newobj)
                    instruction.OpCode = OpCodes.Call;
                
                CreateMethodCallIfPossible(action, atLocation, instruction, bodyInstructions, injectionPoint);

                // Rewrite set field into our own call
                if (FieldInjectionProcessor.IsSetFieldOpCode(instruction.OpCode))
                {
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = action.MixinMethod;
                }
                else if (instruction.Operand is IMemberRef operand && atLocation != AtLocation.Field)
                {
                    Workspace.RedirectManager.RegisterScopeRedirect(action.TargetMethod, operand, action.MixinMethod);
                }
            }
        }

        private static void CreateMethodCallIfPossible(MixinAction action, AtLocation atLocation, Instruction instruction,
            IList<Instruction> bodyInstructions, InjectionPoint injectionPoint)
        {
            if (atLocation == AtLocation.Constant || atLocation == AtLocation.Field)
            {
                if (!FieldInjectionProcessor.IsSetFieldOpCode(instruction.OpCode))
                {
                    bodyInstructions.Insert(injectionPoint.AfterPoint,
                        new Instruction(OpCodes.Call, action.MixinMethod));

                    if (atLocation == AtLocation.Field && instruction.Operand is FieldDef {IsStatic: false})
                    {
                        bodyInstructions.Insert(injectionPoint.BeforePoint,
                            new Instruction(OpCodes.Ldarg_0)); // Add back the this instance
                    }
                }
            }
        }

        private bool RequiresStaticMixin(MixinAction action, RedirectAttribute attribute)
        {
            if (attribute.At == AtLocation.Field)
                return false;
            return true;
        }
    }
}