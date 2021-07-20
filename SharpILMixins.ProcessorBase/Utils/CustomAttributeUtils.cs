using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using dnlib.DotNet;

namespace SharpILMixins.Processor.Utils
{
    public static class CustomAttributeUtils
    {
        public static T? GetCustomAttribute<T>(this IHasCustomAttribute provider) where T : class
        {
            return GetCustomAttributes<T>(provider).FirstOrDefault();
        }

        public static T[] GetCustomAttributes<T>(this IHasCustomAttribute provider) where T : class
        {
            return provider.CustomAttributes.Where(attr =>
            {
                var definition = attr.AttributeType.ResolveTypeDef();
                return attr.AttributeType.FullName == typeof(T).FullName || definition?.BaseType != null &&
                    definition.BaseType.FullName == typeof(T).FullName;
            }).Select(GetCustomAttributeFromMetadata<T>).Where(c => c != null).ToArray()!;
        }

        private static T? GetCustomAttributeFromMetadata<T>(CustomAttribute attribute) where T : class
        {
            var type = typeof(T).Assembly.GetType(attribute.AttributeType.FullName);
            var constructorInfos = type?.GetConstructors();
            if (constructorInfos == null) return null;
            foreach (var constructor in constructorInfos)
                try
                {
                    var values = FixValues(attribute.ConstructorArguments,
                        i => constructor.GetParameters()[i].ParameterType).ToArray();

                    var result = constructor?.Invoke(values) as T;
                    if (result == null || !attribute.HasNamedArguments) return result;
                    {
                        var arguments = attribute.NamedArguments.Select(c => c.Argument).ToList();
                        var fixedValues = FixValues(arguments,
                            i => Type.GetType(attribute.NamedArguments[i].Type.FullName) ?? typeof(object)).ToArray();
                        var valueTypes = Enumerable.Range(0, attribute.NamedArguments.Count)
                            .Select(i => (attribute.NamedArguments[i], fixedValues[i]));

                        foreach (var (argument, value) in valueTypes)
                        {
                            var member = (MemberInfo)(argument.IsProperty
                                ? result.GetType().GetProperty(argument.Name)
                                : result.GetType().GetField(argument.Name))!;

                            switch (member)
                            {
                                case PropertyInfo prop:
                                    prop.SetValue(result, value);
                                    break;
                                case FieldInfo field:
                                    field.SetValue(result, value);
                                    break;
                            }
                        }
                    }
                    return result;
                }
                catch (Exception)
                {
                    // ignored
                }

            return null;
        }

        private static IEnumerable<object?> FixValues(IList<CAArgument> constructorArguments,
            Func<int, Type> parameterType)
        {
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                var argument = constructorArguments[i];
                var obj = argument.Value;
                switch (obj)
                {
                    case TypeRef type:
                        yield return type.FullName;
                        break;
                    case CorLibTypeSig corLibType:
                        yield return corLibType.FullName;
                        break;
                    case ClassSig:
                        yield return obj.ToString();
                        break;
                    default:
                    {
                        if (parameterType(i).IsArray)
                            foreach (var o in FixArrayValues(parameterType, obj, i))
                                yield return o;
                        else
                            yield return Cast(obj, parameterType(i));

                        break;
                    }
                }
            }
        }

        private static IEnumerable<object> FixArrayValues(Func<int, Type> parameterType, object obj, int i)
        {
            if (obj is not IList<CAArgument> iList) yield break;

            var elementType = parameterType(i).GetElementType()!;
            var array = Array.CreateInstance(elementType, iList.Count);
            var fixedValues = FixValues(iList, _ => elementType).ToArray();
            for (var index = 0; index < iList.Count; index++) array.SetValue(fixedValues[index], index);

            yield return array;
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