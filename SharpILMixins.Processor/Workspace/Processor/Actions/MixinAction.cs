using System.Linq;
using dnlib.DotNet;
using JetBrains.Annotations;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public record MixinAction
    {
        public MixinAction(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute, TypeDef targetType)
        {
            MixinMethod = mixinMethod;
            MixinAttribute = mixinAttribute;
            TargetMethod = GetTargetMethod(mixinMethod, mixinAttribute, targetType) ??
                           throw new MixinApplyException(
                               $"Unable to find a target method for description \"{GetTarget(mixinMethod, mixinAttribute)}\"");
            Priority = mixinAttribute.Priority;
        }

        private static string GetTarget(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute)
        {
            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (targetAttribute != null)
            {
                return $"{targetAttribute.ReturnType} {targetAttribute.Name}({string.Join(',', targetAttribute.ArgumentTypes)})";
            }

            return mixinAttribute.Target;
        }

        public static MethodDef? GetTargetMethod(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute,
            TypeDef targetType)
        {
            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (targetAttribute != null)
            {
                return targetType.Methods.FirstOrDefault(m =>
                    targetAttribute.ReturnType == m.ReturnType.FullName && targetAttribute.Name == m.Name && Enumerable
                        .Range(0, m.GetParamCount()).All(i =>
                            m.GetParams().ElementAtOrDefault(i)?.FullName == targetAttribute.ArgumentTypes.ElementAtOrDefault(i)));
            }

            return targetType.Methods.FirstOrDefault(m => m.FullName == mixinAttribute.Target);
        }

        public int Priority { get; set; }

        public MethodDef TargetMethod { get; set; }

        public MethodDef MixinMethod { get; }

        public BaseMixinAttribute MixinAttribute { get; }

        public void Deconstruct(out MethodDef targetMethod, out MethodDef mixinMethod,
            out BaseMixinAttribute mixinAttribute)
        {
            targetMethod = TargetMethod;
            mixinMethod = MixinMethod;
            mixinAttribute = MixinAttribute;
        }
    }
}