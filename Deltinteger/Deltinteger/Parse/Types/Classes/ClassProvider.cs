using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassInitializer : ICodeTypeInitializer, IResolveElements, IWorkshopInit
    {
        public string Name { get; }
        public string Documentation { get; protected set; }
        public virtual int GenericsCount { get; protected set; }
        public List<ObjectVariable> ObjectVariables { get; } = new List<ObjectVariable>();
        public int Identifier { get; private set; } = -1;
        public CodeType Extends { get; protected set; }
        public Constructor[] Constructors { get; protected set; }

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
        public Scope OperationalScope { get; protected set; }
        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        public Scope StaticScope { get; protected set; }
        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        public Scope ServeObjectScope { get; protected set; }

        /// <summary>Determines if the class elements were resolved.</summary>
        protected bool _elementsResolved = false;
        /// <summary>Determines if the class was initialized for the workshop output.</summary>
        protected bool _workshopInitialized = false;

        public CodeType WorkingInstance { get; private set; }

        public ClassInitializer(string name)
        {
            Name = name;
            WorkingInstance = GetInstance();
        }

        public abstract bool BuiltInTypeMatches(Type type);

        public virtual void ResolveElements()
        {
            if (_elementsResolved) return;
            _elementsResolved = true;

            string scopeName = "class " + Name;

            if (Extends == null)
            {
                BaseScopes(scopeName);

                StaticScope.CompletionCatch = true;
                StaticScope.ProtectedCatch = true;
                ServeObjectScope.CompletionCatch = true;
            }
            else
            {
                ((ClassType)Extends).ResolveElements();

                StaticScope      = ((ClassType)Extends).Initializer.StaticScope.Child(scopeName);
                OperationalScope = ((ClassType)Extends).Initializer.OperationalScope.Child(scopeName);
                ServeObjectScope = ((ClassType)Extends).Initializer.ServeObjectScope.Child(scopeName);
            }

            StaticScope.PrivateCatch = true;
            OperationalScope.PrivateCatch = true;
            OperationalScope.This = WorkingInstance;
        }

        protected virtual void BaseScopes(string scopeName)
        {
            StaticScope = new Scope(scopeName);
            OperationalScope = new Scope(scopeName);
            ServeObjectScope = new Scope(scopeName);
        }

        private int StackStart(bool inclusive)
        {
            int extStack = 0;
            if (Extends != null) extStack = ((ClassType)Extends).Initializer.StackStart(true);
            if (inclusive) extStack += ObjectVariables.Count;
            return extStack;
        }

        public virtual void WorkshopInit(DeltinScript translateInfo)
        {
            if (_workshopInitialized) return;
            _workshopInitialized = true;

            ClassData classData = translateInfo.GetComponent<ClassData>();

            Identifier = classData.AssignID();
            int stackOffset = StackStart(false);

            Extends?.WorkshopInit(translateInfo);

            for (int i = 0; i < ObjectVariables.Count; i++)
                ObjectVariables[i].SetArrayStore(classData.GetClassVariableStack(translateInfo.VarCollection, i + stackOffset));
        }

        public IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            ClassData classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = Create(actionSet, classData);

            New(actionSet, new NewClassInfo(classReference, constructor, constructorValues, additionalParameterData));

            // Return the reference.
            return classReference.GetVariable();
        }

        public IndexReference Create(ActionSet actionSet, ClassData classData)
        {
            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = actionSet.VarCollection.Assign("_new_" + Name + "_class_index", actionSet.IsGlobal, true);
            classData.GetClassIndex(Identifier, classReference, actionSet);

            // Get object variables.
            BaseSetup(actionSet, (Element)classReference.GetVariable());

            return classReference;
        }

        public void BaseSetup(ActionSet actionSet, Element reference)
        {
            if (Extends != null)
                Extends.BaseSetup(actionSet, reference);

            foreach (ObjectVariable variable in ObjectVariables)
                variable.Init(actionSet, reference);
        }

        protected virtual void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Parse the constructor.
            newClassInfo.Constructor.Parse(actionSet, newClassInfo.ConstructorValues, newClassInfo.AdditionalParameterData);
        }

        protected ObjectVariable AddObjectVariable(IIndexReferencer variable)
        {
            // Create an ObjectVariable
            ObjectVariable createdObjectVariable = new ObjectVariable(variable);
            // Add the ObjectVariable to the ObjectVariables list. This will assign the variable a stack when WorkshopInit executes.
            ObjectVariables.Add(createdObjectVariable);
            // Copy the variable to the serve object scope. This allows the variable to be accessed when doing className.variableName. 
            ServeObjectScope.CopyVariable(variable);
            // Return the created ObjectVariable.
            return createdObjectVariable;
        }

        public virtual void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            Extends?.AddObjectVariablesToAssigner(reference, assigner);
            for (int i = 0; i < ObjectVariables.Count; i++)
                ObjectVariables[i].AddToAssigner((Element)reference, assigner);
        }

        public virtual CodeType GetInstance() => new ClassType(this);
        public virtual CodeType GetInstance(GetInstanceInfo instanceInfo) => new ClassType(this);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };
    }
}