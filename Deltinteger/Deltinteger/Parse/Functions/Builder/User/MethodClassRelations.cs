using System.Linq;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse.Functions.Builder.User
{
    class MethodClassRelations
    {
        /// <summary>Original method that this MethodClassRelations was created from.</summary>
        public DefinedMethodInstance Method { get; }

        /// <summary>The methods that override Method.</summary>
        public DefinedMethodInstance[] Overriders { get; }

        /// <summary>The relation of the class that Method was defined in.</summary>
        public ClassWorkshopRelation ClassRelation { get; }

        public MethodClassRelations(ToWorkshop toWorkshop, DefinedMethodInstance method)
        {
            Method = method;
            
            if (method.DefinedInType != null)
            {
                // Get the class relation.
                ClassRelation = toWorkshop.ClassInitializer.RelationFromClassType((ClassType)method.DefinedInType);

                // Extract the virtual functions.
                // todo: In _allVirtualOptions, it may be a good idea to include classes that do not override so we don't need to check auto-implementations in the virtual builder.
                Overriders = ClassRelation.ExtractOverridenElements<DefinedMethodInstance>(extender => DoesOverride(method, extender))
                    .ToArray();
            }
        }

        static bool DoesOverride(DefinedMethodInstance target, DefinedMethodInstance overrider)
        {
            while (overrider != null)
            {
                if (overrider.Provider == target.Provider) return true;
                overrider = overrider.Provider.OverridingFunction;
            }
            return false;
        }
    }
}