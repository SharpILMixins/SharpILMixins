using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public record MixinAction
    {
        public MixinAction(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute, TypeDef targetType, MixinWorkspace workspace)
        {
            MixinMethod = mixinMethod;
            MixinAttribute = mixinAttribute;
            Workspace = workspace;
            TargetMethod = GetTargetMethod(mixinMethod, mixinAttribute, targetType) ??
                           throw new MixinApplyException(
                               $"Unable to find a target method for description \"{GetTargetDescription(mixinMethod, mixinAttribute)}\"");
            Priority = mixinAttribute.Priority;
        }

        private static string GetTargetDescription(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute)
        {
            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (targetAttribute != null)
            {
                return
                    $"{targetAttribute.ReturnType} {targetAttribute.Name}({string.Join(',', targetAttribute.ArgumentTypes)})";
            }

            if (string.IsNullOrEmpty(mixinAttribute.Target))
            {
                return
                    $"{mixinMethod.ReturnType} {mixinMethod.Name}({string.Join(',', mixinMethod.GetParams().Select(c => c.FullName))})";
            }

            return mixinAttribute.Target;
        }

        public static MethodDef? GetTargetMethod(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute,
            TypeDef targetType)
        {
            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (!string.IsNullOrEmpty(mixinAttribute.Target))
                return targetType.Methods.FirstOrDefault(m => m.FullName == mixinAttribute.Target);

            var returnType = targetAttribute?.ReturnType ?? mixinMethod.ReturnType.FullName;
            var name = targetAttribute?.Name ?? mixinMethod.Name;
            var argumentTypes = targetAttribute?.ArgumentTypes ??
                                mixinMethod.GetParams().Select(c => c.FullName).ToArray();

            return targetType.Methods.FirstOrDefault(m =>
            {
                var returnTypeEquals = returnType == m.ReturnType.FullName;
                var nameEquals = name == m.Name;
                var paramCountEquals = m.GetParamCount() == argumentTypes.Length;

                var paramTypesEquals = Enumerable
                    .Range(0, m.GetParamCount())
                    .All(
                        i => m.GetParams().ElementAtOrDefault(i)?.FullName ==
                             argumentTypes.ElementAtOrDefault(i)
                    );

                return returnTypeEquals && nameEquals &&
                       paramCountEquals && paramTypesEquals;
            });
        }

        public int Priority { get; set; }

        public MethodDef TargetMethod { get; set; }

        public MethodDef MixinMethod { get; }

        public BaseMixinAttribute MixinAttribute { get; }
        public MixinWorkspace Workspace { get; }

        public void Deconstruct(out MethodDef targetMethod, out MethodDef mixinMethod,
            out BaseMixinAttribute mixinAttribute)
        {
            targetMethod = TargetMethod;
            mixinMethod = MixinMethod;
            mixinAttribute = MixinAttribute;
        }
    }
}