using System;
using SampleProjectFramework;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inject;

namespace SampleProjectFrameworkMixins
{
    [Mixin(typeof(Program))]
    public class ProgramMixin
    {
        [Inject(AtLocation.Head)]
        [Unique]
        public static void Main(MyCoolEnum value, string[] args)
        {
            Console.Out.WriteLine("value = {0}", value);
        }
    }
}
