using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;

namespace SampleProject
{
    public class Program
    {
        private static int _coolNumber = 9;

        static void Main(string[] args)
        {
            Console.WriteLine($"Hello World from SampleProject {_coolNumber}!");
            var val = BooleanOverload(false);
            if (val)
            {
                Console.WriteLine("HEY FROM OVERLOAD");
            }
        }

        static bool BooleanOverload(bool overload)
        {
            return true.Equals(overload);
        }


        static int RandomNumber()
        {
            return 4;
        }
    }
}

