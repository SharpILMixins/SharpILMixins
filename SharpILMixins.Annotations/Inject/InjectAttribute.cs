using System;

namespace SharpILMixins.Annotations.Inject
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectAttribute : BaseMixinAttribute
    {
        public InjectAttribute(AtLocation at) : this(string.Empty, at)
        {
        }

        public InjectAttribute(AtLocation at, int priority = 1000) : this(string.Empty, at, priority)
        {
        }

        public InjectAttribute(string target, AtLocation at) : base(target)
        {
            At = at;
        }

        public InjectAttribute(string target, AtLocation at, int priority = 1000) : base(target, priority)
        {
            At = at;
        }

        public AtLocation At { get; set; }
    }
}