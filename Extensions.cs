using System.Collections.Generic;
using System.Linq;
using System;

namespace LifeSpace
{
    public static class Extensions
    {
        public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default(T);
            rest = list.Skip(1).ToList();
        }

        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default(T);
            second = list.Count > 1 ? list[1] : default(T);
            rest = list.Skip(2).ToList();
        }

        public static string ToIsoDate(this DateTime t) => t.ToString("yyyy-MM-dd");
    }
}