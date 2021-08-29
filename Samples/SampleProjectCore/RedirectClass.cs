using System;

namespace SampleProjectCore
{
    public class RedirectClass
    {
        private StringWrapper e10;

        private double _d;

        private void Example()
        {
            // `RedirectPrintln` should target here
            // `RedirectStringWrapperCtor` should target here
            Console.WriteLine(new StringWrapper("EPIC"));
            
            // `RedirectString` should target here
            Console.WriteLine("EPIC");

            // `RedirectFieldGet` should target here
            // `RedirectFieldSet` should target here
            _d += 0.01;

            if (Math.Abs(new Random().Next() - _d) < double.Epsilon) return;
        }
    }
}