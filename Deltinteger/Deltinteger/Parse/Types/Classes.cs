using System;
using System.Collections.Generic;
using System.Linq;
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
            CanBeDeleted = true;
            StaticScope = new Scope("class " + name);
            ObjectScope = StaticScope.Child("class " + name);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            // TODO: Class identifier
            IndexReference objectReference = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(1, actionSet, "_new_PathMap");

            New(actionSet, objectReference, constructor, constructorValues, additionalParameterData);

            // Return the reference.
            return objectReference.GetVariable();
        }

        protected virtual void New(ActionSet actionSet, IndexReference objectReference, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Parse the constructor.
            constructor.Parse(actionSet, constructorValues, additionalParameterData);
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
        private List<IndexReference> VariableStacks { get; } = new List<IndexReference>();
        private int AssignClassID = 0;

        public ClassData(VarCollection varCollection)
        {
            ClassIndexes = varCollection.Assign("_classIndexes", true, false);
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
                Element.Part<V_IndexOfArrayValue>(
                    ClassIndexes.GetVariable(),
                    new V_Number(0)
                )
            ));
            actionSet.AddAction(ClassIndexes.SetVariable(
                classIdentifier,
                null,
                (Element)classReference.GetVariable()
            ));
        }

        public IndexReference GetClassVariableStack(VarCollection collection, int index)
        {
            if (index > VariableStacks.Count) throw new Exception("Variable stack skipped");
            if (index == VariableStacks.Count)
                VariableStacks.Add(collection.Assign("_objectVariable_" + index, true, false));
            
            return VariableStacks[index];
        }

        public int AssignID()
        {
            AssignClassID++;
            return AssignClassID;
        }
    }
}