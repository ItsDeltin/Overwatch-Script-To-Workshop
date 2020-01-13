using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ClassData
    {
        public IndexReference ClassIndexes { get; }
        public IndexReference ClassArray { get; }

        public ClassData(VarCollection varCollection)
        {
            ClassArray = varCollection.Assign("_classArray", true, false);
            if (DefinedType.CLASS_INDEX_WORKAROUND)
                ClassIndexes = varCollection.Assign("_classIndexes", true, false);
        }

        public ClassObjectResult CreateObject(ActionSet actionSet, string internalName)
        {
            var classReference = actionSet.VarCollection.Assign(internalName, actionSet.IsGlobal, true);
            DefinedType.GetClassIndex(classReference, actionSet, this);
            var classObject = ClassArray.CreateChild((Element)classReference.GetVariable());

            return new ClassObjectResult(classReference, classObject);
        }
    }

    public class ClassObjectResult
    {
        public IndexReference ClassReference { get; }
        public IndexReference ClassObject { get; }

        public ClassObjectResult(IndexReference classReference, IndexReference classObject)
        {
            ClassReference = classReference;
            ClassObject = classObject;
        }
    }
}