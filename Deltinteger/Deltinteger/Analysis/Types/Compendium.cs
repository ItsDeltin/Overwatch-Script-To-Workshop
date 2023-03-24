using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Types
{
    class Compendium
    {
        readonly Dictionary<int, InternalCompendiumItem> types = new Dictionary<int, InternalCompendiumItem>();


        public CompendiumItem GetType(int hash, Func<CodeType> factory)
        {
            CodeType result;

            // Matching type exists.
            if (types.TryGetValue(hash, out InternalCompendiumItem? compendium))
                result = compendium.Value;
            // Matching type does not exist, create instance.
            else
            {
                result = factory();
                types.Add(hash, compendium = new InternalCompendiumItem(this, hash, result));
            }

            return new CompendiumItem(compendium.CreateReference(), result);
        }


        class InternalCompendiumItem
        {
            public CodeType Value { get; }

            readonly Compendium compendium;
            readonly int hash;

            int referenceCount;

            public InternalCompendiumItem(Compendium compendium, int hash, CodeType value)
            {
                Value = value;
                this.hash = hash;
                this.compendium = compendium;
            }


            public IDisposable CreateReference()
            {
                ++referenceCount;
                return Disposable.Create(() =>
                {
                    if (--referenceCount == 0)
                        compendium.types.Remove(hash);
                });
            }
        }
    }

    struct CompendiumItem
    {
        public readonly IDisposable RemoveReference;
        public readonly CodeType Value;

        public CompendiumItem(IDisposable removeReference, CodeType value)
        {
            RemoveReference = removeReference;
            Value = value;
        }
    }
}