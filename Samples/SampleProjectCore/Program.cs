using System;

namespace SampleProject
{
    public class Program
    {
        private static readonly int _coolNumber = 9;

        private static void Main(string[] args)
        {
            Console.WriteLine($"Hello World from SampleProject {_coolNumber}!");
            var val = BooleanOverload(false);
            if (val) Console.WriteLine("HEY FROM OVERLOAD");
        }

        private static bool BooleanOverload(bool overload)
        {
            return true.Equals(overload);
        }


        private static int RandomNumber()
        {
            return 4;
        }
    }
}