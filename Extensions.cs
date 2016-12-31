using System;
using System.Collections.Generic;
using System.Linq;

namespace generate_to_assembly
{
    static class StringExtensions
    {
        public static string TryConcat(this string stringValue, string append)
        {
            return stringValue == null ? null : string.Concat(stringValue, append);
        }
    }

    static class ArrayExtensions
    {
        public static string Get(this string[] args, int index)
        {
            return (args == null || (args.Length < index + 1)) ? null : args[index];
        }
    }

    static class EnumerableExtensions
    {
        public static string JoinToString<T>(this IEnumerable<T> list, string separator)
        {
            return string.Join<T>(separator, list);
        }

        public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            return list.Where(item => !predicate(item));
        }
    }
}
