using System;
using JetBrains.Annotations;

namespace SharpILMixins.Annotations
{
    [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MixinAttribute : Attribute
    {
        public MixinAttribute(Type target)
        {
            Target = target.FullName;
        }

        public MixinAttribute(string target)
        {
            Target = target;
        }

        public string Target { get; }
    }
}