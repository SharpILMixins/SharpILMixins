using System.Collections.Generic;

namespace SampleProjectCore
{
    public class YieldReturnClass
    {
        public static IEnumerable<object> GetSomething()
        {
            yield return "something";
            yield return new object();
            yield return 12;
        }
    }
}