using System;

namespace SharpILMixins.Annotations.Inject
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectAttribute : BaseMixinAttribute
    {
        public InjectAttribute()
        {
            
        }

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

        public InjectAttribute(string target, AtLocation at, int priority = 1000, int ordinal = -1) : base(target, priority)
        {
            At = at;
            Ordinal = ordinal;
        }

        public AtLocation At { get; set; }

        public int Ordinal { get; set; }

        public int ShiftBy
        {
            get => ShiftByAmount;
            set => ShiftByAmount = value;
        }

        public Shift Shift { get; set; }

        public int ShiftByAmount { get; set; }
    }
}