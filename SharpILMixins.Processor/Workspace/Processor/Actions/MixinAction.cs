using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Parameters;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Actions
{
    public record MixinAction
    {
        public MixinAction(MethodDef mixinMethod, BaseMixinAttribute mixinAttribute, TypeDef targetType,
            MixinWorkspace workspace)
        {
            MixinMethod = mixinMethod;
            MixinAttribute = mixinAttribute;
            TargetType = targetType;
            Workspace = workspace;
            Priority = mixinAttribute.Priority;
            GetMixinAttributeInfo();
        }

        public bool HasCancelParameter { get; set; }

        public int Priority { get; set; }

        public MethodDef TargetMethod { get; set; } = null!;

        public MethodDef MixinMethod { get; }

        public BaseMixinAttribute MixinAttribute { get; }

        public TypeDef TargetType { get; }

        public MixinWorkspace Workspace { get; }

        private void GetMixinAttributeInfo()
        {
            HasCancelParameter =
                MixinMethod.ParamDefs.Any(p => p.GetCustomAttribute<InjectCancelParamAttribute>() != null);
        }

        public void CheckIsValid()
        {
            CheckStaticMismatch();

            if (MixinMethod.ParamDefs.Count(p => p.GetCustomAttribute<InjectCancelParamAttribute>() != null) > 1)
                throw new MixinApplyException(
                    "The mixin method contains multiple parameters with the [InjectCancelParam] Attribute.");
        }

        private void CheckStaticMismatch()
        {
            if (TargetMethod.IsStatic != MixinMethod.IsStatic)
            {
                var targetIsStatic = $"is{(!TargetMethod.IsStatic ? "n't" : "")}";
                var mixinIsStatic = $"is{(!MixinMethod.IsStatic ? "n't" : "")}";

                throw new MixinApplyException(
                    $"The mixin method {mixinIsStatic} static but the target method {targetIsStatic}.");
            }
        }

        private static string GetTargetDescription(MethodDef mixinMethod, BaseMixinAttribute? mixinAttribute)
        {
            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (targetAttribute != null)
                return
                    $"{targetAttribute.ReturnType} {targetAttribute.Name}({string.Join(',', targetAttribute.ArgumentTypes)})";

            if (string.IsNullOrEmpty(mixinAttribute?.Method))
                return
                    $"{mixinMethod.ReturnType} {mixinMethod.Name}({string.Join(',', mixinMethod.GetParams().Select(c => c.FullName))})";

            return mixinAttribute.Method;
        }

        public static MethodDef? GetTargetMethod(MethodDef mixinMethod, BaseMixinAttribute? mixinAttribute,
            TypeDef targetType, MixinWorkspace workspace)
        {
            try
            {
                return GetTargetMethodThrow(mixinMethod, mixinAttribute, targetType, workspace);
            }
            catch
            {
                return null;
            }
        }

        public static MethodDef GetTargetMethodThrow(MethodDef mixinMethod, BaseMixinAttribute? mixinAttribute,
            TypeDef targetType, MixinWorkspace workspace)
        {
            var exception = new MixinApplyException(
                $"Unable to find a target method for description \"{GetTargetDescription(mixinMethod, mixinAttribute)}\"");


            var targetAttribute = mixinMethod.GetCustomAttribute<MethodTargetAttribute>();
            if (!string.IsNullOrEmpty(mixinAttribute?.Method))
            {
                var directResult = targetType.Methods.FirstOrDefault(m => m.FullName == mixinAttribute.Method) ??
                                   targetType.Methods.Single(m => m.Name == mixinAttribute.Method);
                return directResult ?? throw exception;
            }

            var redirectManager = workspace.RedirectManager;

            var returnType = targetAttribute?.ReturnType ?? mixinMethod.ReturnType.FullName;
            var name = targetAttribute?.Name ?? mixinMethod.Name;
            var argumentTypes = (targetAttribute?.ArgumentTypes ??
                                 mixinMethod.GetParams().Select(c => c.FullName)).Select(redirectManager.RedirectType)
                .ToArray();

            var result = targetType.Methods.FirstOrDefault(m =>
            {
                var returnTypeEquals = redirectManager.RedirectType(returnType) == m.ReturnType.FullName;
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
            return result ?? throw exception;
        }

        public void Deconstruct(out MethodDef targetMethod, out MethodDef mixinMethod,
            out BaseMixinAttribute mixinAttribute)
        {
            targetMethod = TargetMethod;
            mixinMethod = MixinMethod;
            mixinAttribute = MixinAttribute;
        }

        public void LocateTargetMethod()
        {
            TargetMethod = GetTargetMethodThrow(MixinMethod, MixinAttribute, TargetType, Workspace);
        }
    }
}