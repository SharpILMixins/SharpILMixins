using System;

namespace SharpILMixins.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class MethodTargetAttribute : Attribute
    {
        public string ReturnType { get; }
        public string Name { get; }
        public string[] ArgumentTypes { get; }

        public MethodTargetAttribute(Type returnType, string name) : this(returnType, name, new Type[] { })
        {
        }

        public MethodTargetAttribute(string returnType, string name) : this(returnType, name, new string[] { })
        {
        }

        public MethodTargetAttribute(Type returnType, string name, Type[] argumentTypes)
        {
            ReturnType = returnType.FullName;
            Name = name;
            
            var argumentTypesArray = new string[argumentTypes.Length];
            for (var index = 0; index < argumentTypes.Length; index++)
            {
                argumentTypesArray[index] = argumentTypes[index].FullName;
            }

            ArgumentTypes = argumentTypesArray;
        }

        public MethodTargetAttribute(string returnType, string name, string[] argumentTypes)
        {
            ReturnType = returnType;
            Name = name;
            ArgumentTypes = argumentTypes;
        }
    }
}