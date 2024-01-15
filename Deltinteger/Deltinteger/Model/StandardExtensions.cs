#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Deltin.Deltinteger.Model;

static class StandardExtensions
{
    public static IEnumerable<KeyValuePair<T, int>> WithIndex<T>(this IEnumerable<T> enumerable) =>
        enumerable.Select((item, i) => new KeyValuePair<T, int>(item, i));

    public static bool TryGetValue<T>(this IEnumerable<T> enumerable, Func<T, bool> selector, [NotNullWhen(true)] out T? value)
    {
        foreach (var item in enumerable)
        {
            if (item is not null && selector(item))
            {
                value = item;
                return true;
            }
        }
        value = default;
        return false;
    }
}