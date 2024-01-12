#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Model;

static class StandardExtensions
{
    public static IEnumerable<KeyValuePair<T, int>> WithIndex<T>(this IEnumerable<T> enumerable) =>
        enumerable.Select((item, i) => new KeyValuePair<T, int>(item, i));
}