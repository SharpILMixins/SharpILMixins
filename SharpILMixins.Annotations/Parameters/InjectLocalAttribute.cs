namespace SharpILMixins.Annotations.Parameters
{
    public class InjectLocalAttribute : BaseParameterAttribute
    {
        public int Ordinal { get; }

        public InjectLocalAttribute(int ordinal)
        {
            Ordinal = ordinal;
        }
    }
}