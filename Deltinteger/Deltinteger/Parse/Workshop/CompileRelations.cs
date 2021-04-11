using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Workshop
{
    public class CompileRelations
    {
        readonly List<IMethodProvider> _allFunctions = new List<IMethodProvider>();
        readonly List<IClassInitializer> _allClasses = new List<IClassInitializer>();

        readonly Dictionary<IMethod, LinkedFunctions> _functionRelations = new Dictionary<IMethod, LinkedFunctions>();
        readonly Dictionary<ClassType, LinkedClasses> _classRelations = new Dictionary<ClassType, LinkedClasses>();

        public CompileRelations()
        {
            CompileFunctions();
            CompileClasses();
        }

        void CompileFunctions()
        {
            foreach (var function in _allFunctions)
            {
                // Function does not override anything.
                if (function.Overriding == null) return;

                // Get the LinkedFunctions for the overriden function.
                if (!_functionRelations.TryGetValue(function.Overriding, out LinkedFunctions links))
                {
                    links = new LinkedFunctions(new HashSet<IMethodProvider>());
                    _functionRelations.Add(function.Overriding, links);
                }

                links.OverridenBy.Add(function);
            }
        }

        void CompileClasses()
        {
            foreach (var classType in _allClasses)
            {
                // Class does not extend anything.
                if (classType.Extends == null) return;

                // Get the LinkedClasses for the extended class.
                if (!_classRelations.TryGetValue(classType.Extends, out LinkedClasses links))
                {
                    links = new LinkedClasses(new HashSet<IClassInitializer>());
                    _classRelations.Add(classType.Extends, links);
                }

                links.ExtendedBy.Add(classType);
            }
        }

        public IMethodProvider[] GetOverridersOf(IMethodExtensions methodExtensions)
        {
            foreach (KeyValuePair<IMethod, LinkedFunctions> relation in _functionRelations)
                // MethodInfo and the methodExtensions will have the same object reference.
                // The IMethodExtensions will typically be an IMethodProvider.
                if (Object.ReferenceEquals(relation.Key.MethodInfo, methodExtensions))
                    return relation.Value.OverridenBy.ToArray();

            return new IMethodProvider[0];
        }

        public IClassInitializer[] GetExtendersOf(ClassType type)
        {
            foreach (KeyValuePair<ClassType, LinkedClasses> relation in _classRelations)
                // It's easier to compare for classes, since CodeTypes have the 'Is' function for checking identicality.
                if (type.Is(relation.Key))
                    return relation.Value.ExtendedBy.ToArray();
            
            return new IClassInitializer[0];
        }

        public IClassInitializer[] GetAllExtendersOf(ClassType type)
        {
            // Get the relation with the matching provider.
            foreach (KeyValuePair<ClassType, LinkedClasses> relation in _classRelations)
                if (type.Provider == relation.Key.Provider)
                {
                    // The list of extenders.
                    var extenders = new HashSet<IClassInitializer>();

                    // Recursively get extending types.
                    foreach (var add in relation.Value.ExtendedBy)
                        GetAllExtendersOfInternal(extenders, add);
                    
                    // Done
                    return extenders.ToArray();
                }
            
            return new IClassInitializer[0];
        }

        void GetAllExtendersOfInternal(HashSet<IClassInitializer> extenders, IClassInitializer current)
        {
            extenders.Add(current);

            // Current is extended by classes.
            var children = _classRelations.FirstOrDefault(kvp => kvp.Key.Provider == current).Value.ExtendedBy;

            if (children != null)
                foreach (var child in children)
                    GetAllExtendersOfInternal(extenders, child);
        }

        private struct LinkedFunctions
        {
            public HashSet<IMethodProvider> OverridenBy;
            public LinkedFunctions(HashSet<IMethodProvider> overridenBy)
            {
                OverridenBy = overridenBy;
            }
        }

        private struct LinkedClasses
        {
            public HashSet<IClassInitializer> ExtendedBy;
            public LinkedClasses(HashSet<IClassInitializer> extendedBy)
            {
                ExtendedBy = extendedBy;
            }
        }
    }
}