using System;

namespace SharpILMixins.Processor.Workspace.Generator
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class IteratorStateMachineAttribute : Attribute
    {
        public IteratorStateMachineAttribute(string stateMachineType)
        {
            StateMachineType = stateMachineType;
        }

        public string StateMachineType { get; }
    }
}