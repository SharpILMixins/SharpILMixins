using System;

namespace SharpILMixins.Annotations.Inject
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectAttribute : BaseMixinAttribute
    {
        public InjectAttribute(AtLocation at, int shift = 0) : this(string.Empty, at, shift)
        {
        }

        public InjectAttribute(AtLocation at, int priority = 1000, int shift = 0) : this(string.Empty, at, shift, priority)
        {
        }

        public InjectAttribute(string target, AtLocation at, int shift = 0) : base(target)
        {
            At = at;
            Shift = shift;
        }

        public InjectAttribute(string target, AtLocation at, int priority = 1000, int ordinal = -1, int shift = 0) : base(target, priority)
        {
            At = at;
            Ordinal = ordinal;
            Shift = shift;
        }

        public AtLocation At { get; }

        public int Ordinal { get; }

        public int Shift { get; }
    }
}