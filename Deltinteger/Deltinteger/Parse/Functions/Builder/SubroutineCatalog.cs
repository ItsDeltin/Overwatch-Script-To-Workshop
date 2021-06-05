using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class SubroutineCatalog
    {
        readonly Dictionary<object, SubroutineCatalogItem> _subroutines = new Dictionary<object, SubroutineCatalogItem>();
        
        // Gets a subroutine for the specified key. If the key has no subroutine, it will be created.
        public SubroutineCatalogItem GetSubroutine(object key, Func<SetupSubroutine> create)
        {
            // Get the subroutine.
            if (!_subroutines.TryGetValue(key, out SubroutineCatalogItem info))
            {
                // No subroutine found, create and add to dictionary.
                var setup = create();
                info = setup.Item;
                _subroutines.Add(key, info);
                setup.CompleteSetup();
            }
            return info;
        }
    }

    public struct SetupSubroutine
    {
        public SubroutineCatalogItem Item;
        public Action CompleteSetup;

        public SetupSubroutine(SubroutineBuilder builder)
        {
            Item = builder.Initiate();
            CompleteSetup = builder.Complete;
        }
    }

    public class SubroutineCatalogItem
    {
        public Subroutine Subroutine { get; } // The subroutine's workshop item.
        public IParameterHandler ParameterHandler { get; } // The subroutine's parameter handlers.
        public IndexReference ObjectStack { get; } // Stores data about the subroutine's object instances. Will be a stack if recursive, a singular value otherwise.
        public ReturnHandler ReturnHandler { get; } // The subroutine's return handler.

        public SubroutineCatalogItem(Subroutine subroutine, IParameterHandler parameterHandler, IndexReference objectStack, ReturnHandler returnHandler)
        {
            Subroutine = subroutine;
            ParameterHandler = parameterHandler;
            ObjectStack = objectStack;
            ReturnHandler = returnHandler;
        }

        public static implicit operator bool(SubroutineCatalogItem item) => item != null; 
        public static bool operator true(SubroutineCatalogItem item) => item != null;
        public static bool operator false(SubroutineCatalogItem item) => item == null;
        public static bool operator !(SubroutineCatalogItem item) => item == null;
    }
}