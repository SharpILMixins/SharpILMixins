namespace SharpILMixins.Annotations
{
    public class InlineMethodAttribute : MethodInlineOptionAttribute
    {
        public InlineMethodAttribute() : base(InlineSetting.DoInline)
        {
        }
    }

    public class NoInlineAttribute : MethodInlineOptionAttribute
    {
        public NoInlineAttribute() : base(InlineSetting.NoInline)
        {
        }
    }
}