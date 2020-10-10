using dnlib.DotNet.Emit;
using JetBrains.Annotations;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl
{
    public class OverwriteActionProcessor : BaseMixinActionProcessor<OverwriteAttribute>
    {
        public override void ProcessAction(MixinAction action, OverwriteAttribute attribute)
        {
               var body = action.TargetMethod.Body;
               body.Variables.Clear();
               body.ExceptionHandlers.Clear();
               body.Instructions.Clear();
               
               var mixinMethodBody = action.MixinMethod.Body;
               
               if (Workspace.MixinProcessor.CopyScaffoldingHandler.IsMethodInlined(action.MixinMethod))
               {
                   foreach (var handler in mixinMethodBody.ExceptionHandlers) body.ExceptionHandlers.Add(handler);
                   foreach (var variable in mixinMethodBody.Variables) body.Variables.Add(variable);
                   foreach (var instruction in mixinMethodBody.Instructions) body.Instructions.Add(instruction);
               }
               else
               {
                   var returnInstruction = new Instruction(OpCodes.Ret);
                   var methodCall = IntermediateLanguageHelper.InvokeMethod(action, returnInstruction);

                   foreach (var instruction in methodCall) body.Instructions.Add(instruction);
                   body.Instructions.Add(returnInstruction);
               }
            
        }

        public OverwriteActionProcessor([NotNull] MixinWorkspace workspace) : base(workspace)
        {
        }
    }
}