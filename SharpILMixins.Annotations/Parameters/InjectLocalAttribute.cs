namespace SharpILMixins.Annotations.Parameters
{
    public class InjectLocalAttribute : BaseParameterAttribute
    {
        public InjectLocalAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }

        public int Ordinal { get; }
    }
}