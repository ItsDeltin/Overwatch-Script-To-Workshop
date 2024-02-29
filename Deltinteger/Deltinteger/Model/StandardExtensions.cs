#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Deltin.Deltinteger.Model;

static class StandardExtensions
{
    /// <summary>
    /// Creates an enumerable with the index.
    /// </summary>
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

    public static T? FirstOrNull<T>(this IEnumerable<T> enumerable, Func<T, bool> selector) where T : struct
    {
        foreach (var item in enumerable)
            if (selector(item))
                return item;
        return default;
    }

    public static T? Or<T>(this T? maybe, T? otherwise)
    {
        return maybe is not null ? maybe : otherwise;
    }

    public static T Unwrap<T>(this T? nullable, string message) where T : struct =>
        nullable ?? throw new Exception(message);

    public static T Unwrap<T>(this T? nullable, string message) where T : class =>
        nullable ?? throw new Exception(message);

    public static Result<T, E> OkOr<T, E>(this T? nullable, E error) =>
        nullable is null ? error : nullable;
}