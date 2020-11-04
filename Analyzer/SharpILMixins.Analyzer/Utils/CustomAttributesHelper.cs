using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace SharpILMixins.Analyzer.Utils
{
    public static class CustomAttributesHelper
    {
        [DebuggerStepThrough]
        public static T? GetCustomAttribute<T>(this ISymbol provider) where T : class
        {
            return GetCustomAttributes<T>(provider).FirstOrDefault();
        }

        public static T?[] GetCustomAttributes<T>(this ISymbol provider) where T : class
        {
            return provider.GetAttributes().Where(attr =>
            {
                var definition = attr.AttributeClass;
                return definition.ToDisplayString() == typeof(T).FullName || definition?.BaseType != null &&
                    definition.BaseType.ToDisplayString() == typeof(T).FullName;
            }).Select(GetCustomAttributeFromMetadata<T>).Where(c => c != null).ToArray();
        }


        public static AttributeData? GetCustomAttributeRaw<T>(this ISymbol provider) where T : class
        {
            return GetCustomAttributesRaw<T>(provider).FirstOrDefault();
        }

        public static AttributeData?[] GetCustomAttributesRaw<T>(this ISymbol provider) where T : class
        {
            return provider.GetAttributes().Where(attr =>
            {
                var definition = attr.AttributeClass;
                return definition.ToDisplayString() == typeof(T).FullName || definition?.BaseType != null &&
                    definition.BaseType.ToDisplayString() == typeof(T).FullName;
            }).Where(c => c != null).ToArray();
        }

        private static T? GetCustomAttributeFromMetadata<T>(AttributeData attribute) where T : class
        {
            var type = typeof(T).Assembly.GetType(attribute.AttributeClass.ToDisplayString());
            var constructorInfos = type?.GetConstructors();
            if (constructorInfos == null) return null;
            foreach (var constructor in constructorInfos)
                try
                {
                    var values = FixValues(attribute.ConstructorArguments,
                        i => constructor.GetParameters()[i].ParameterType).ToArray();

                    var result = constructor?.Invoke(values) as T;
                    if (result == null || attribute.NamedArguments.Length == 0) return result;
                    {
                        var arguments = attribute.NamedArguments.Select(c => c.Value).ToList();
                        var fixedValues = FixValues(arguments,
                            i => Type.GetType(attribute.NamedArguments[i].Value.Type.Name) ?? typeof(object)).ToArray();
                        var valueTypes = Enumerable.Range(0, attribute.NamedArguments.Length)
                            .Select(i => (attribute.NamedArguments[i], fixedValues[i]));

                        foreach (var (argument, value) in valueTypes)
                        {
                            var member = (MemberInfo) result.GetType().GetProperty(argument.Key) ?? result.GetType().GetField(argument.Key);

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

        [ItemCanBeNull]
        private static IEnumerable<object> FixValues(ICollection<TypedConstant> constructorArguments,
            Func<int, Type> parameterType)
        {
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                var argument = constructorArguments.ElementAt(i);
                var obj = argument.Value;
                switch (obj)
                {
                    default:
                    {
                        if (argument.Kind == TypedConstantKind.Array)
                            foreach (var o in FixArrayValues(parameterType, argument.Values, i))
                                yield return o;
                        else
                            yield return Cast(obj, parameterType(i));

                        break;
                    }
                }
            }
        }

        private static IEnumerable<object> FixArrayValues(Func<int, Type> parameterType, ImmutableArray<TypedConstant> iList, int i)
        {

            var elementType = parameterType(i).GetElementType();
            var array = Array.CreateInstance(elementType, iList.Length);
            var fixedValues = FixValues(iList, _ => elementType).ToArray();
            for (var index = 0; index < iList.Length; index++) array.SetValue(fixedValues[index], index);

            yield return array;
        }

        [CanBeNull]
        public static object Cast(object data, Type type)
        {
            var dataParam = Expression.Parameter(typeof(object), "data");
            var block = Expression.Block(Expression.Convert(Expression.Convert(dataParam, data.GetType()), type));

            var compile = Expression.Lambda(block, dataParam).Compile();
            var ret = compile.DynamicInvoke(data);
            return ret;
        }
    
    }
}