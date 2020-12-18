using System;

namespace SampleProjectCore
{
    public class RedirectClass
    {
        private double _d;

        private void Example()
        {
            // `RedirectPrintln` should target here
            // `RedirectStringWrapperCtor` should target here
            Console.WriteLine(new StringWrapper("EPIC"));

            // `RedirectFieldGet` should target here
            // `RedirectFieldSet` should target here
            _d += 0.01;

            // `RedirectRandom` should target here
            // `RedirectFieldGet` should target here
            if (Math.Abs(new Random().Next() - _d) < double.Epsilon) return;
        }
    }
}