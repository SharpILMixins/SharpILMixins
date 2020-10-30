using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class OverwriteAttribute : BaseMixinAttribute
    {
        public OverwriteAttribute(int priority = 1000) : this(string.Empty, priority)
        {
        }

        public OverwriteAttribute(string method, int priority = 1000) : base(method, priority)
        {
        }
    }
}