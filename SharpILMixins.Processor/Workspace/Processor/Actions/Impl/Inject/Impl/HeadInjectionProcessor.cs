using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpILMixins.Annotations.Inject;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class HeadInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Head;

        public override IEnumerable<Instruction> GetInstructionsForAction(MixinAction action, InjectAttribute attribute,
            InjectionPoint location, Instruction? nextInstruction)
        {
            return IntermediateLanguageHelper.InvokeMethod(action, nextInstruction);
        }

        public override IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            var ctorCall = action.TargetMethod.Body.Instructions.FirstOrDefault(c =>
                c.Operand is IMethodDefOrRef methodCall && methodCall.Name == ".ctor" &&
                methodCall.DeclaringType.FullName == action.TargetMethod.DeclaringType.BaseType.FullName);

            yield return new InjectionPoint(ctorCall != null ? action.TargetMethod.Body.Instructions.IndexOf(ctorCall) + 1 : 0);
        }
    }
}