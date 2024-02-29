#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse.Vanilla.Cache;

/// <summary>The inputs to a cached vanilla analyzed rule. This determines if the
/// cached data may be reused.</summary>
/// <param name="Scope">The variables and subroutines in the scope.</param>
record struct CacheInput(VanillaScope Scope)
{
    public readonly bool CompatibleWith(CacheInput other)
    {
        return Scope.GetVariables().SequenceEqual(other.Scope.GetVariables())
            && Scope.GetSubroutines().SequenceEqual(other.Scope.GetSubroutines());
    }
}

/// <summary>Cached vanilla data.</summary>
/// <param name="Analysis">The analyzed rule.</param>
/// <param name="IdeItems">Completion and signature help that this rule adds to the file.</param>
record struct CacheItem(VanillaRuleAnalysis Analysis, CacheItems IdeItems);

/// <summary>Caches analyzed vanilla rules so that they can be reused in multiple parses.</summary>
class VanillaCache
{
    readonly Dictionary<object, VanillaCacheGroup> groups = [];
    HashSet<object> untracked = [];

    /// <summary>Finds a cache group using the provided key and input.</summary>
    public VanillaCacheGroup GetGroup(object key, CacheInput inputs)
    {
        if (groups.TryGetValue(key, out var group))
        {
            if (!group.CompatibleWith(inputs))
            {
                // Existing group is not compatible, replace it with a fresh one.
                group = new(inputs);
                groups[key] = group;
            }
        }
        else
        {
            group = new(inputs);
            groups.Add(key, group);
        }
        untracked.Remove(key);
        return group;
    }

    public void BeginTracking() => untracked = new(groups.Keys);

    public void RemoveUnused()
    {
        foreach (var removeUnused in untracked)
        {
            groups.Remove(removeUnused);
        }
    }

    public static VanillaCache Instance = new();
}

class VanillaCacheGroup
{
    readonly CacheInput inputs;
    readonly Dictionary<VanillaRule, CacheItem> cacheItems = [];

    public VanillaCacheGroup(CacheInput Inputs)
    {
        inputs = Inputs;
    }

    public bool TryGetCacheItem(VanillaRule syntax, [NotNullWhen(true)] out CacheItem? item)
    {
        if (cacheItems.TryGetValue(syntax, out var setItem))
        {
            item = setItem;
            return true;
        }
        item = null;
        return false;
    }

    public void Cache(VanillaRule syntax, CacheItem item)
    {
        cacheItems.Add(syntax, item);
    }

    public bool CompatibleWith(CacheInput inputs) => this.inputs.CompatibleWith(inputs);
}