using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using dnlib.DotNet;

namespace SharpILMixins.Processor.Utils
{
    public static class CustomAttributeUtils
    {
        public static T? GetCustomAttribute<T>(this IHasCustomAttribute provider) where T : class
        {
            var attribute =
                provider.CustomAttributes.FirstOrDefault(attr =>
                {
                    var definition = attr.AttributeType.ResolveTypeDef();
                    return attr.AttributeType.FullName == typeof(T).FullName || definition?.BaseType != null && definition.BaseType.FullName == typeof(T).FullName;
                });

            if (attribute == null) return null;

            var type = typeof(T).Assembly.GetType(attribute.AttributeType.FullName);
            var constructorInfos = type?.GetConstructors();
            if (constructorInfos == null) return null;
            foreach (var constructor in constructorInfos)
            {
                try
                {
                    var values = FixValues(attribute.ConstructorArguments, i => constructor.GetParameters()[i].ParameterType).ToArray();

                    return constructor?.Invoke(values) as T;
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            return null;
        }

        private static IEnumerable<object?> FixValues(IList<CAArgument> constructorArguments, Func<int, Type> parameterType)
        {
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                var argument = constructorArguments[i];
                var obj = argument.Value;
                if (obj is TypeRef type)
                    yield return type.FullName;
                else if (obj is CorLibTypeSig corLibType)
                    yield return corLibType.FullName;
                else if (obj is ClassSig)
                    yield return obj.ToString();
                else if (parameterType(i).IsArray)
                {
                    if (obj is IList<CAArgument> iList)
                    {
                        var elementType = parameterType(i).GetElementType()!;
                        var array = Array.CreateInstance(elementType, iList.Count);
                        var fixedValues = FixValues(iList, _ => elementType).ToArray();
                        for (var index = 0; index < iList.Count; index++)
                        {
                            array.SetValue(fixedValues[index], index);
                        }

                        yield return array;
                    }
                }
                else
                {

                    yield return Cast(obj, parameterType(i));
                }
            }
        }
        public static object? Cast(object data, Type type)
        {
                var dataParam = Expression.Parameter(typeof(object), "data");
                var block = Expression.Block(Expression.Convert(Expression.Convert(dataParam, data.GetType()), type));

                var compile = Expression.Lambda(block, dataParam).Compile();
                var ret = compile.DynamicInvoke(data);
                return ret;
            
        }

    }

}