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

    public static T? ElementAtOrNull<T>(this IList<T> list, int index)
    {
        if (list.Count <= index)
            return list[index];
        return default;
    }

    public static IEnumerable<U> SelectWithoutNull<T, U>(this IEnumerable<T> enumerable, Func<T, U?> selector) where T : class
    {
        foreach (var item in enumerable)
        {
            var selected = selector(item);
            if (selected is not null)
                yield return selected;
        }
    }

    public static T? Or<T>(this T? maybe, T? otherwise)
    {
        return maybe is not null ? maybe : otherwise;
    }

    public static T Unwrap<T>(this T? nullable, string message) where T : struct =>
        nullable ?? throw new Exception(message);

    public static T Unwrap<T>(this T? nullable, string message) where T : class =>
        nullable ?? throw new Exception(message);

    public static Result<T, E> OkOr<T, E>(this T? nullable, E error) where T : struct =>
        nullable is null ? error : nullable.Value;

    public static Result<T, E> OkOr<T, E>(this T? nullable, E error) where T : class =>
        nullable is null ? error : nullable;

    public static U? Map<T, U>(this T? nullable, Func<T, U> map) =>
        nullable is null ? default : map(nullable);

    public static (T, U)? Zip<T, U>(this T? nullable, U? other) => nullable is null || other is null ? default : (nullable, other);
}