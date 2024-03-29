﻿using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Annotations.Parameters;
using SharpILMixins.Processor.Workspace;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Utils
{
    public static class IntermediateLanguageHelper
    {
        public delegate void ModifyParameterInstructionHandler(int argumentIndex,
            ref IEnumerable<Instruction> parameterInstructions, List<Instruction> afterCallInstructions,
            List<Instruction> beforeParamInstructions);

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

            var beforeParamInstructions = new List<Instruction>();
            var afterCallInstructions = new List<Instruction>();

            var passCallerArguments = callerArguments ??
                                      PassCallerArguments(methodToInvoke, argumentsToPass, modifyParameterHandler,
                                          targetMethod, afterCallInstructions, beforeParamInstructions);

            foreach (var instruction2 in beforeParamInstructions) yield return instruction2;

            foreach (var instruction2 in passCallerArguments) yield return instruction2;

            yield return new Instruction(OpCodes.Call, methodToInvoke);

            if (discardResult && methodToInvoke.HasReturnType)
                yield return new Instruction(OpCodes.Pop);

            foreach (var instruction in afterCallInstructions) yield return instruction;
        }

        private static IEnumerable<Instruction> PassCallerArguments(MethodDef methodToInvoke, int argumentsToPass,
            ModifyParameterInstructionHandler? modifyParameterHandler, MethodDef? targetMethod,
            List<Instruction> afterCallInstructions, List<Instruction> beforeParamInstructions)
        {
            var parametersMethod = targetMethod ?? methodToInvoke;
            var paramsMethodParams = parametersMethod.Parameters.Where(p => !p.IsHiddenThisParameter).ToArray();
            List<Instruction> arguments = new();

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
                {
                    new Instruction(isRef ? OpCodes.Ldarga : OpCodes.Ldarg, paramsMethodParams.ElementAtOrDefault(i))
                };

                modifyParameterHandler?.Invoke(i, ref ldArgInst, afterCallInstructions, beforeParamInstructions);

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
                        List<Instruction> callInstructions, List<Instruction> beforeParamInstructions) =>
                    HandleParameterInstruction(action, index, ref instructions, callInstructions, nextInstruction,
                        beforeParamInstructions),
                action.TargetMethod, discardResult, callerArguments);
        }

        public static void HandleParameterInstruction(MixinAction action, int index,
            ref IEnumerable<Instruction> instruction, List<Instruction> afterCallInstructions,
            Instruction? nextInstruction, List<Instruction> beforeParamInstructions)
        {
            var methodSigParam = action.MixinMethod.MethodSig.Params[index];
            var attribute = action.MixinMethod.ParamDefs[index].GetCustomAttribute<BaseParameterAttribute>();
            if (attribute != null)
                switch (attribute)
                {
                    case InjectReturnValueAttribute:
                        if (action.MixinAttribute is not InjectAttribute injectAttribute ||
                            injectAttribute.At != AtLocation.Return && injectAttribute.At != AtLocation.Tail)
                        {
                            throw new MixinApplyException($"Unable to use [{nameof(InjectReturnValueAttribute)}] on a non Inject method or on Injects other than Return or Tail");
                        }
                        HandleInjectReturnValueAttribute(action, out instruction, afterCallInstructions,
                            beforeParamInstructions, methodSigParam);
                        break;
                    case InjectCancelParamAttribute:
                        HandleInjectCancelParameterAttribute(action, out instruction, afterCallInstructions,
                            nextInstruction);
                        break;
                    case InjectLocalAttribute injectLocalAttribute:
                        var bodyVariables = action.TargetMethod.Body.Variables;
                        var local = GetLocalForInjectLocal(injectLocalAttribute, bodyVariables);

                        var description = injectLocalAttribute.Ordinal != null
                            ? $"ordinal {injectLocalAttribute.Ordinal}"
                            : $"name {injectLocalAttribute.Name}";

                        instruction = new[]
                        {
                            Instruction.Create(methodSigParam.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc,
                                local ??
                                throw new MixinApplyException(
                                    $"Unable to find a local in {action.TargetMethod} with {description}."))
                        };
                        break;
                    default:
                        throw new MixinApplyException(
                            $"Unable to process {attribute} in parameter {index} on {action.MixinMethod}.");
                }
        }

        private static void HandleInjectReturnValueAttribute(MixinAction action,
            out IEnumerable<Instruction> instruction, List<Instruction> afterCallInstructions,
            List<Instruction> beforeParamInstructions, TypeSig methodSigParam)
        {
            var (targetMethod, mixinMethod, _) = action;

            // Create a local variable that will hold our final result
            var parameterFinalTypeSig = methodSigParam.ToByRefSig()?.Next ?? throw new MixinApplyException($"Invalid [{nameof(InjectReturnValueAttribute)}] usage: Parameter is not ref");
            var parameterFinalType = parameterFinalTypeSig.ToTypeDefOrRef();
            var temporaryResultVariable =
                new Local(parameterFinalTypeSig, Utilities.GenerateRandomName("finalResult"));

            // Add local variable to our body
            targetMethod.Body.Variables.Add(temporaryResultVariable);

            // Cast what we have to something compatible
            beforeParamInstructions.Add(Instruction.Create(
                parameterFinalType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameterFinalType));

            // First, save our current result (current value on stack) into our variable
            beforeParamInstructions.Add(Instruction.Create(OpCodes.Stloc, temporaryResultVariable));

            // Then, pass our local variable as a reference
            instruction = new[]
            {
                Instruction.Create(OpCodes.Ldloca, temporaryResultVariable),
            };

            // Finally, after the call has been done, our variable needs to be loaded back into the stack.
            afterCallInstructions.Add(Instruction.Create(OpCodes.Ldloc, temporaryResultVariable));
            
            // Cast what we have to something compatible to return
            afterCallInstructions.Add(Instruction.Create(
                targetMethod.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, targetMethod.ReturnType.ToTypeDefOrRef()));
        }

        private static Local? GetLocalForInjectLocal(InjectLocalAttribute injectLocalAttribute, LocalList bodyVariables)
        {
            if (injectLocalAttribute.Ordinal != null)
                return bodyVariables.ElementAtOrDefault(injectLocalAttribute.Ordinal.Value);
            if (injectLocalAttribute.Name != null)
                return bodyVariables.FirstOrDefault(v => v.Name == injectLocalAttribute.Name);
            return null;
        }

        private static void HandleInjectCancelParameterAttribute(MixinAction action,
            out IEnumerable<Instruction> instruction, ICollection<Instruction> afterCallInstructions,
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


            // The method to invoke requires us to pop the result if we return something because of the inject cancel parameter attribute
            var methodToInvoke = action.MixinMethod;
            var hasInjectCancelParameter =
                methodToInvoke.ParamDefs.Any(p => p.GetCustomAttribute<InjectCancelParamAttribute>() != null);
            var popInstruction = new Instruction(OpCodes.Pop);
            var requiresPopAfterCancel = hasInjectCancelParameter && methodToInvoke.HasReturnType;

            afterCallInstructions.Add(new Instruction(OpCodes.Ldloc, isCancelledVariable));
            afterCallInstructions.Add(new Instruction(OpCodes.Brfalse,
                (requiresPopAfterCancel ? popInstruction : nextInstruction) ?? throw new MixinApplyException(
                    $"Unable to find next instruction to jump to after cancelling the method {targetMethod} on {mixinMethod}")));
            afterCallInstructions.Add(new Instruction(OpCodes.Ret));

            if (requiresPopAfterCancel)
            {
                afterCallInstructions.Add(popInstruction);
            }
        }
    }
}