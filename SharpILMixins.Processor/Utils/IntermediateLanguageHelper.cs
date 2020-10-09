﻿using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Parameters;
using SharpILMixins.Processor.Workspace.Processor.Actions;
using SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject;

namespace SharpILMixins.Processor.Utils
{
    public static class IntermediateLanguageHelper
    {
        public delegate void ModifyParameterInstructionHandler(int argumentIndex,
            ref IEnumerable<Instruction> parameterInstructions, List<Instruction> afterCallInstructions);

        public static IEnumerable<Instruction> InvokeMethod(MethodDef methodToInvoke, int argumentsToPass,
            ModifyParameterInstructionHandler? modifyParameterHandler = null, MethodDef? targetMethod = null)
        {
            var parametersMethod = targetMethod ?? methodToInvoke;
            var paramsMethodParams = parametersMethod.Parameters.Where(p => !p.IsHiddenThisParameter).ToArray();
            if (!methodToInvoke.IsStatic)
            {
                yield return new Instruction(OpCodes.Ldarg_0); //this instance
            }

            var afterCallInstructions = new List<Instruction>();
            for (var i = 0; i < argumentsToPass; i++)
            {
                var isRef = methodToInvoke.GetParams()[i].IsByRef && !methodToInvoke.GetParams()[i].IsByRef;
                IEnumerable<Instruction> ldArgInst = new[]
                    {new Instruction(isRef ? OpCodes.Ldarga : OpCodes.Ldarg, paramsMethodParams.ElementAtOrDefault(i))};


                modifyParameterHandler?.Invoke(i, ref ldArgInst, afterCallInstructions);

                foreach (var instruction in ldArgInst)
                {
                    yield return instruction; //parameter
                }
            }

            yield return new Instruction(OpCodes.Call, methodToInvoke);

            foreach (var instruction in afterCallInstructions)
            {
                yield return instruction;
            }
        }

        public static IEnumerable<Instruction> InvokeMethod(MixinAction action, Instruction? nextInstruction = null)
        {
            return InvokeMethod(action.MixinMethod, action.MixinMethod.GetParamCount(),
                (int index, ref IEnumerable<Instruction> instructions, List<Instruction> callInstructions) =>
                    HandleParameterInstruction(action, index, ref instructions, callInstructions, nextInstruction), action.TargetMethod);
        }

        public static void HandleParameterInstruction(MixinAction action, int index,
            ref IEnumerable<Instruction> instruction, List<Instruction> afterCallInstructions,
            Instruction? nextInstruction)
        {
            //action.TargetMethod.Body.KeepOldMaxStack = true;
            var attribute = action.MixinMethod.ParamDefs[index].GetCustomAttribute<BaseParameterAttribute>();
            if (attribute != null)
            {
                switch (attribute)
                {
                    case InjectCancelParamAttribute injectCancelParamAttribute:
                        var isCancelledVariable =
                            new Local(new ClassSig(action.MixinMethod.Module.CorLibTypes.Boolean.TypeDefOrRef), Utilities.GenerateRandomName("isCancelled"));
                        action.TargetMethod.Body.Variables.Add(isCancelledVariable);

                        instruction = new[]
                        {
                            new Instruction(OpCodes.Ldc_I4_0), //Load false
                            new Instruction(OpCodes.Stloc, isCancelledVariable), //Set variable to false
                            new Instruction(OpCodes.Ldloca, isCancelledVariable) //Load it for the method
                        };



                        afterCallInstructions.Add(new Instruction(OpCodes.Ldloc, isCancelledVariable));
                        afterCallInstructions.Add(new Instruction(OpCodes.Brfalse, nextInstruction ?? throw new MixinApplyException($"Unable to find next instruction to jump to after cancelling the method {action.TargetMethod} on {action.MixinMethod}")));
                        afterCallInstructions.Add(new Instruction(OpCodes.Ret));

                        break;
                    default:
                        throw new MixinApplyException(
                            $"Unable to process {attribute} in parameter {index} on {action.MixinMethod}.");
                }
            }
        }
    }
}