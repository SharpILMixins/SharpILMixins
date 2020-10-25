using System;
using System.Diagnostics.CodeAnalysis;

namespace SampleProject
{
    public class Program
    {
        private static int _coolNumber = 9;

        static void Main(string[] args)
        {
            Console.WriteLine($"Hello World from SampleProject {_coolNumber}!");
        }

        static int RandomNumber()
        {
            return 4;
        }
    }
}

