using System;
using JetBrains.Annotations;

namespace SharpILMixins.Annotations
{
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MixinAttribute : Attribute
    {
        public string Target { get; }

        public MixinAttribute(Type target)
        {
            Target = target.FullName;
        }

        public MixinAttribute(string target)
        {
            Target = target;
        }
    }

}
