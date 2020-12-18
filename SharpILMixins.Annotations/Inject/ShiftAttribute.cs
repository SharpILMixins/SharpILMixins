using System;

namespace SharpILMixins.Annotations.Inject
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ShiftAttribute : Attribute
    {
        public ShiftAttribute()
        {
        }

        public ShiftAttribute(int byAmount)
        {
            ByAmount = byAmount;
        }

        public Shift Shift { get; set; }

        public int ByAmount { get; set; }
    }
}