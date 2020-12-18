namespace SampleProjectCore
{
    public class StringWrapper
    {
        public StringWrapper(string epic)
        {
            Epic = epic;
        }

        public string Epic { get; }

        public override string ToString()
        {
            return Epic;
        }
    }
}