using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AccessorAttribute : MixinAttribute
    {
        public AccessorAttribute(Type target) : base(target)
        {
        }

        public AccessorAttribute(string target) : base(target)
        {
        }
    }
}