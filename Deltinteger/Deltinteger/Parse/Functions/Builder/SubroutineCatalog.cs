using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    // todo: switch to IWorkshopComponent
    // 
    class SubroutineCatalog : IComponent
    {
        readonly Dictionary<object, SubroutineCatalogItem> _subroutines = new Dictionary<object, SubroutineCatalogItem>();
        
        // Gets a subroutine for the specified key. If the key has no subroutine, it will be created.
        public SubroutineCatalogItem GetSubroutine(object key, Func<SubroutineCatalogItem> create)
        {
            // Get the subroutine.
            if (!_subroutines.TryGetValue(key, out SubroutineCatalogItem info))
            {
                // No subroutine found, create and add to dictionary.
                info = create();
                _subroutines.Add(key, info);
            }
            return info;
        }

        public void Init(DeltinScript deltinScript) {}
    }

    public class SubroutineCatalogItem
    {
        public Subroutine Subroutine { get; } // The subroutine's workshop item.
        public IParameterHandler[] Parameters { get; } // The subroutine's parameter handlers.
        public IndexReference ObjectStack { get; } // Stores data about the subroutine's object instances. Will be a stack if recursive, a singular value otherwise.
        public ReturnHandler ReturnHandler { get; } // The subroutine's return handler.

        public SubroutineCatalogItem(Subroutine subroutine, IParameterHandler[] parameters, IndexReference objectStack, ReturnHandler returnHandler)
        {
            Subroutine = subroutine;
            Parameters = parameters;
            ObjectStack = objectStack;
            ReturnHandler = returnHandler;
        }

        public static implicit operator bool(SubroutineCatalogItem item) => item != null; 
        public static bool operator true(SubroutineCatalogItem item) => item != null;
        public static bool operator false(SubroutineCatalogItem item) => item == null;
        public static bool operator !(SubroutineCatalogItem item) => item == null;
    }
}