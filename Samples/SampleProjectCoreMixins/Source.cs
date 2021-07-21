using System;

namespace SampleProjectCore.Mixins
{
    public class Source
    {

        public Source()
        {
        }

        public void SendMessage(string message)
        {
            Console.WriteLine(message);
        }
    }}