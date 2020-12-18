using JetBrains.Annotations;

namespace SharpILMixins.Annotations.Parameters
{
    public class InjectLocalAttribute : BaseParameterAttribute
    {
        public InjectLocalAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }

        public InjectLocalAttribute(string name)
        {
            Name = name;
        }

        public int? Ordinal { get; }

        [CanBeNull] public string Name { get; set; }
    }
}