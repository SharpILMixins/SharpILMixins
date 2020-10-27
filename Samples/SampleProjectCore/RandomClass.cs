namespace SampleProjectCore
{
    public class RandomClass
    {
        public double DoubleValue;

        public class RandomClass2 : RandomClass
        {
        }

        public interface IRandomInterface
        {
            double GetDoubleValue();
        }
    }
}