namespace SharpILMixins.Processor.Workspace.Processor.Actions.Impl.Inject
{
    public record InjectionPoint(int BeforePoint, int AfterPoint)
    {
        public InjectionPoint(int point) : this(point, point + 1)
        {
        }

        public static InjectionPoint operator +(InjectionPoint self, int value)
        {
            return self with {BeforePoint = self.BeforePoint + value, AfterPoint = self.AfterPoint + value};
        }
    }
}