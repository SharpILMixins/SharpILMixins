namespace SharpILMixins.Annotations.Inline
{
    public class NoInlineAttribute : MethodInlineOptionAttribute
    {
        public NoInlineAttribute() : base(InlineSetting.NoInline)
        {
        }
    }
}