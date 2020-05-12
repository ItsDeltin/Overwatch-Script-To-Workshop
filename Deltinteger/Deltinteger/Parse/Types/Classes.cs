using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassType : CodeType, IImplementer
    {
        /*
Static scope, static members only.
> Give to returning scope
> static methods

Operational scope. All object and static members.
> object methods

Object-serve scope. Only object members.
> Give to object scope.
        */

        /// <summary>Used in object methods and constructors.</summary>
        protected Scope operationalScope;
        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        protected Scope staticScope;
        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        protected Scope serveObjectScope;

        /// <summary>Determines if the class elements were resolved.</summary>
        protected bool elementsResolved { get; private set; } = false;

        /// <summary>Determines if the class was initialized for the workshop output.</summary>
        protected bool workshopInitialized { get; private set; } = false;

        /// <summary>The interfaces that the class implements.</summary>
        public List<Interface> Contracts { get; } = new List<Interface>();

        /// <summary>The unique identifier for the class. This is set after WorkshopInit runs.</summary>
        public int Identifier { get; private set; } = -1;

        public readonly List<ObjectVariable> ObjectVariables = new List<ObjectVariable>();
        public readonly List<IMethod> ObjectFunctions = new List<IMethod>();

        public ClassType(string name) : base(name)
        {
            CanBeDeleted = true;
            CanBeExtended = true;
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            elementsResolved = true;

            string scopeName = "class " + Name;

            if (Extends == null)
            {
                BaseScopes(scopeName);

                staticScope.CompletionCatch = true;
                staticScope.ProtectedCatch = true;
                serveObjectScope.CompletionCatch = true;
            }
            else
            {
                ((ClassType)Extends).ResolveElements();

                staticScope      = ((ClassType)Extends).staticScope.Child(scopeName);
                operationalScope = ((ClassType)Extends).operationalScope.Child(scopeName);
                serveObjectScope = ((ClassType)Extends).serveObjectScope.Child(scopeName);
            }

            staticScope.PrivateCatch = true;
            operationalScope.PrivateCatch = true;
            operationalScope.This = this;
        }

        protected virtual void BaseScopes(string scopeName)
        {
            staticScope = new Scope(scopeName);
            operationalScope = new Scope(scopeName);
            serveObjectScope = new Scope(scopeName);
        }

        private int StackStart(bool inclusive)
        {
            int extStack = 0;
            if (Extends != null) extStack = ((ClassType)Extends).StackStart(true);
            if (inclusive)
            {
                foreach (var ov in ObjectVariables)
                    if (ov.ArrayStore == null)
                        extStack++;
            }
            return extStack;
        }

        public override bool DoesImplement(CodeType type)
        {
            if (base.DoesImplement(type)) return true;
            if (type is Interface)
                foreach (Interface implements in Contracts)
                    if (implements.DoesImplement(type)) return true;
            return false;
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            if (workshopInitialized) return;
            workshopInitialized = true;

            // Assign an identifier.
            ClassData classData = translateInfo.GetComponent<ClassData>();
            Identifier = classData.AssignID();

            // Setup interfaces.
            foreach (Interface contract in Contracts)
                contract.SetupImplementer(this);

            int stackOffset = StackStart(false);

            Extends?.WorkshopInit(translateInfo);

            for (int i = 0, c = 0; i < ObjectVariables.Count; i++)
            {
                if (ObjectVariables[i].ArrayStore == null)
                {
                    ObjectVariables[i].SetArrayStore(classData.GetClassVariableStack(translateInfo.VarCollection, c + stackOffset));
                    c++;
                }
            }
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            ClassData classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            classData.GetClassIndex(Identifier, classReference, actionSet);

            // Get object variables.
            BaseSetup(actionSet, (Element)classReference.GetVariable());
            New(actionSet, new NewClassInfo(classReference, constructor, constructorValues, additionalParameterData));

            // Return the reference.
            return classReference.GetVariable();
        }

        public override void BaseSetup(ActionSet actionSet, Element reference)
        {
            if (Extends != null)
                Extends.BaseSetup(actionSet, reference);

            foreach (ObjectVariable variable in ObjectVariables)
            if (variable.Variable.InitialValue != null)
            {
                actionSet.AddAction(variable.ArrayStore.SetVariable(
                    value: (Element)variable.Variable.InitialValue.Parse(actionSet),
                    index: reference
                ));
            }
        }

        protected virtual void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Parse the constructor.
            newClassInfo.Constructor.Parse(actionSet, newClassInfo.ConstructorValues, newClassInfo.AdditionalParameterData);
        }

        public override void Delete(ActionSet actionSet, Element reference)
        {
            if (Extends != null && Extends.CanBeDeleted)
                Extends.Delete(actionSet, reference);

            foreach (ObjectVariable objectVariable in ObjectVariables)
                actionSet.AddAction(objectVariable.ArrayStore.SetVariable(
                    value: new V_Number(0),
                    index: reference
                ));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            Extends?.AddObjectVariablesToAssigner(reference, assigner);
            for (int i = 0; i < ObjectVariables.Count; i++)
                ObjectVariables[i].AddToAssigner((Element)reference, assigner);
        }

        public override Scope GetObjectScope() => serveObjectScope;
        public override Scope ReturningScope() => staticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };
    }

    public class NewClassInfo
    {
        public IndexReference ObjectReference { get; }
        public Constructor Constructor { get; }
        public IWorkshopTree[] ConstructorValues { get; }
        public object[] AdditionalParameterData { get; }
        
        public NewClassInfo(IndexReference objectReference, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            ObjectReference = objectReference;
            Constructor = constructor;
            ConstructorValues = constructorValues;
            AdditionalParameterData = additionalParameterData;
        }
    }

    public class ObjectVariable
    {
        public Var Variable { get; }
        public IndexReference ArrayStore { get; private set; }

        public ObjectVariable(Var variable)
        {
            Variable = variable;
        }

        public void SetArrayStore(IndexReference store)
        {
            if (ArrayStore == null)
                ArrayStore = store;
        }

        public void AddToAssigner(Element reference, VarIndexAssigner assigner)
        {
            assigner.Add(Variable, ArrayStore.CreateChild(reference));
        }
    }

    public class ClassData : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        public IndexReference ClassIndexes { get; private set; }
        private List<IndexReference> VariableStacks { get; } = new List<IndexReference>();
        private int AssignClassID = 0;

        public void Init()
        {
            ClassIndexes = DeltinScript.VarCollection.Assign("_classIndexes", true, false);
            DeltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(0, null, Constants.MAX_ARRAY_LENGTH));
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