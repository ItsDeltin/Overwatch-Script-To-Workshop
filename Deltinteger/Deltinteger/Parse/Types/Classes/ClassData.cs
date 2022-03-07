using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ClassData : IComponent
    {
        public const string ObjectVariableTag = "_objectVariable_";
        public const string ClassIndexesTag = "_classIndexes";
        public IndexReference ClassIndexes { get; private set; }

        public void Init(DeltinScript deltinScript)
        {
            ClassIndexes = deltinScript.VarCollection.Assign(ClassIndexesTag, true, false);
            deltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(0, null, Constants.MAX_ARRAY_LENGTH));
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
            actionSet.AddAction(classReference.SetVariable(
                Element.IndexOfArrayValue(ClassIndexes.Get(), 0)
            ));
            actionSet.AddAction(ClassIndexes.SetVariable(
                classIdentifier,
                null,
                (Element)classReference.GetVariable()
            ));
        }
    }
}