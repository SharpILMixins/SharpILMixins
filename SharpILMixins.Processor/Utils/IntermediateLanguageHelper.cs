using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Parameters;
using SharpILMixins.Processor.Workspace;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Utils
{
    public static class IntermediateLanguageHelper
    {
        public delegate void ModifyParameterInstructionHandler(int argumentIndex,
            ref IEnumerable<Instruction> parameterInstructions, List<Instruction> afterCallInstructions);

        public static IEnumerable<Instruction> InvokeMethod(MixinWorkspace workspace, MethodDef methodToInvoke,
            int argumentsToPass, ModifyParameterInstructionHandler? modifyParameterHandler = null,
            MethodDef? targetMethod = null, bool discardResult = false,
            IEnumerable<Instruction>? callerArguments = null)
        {
            if (targetMethod != null && workspace.MixinProcessor.CopyScaffoldingHandler.IsMethodInlined(methodToInvoke))
            {
                foreach (var instruction1 in InvokeMethodAsInlined(methodToInvoke, targetMethod))
                    yield return instruction1;
                yield break;
            }

            var afterCallInstructions = new List<Instruction>();

            var passCallerArguments = callerArguments ??
                                      PassCallerArguments(methodToInvoke, argumentsToPass, modifyParameterHandler,
                                          targetMethod, afterCallInstructions);
            foreach (var instruction2 in passCallerArguments) yield return instruction2;

            yield return new Instruction(OpCodes.Call, methodToInvoke);
            if (discardResult && methodToInvoke.HasReturnType)
                yield return new Instruction(OpCodes.Pop);

            foreach (var instruction in afterCallInstructions) yield return instruction;
        }

        private static IEnumerable<Instruction> PassCallerArguments(MethodDef methodToInvoke, int argumentsToPass,
            ModifyParameterInstructionHandler? modifyParameterHandler, MethodDef? targetMethod,
            List<Instruction> afterCallInstructions)
        {
            var parametersMethod = targetMethod ?? methodToInvoke;
            var paramsMethodParams = parametersMethod.Parameters.Where(p => !p.IsHiddenThisParameter).ToArray();
            List<Instruction> arguments = new List<Instruction>();

            if (!methodToInvoke.IsStatic) arguments.Add(new Instruction(OpCodes.Ldarg_0)); //this instance

            for (var i = 0; i < argumentsToPass; i++)
            {
                var toInvokeParameter = methodToInvoke.MethodSig?.Params?.ElementAtOrDefault(i);
                var originalMethodParameter = targetMethod?.MethodSig?.Params?.ElementAtOrDefault(i);
                var isRef = toInvokeParameter?.IsByRef == true;
                //We can't have a byref of byref
                if (originalMethodParameter?.IsByRef == true && isRef)
                    isRef = false;

                IEnumerable<Instruction> ldArgInst = new[]
                    {new Instruction(isRef ? OpCodes.Ldarga : OpCodes.Ldarg, paramsMethodParams.ElementAtOrDefault(i))};

                modifyParameterHandler?.Invoke(i, ref ldArgInst, afterCallInstructions);

                arguments.AddRange(ldArgInst); //parameter
            }

            return arguments;
        }

        private static IEnumerable<Instruction> InvokeMethodAsInlined(MethodDef methodToInvoke, MethodDef targetMethod)
        {
            var methodReturnsVoid = methodToInvoke.ReturnType.FullName ==
                                    methodToInvoke.Module.CorLibTypes.Void.FullName;
            var skipLastAmount = methodReturnsVoid ? 1 : 0;

            foreach (var handler in methodToInvoke.Body.ExceptionHandlers)
                targetMethod.Body.ExceptionHandlers.Add(handler);
            foreach (var variable in methodToInvoke.Body.Variables) targetMethod.Body.Variables.Add(variable);
            foreach (var instruction in methodToInvoke.Body.Instructions.SkipLast(skipLastAmount))
                yield return instruction;
        }

        public static IEnumerable<Instruction> InvokeMethod(MixinAction action, Instruction? nextInstruction = null,
            bool discardResult = false,
            IEnumerable<Instruction>? callerArguments = null)
        {
            return InvokeMethod(action.Workspace, action.MixinMethod,
                action.MixinMethod.GetParamCount(), (int index, ref IEnumerable<Instruction> instructions,
                        List<Instruction> callInstructions) =>
                    HandleParameterInstruction(action, index, ref instructions, callInstructions, nextInstruction),
                action.TargetMethod, discardResult, callerArguments);
        }

        public static void HandleParameterInstruction(MixinAction action, int index,
            ref IEnumerable<Instruction> instruction, List<Instruction> afterCallInstructions,
            Instruction? nextInstruction)
        {
            //action.TargetMethod.Body.KeepOldMaxStack = true;
            var attribute = action.MixinMethod.ParamDefs[index].GetCustomAttribute<BaseParameterAttribute>();
            if (attribute != null)
                switch (attribute)
                {
                    case InjectCancelParamAttribute injectCancelParamAttribute:
                        HandleInjectCancelParameterAttribute(action, out instruction, afterCallInstructions, nextInstruction);
                        break;
                    default:
                        throw new MixinApplyException(
                            $"Unable to process {attribute} in parameter {index} on {action.MixinMethod}.");
                }
        }

        private static void HandleInjectCancelParameterAttribute(MixinAction action, out IEnumerable<Instruction> instruction, ICollection<Instruction> afterCallInstructions,
            Instruction? nextInstruction)
        {
            var (targetMethod, mixinMethod, _) = action;
            var isCancelledVariable =
                new Local(
                    new CorLibTypeSig(mixinMethod.Module.CorLibTypes.Boolean.TypeDefOrRef,
                        ElementType.Boolean),
                    Utilities.GenerateRandomName("isCancelled"));
            targetMethod.Body.Variables.Add(isCancelledVariable);

            instruction = new[]
            {
                new Instruction(OpCodes.Ldc_I4_0), //Load false
                new Instruction(OpCodes.Stloc, isCancelledVariable), //Set variable to false
                new Instruction(OpCodes.Ldloca, isCancelledVariable) //Load it for the method
            };


            afterCallInstructions.Add(new Instruction(OpCodes.Ldloc, isCancelledVariable));
            afterCallInstructions.Add(new Instruction(OpCodes.Brfalse,
                nextInstruction ?? throw new MixinApplyException(
                    $"Unable to find next instruction to jump to after cancelling the method {targetMethod} on {mixinMethod}")));
            afterCallInstructions.Add(new Instruction(OpCodes.Ret));
        }
    }
}