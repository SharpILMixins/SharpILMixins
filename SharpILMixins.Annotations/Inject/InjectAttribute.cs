using System;
using JetBrains.Annotations;

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

        public InjectAttribute(string method, AtLocation at) : base(method)
        {
            At = at;
        }

        public InjectAttribute(string method, AtLocation at, int priority = 1000, int ordinal = -1) : base(method, priority)
        {
            At = at;
            Ordinal = ordinal;
        }

        public AtLocation At { get; set; }

        public int Ordinal { get; set; } = -1;

        public int ShiftBy
        {
            get => ShiftByAmount;
            set => ShiftByAmount = value;
        }
        
        [CanBeNull] public string Target { get; set; }

        public Shift Shift { get; set; }

        public int ShiftByAmount { get; set; }
    }
}