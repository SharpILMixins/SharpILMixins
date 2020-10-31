using System;
using CommandLine;

namespace SharpILMixins.Bootstrapper
{
    [Verb("bootstrap", true, HelpText = "Bootstrap an assembly")]
    internal class BootstrapperOptions
    {
        
    }
    
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}