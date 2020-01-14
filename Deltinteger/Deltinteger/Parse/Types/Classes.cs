using System;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassType : CodeType
    {
        protected Scope ObjectScope { get; }
        protected Scope StaticScope { get; }

        public ClassType(string name) : base(name)
        {
            StaticScope = new Scope("class " + name);
            ObjectScope = StaticScope.Child("class " + name);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            ClassObjectResult objectData = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(actionSet, "_new_PathMap");

            New(actionSet, objectData, constructor, constructorValues, additionalParameterData);

            // Return the reference.
            return objectData.ClassReference.GetVariable();
        }

        protected virtual void New(ActionSet actionSet, ClassObjectResult objectData, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Parse the constructor.
            constructor.Parse(actionSet, constructorValues, additionalParameterData);
        }

        public override IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element)
        {
            return translateInfo.SetupClasses().ClassArray.CreateChild((Element)element);
        }

        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => StaticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };
    }

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