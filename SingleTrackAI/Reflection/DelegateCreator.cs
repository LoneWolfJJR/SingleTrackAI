// Copied from TMPE mod
// Copyright 2015 Svetlozar
// MIT License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SingleTrackAI.Reflection
{
    public static class DelegateCreator
    {
        public static TDelegate CreateDelegate<TDelegate>(Type type, string name, bool instance)
            where TDelegate : Delegate
        {
            try
            {
                var types  = GetParameterTypes<TDelegate>(instance);
                var method = type.GetMethod(
                    name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    types,
                    new ParameterModifier[0]);

                if (method == null)
                    throw new InvalidOperationException($"failed to retrieve method {type}.{name}({types.ToSTR()})");

                return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), method);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"error creating delegate for {type}.{name}");
                throw;
            }
        }

        /// <summary>
        /// Gets parameter types from delegate
        /// </summary>
        /// <typeparam name="TDelegate">delegate type</typeparam>
        /// <param name="instance">skip first parameter. Default value is false.</param>
        /// <returns>Type[] representing arguments of the delegate.</returns>
        private static Type[] GetParameterTypes<TDelegate>(bool instance = false) where TDelegate : Delegate
        {
            IEnumerable<ParameterInfo> parameters = typeof(TDelegate).GetMethod("Invoke").GetParameters();
            if (instance)
            {
                parameters = parameters.Skip(1);
            }

            return parameters.Select(p => p.ParameterType).ToArray();
        }

        /// <summary>
        /// Creates and string of all items with enumerable inpute as {item1, item2, item3}
        /// null argument returns "Null".
        /// </summary>
        private static string ToSTR<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return "Null";
            string ret = "{ ";
            foreach (T item in enumerable)
            {
                ret += $"{item}, ";
            }
            ret.Remove(ret.Length - 2, 2);
            ret += " }";
            return ret;
        }
    }
}
