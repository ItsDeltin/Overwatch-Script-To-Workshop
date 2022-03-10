using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    using static Element;

    public class ClassData : IComponent
    {
        public const string ObjectVariableTag = "_objectVariable_";
        public const string ClassIndexesTag = "_classIndexes";
        public IndexReference ClassIndexes { get; private set; }

        public void Init(DeltinScript deltinScript)
        {
            ClassIndexes = deltinScript.VarCollection.Assign(ClassIndexesTag, true, false);
            // Prematurely extends indexes array
            // deltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(0, null, Constants.MAX_ARRAY_LENGTH));
            deltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(-1, index: 0));
        }

        public IndexReference CreateObject(int classIdentifier, ActionSet actionSet, string internalName)
        {
            var classReference = actionSet.VarCollection.Assign(internalName, actionSet.IsGlobal, true);
            GetClassIndex(classIdentifier, classReference, actionSet);
            return classReference;
        }

        public void GetClassIndex(int classIdentifier, IndexReference classReference, ActionSet actionSet)
        {
            // Assign the class reference to the first available slot.
            classReference.Set(actionSet, IndexOfArrayValue(ClassIndexes.Get(), 0));
            // If the class reference is -1, set it to the length of the classIndexes array.
            classReference.Set(actionSet, TernaryConditional(
                Compare(classReference.Get(), Operator.Equal, Num(-1)),
                CountOf(ClassIndexes.Get()),
                classReference.Get()
            ));
            // Register the class.
            ClassIndexes.Set(actionSet, classIdentifier, index: classReference.Get());
        }
    }
}