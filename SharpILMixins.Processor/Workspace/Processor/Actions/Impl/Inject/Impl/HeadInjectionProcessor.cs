using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Annotations.Inject;

namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject.Impl
{
    public class HeadInjectionProcessor : BaseInjectionProcessor
    {
        public override AtLocation Location => AtLocation.Head;

        public override IEnumerable<InjectionPoint> FindInjectionPoints(MixinAction action, InjectAttribute attribute)
        {
            var ctorCall = action.TargetMethod.Body.Instructions.FirstOrDefault(c =>
                c.Operand is IMethodDefOrRef methodCall && methodCall.Name == ".ctor" &&
                methodCall.DeclaringType.FullName == action.TargetMethod.DeclaringType.BaseType.FullName);

            yield return new InjectionPoint(ctorCall != null
                ? action.TargetMethod.Body.Instructions.IndexOf(ctorCall) + 1
                : 0);
        }
    }
}