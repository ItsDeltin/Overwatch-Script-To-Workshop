using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Debugger;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Csv;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassType : CodeType
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

        public int Identifier { get; private set; } = -1;

        public readonly List<ObjectVariable> ObjectVariables = new List<ObjectVariable>();

        public ClassType(string name) : base(name)
        {
            CanBeDeleted = true;
            CanBeExtended = true;
            TokenType = TokenType.Class;
            DebugVariableResolver = new ClassDebugVariableResolver(this);
            Kind = TypeKind.Class;
        }

        public virtual void ResolveElements()
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

                staticScope = ((ClassType)Extends).staticScope.Child(scopeName);
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
            if (inclusive) extStack += ObjectVariables.Count;
            return extStack;
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            if (workshopInitialized) return;
            workshopInitialized = true;

            ClassData classData = translateInfo.GetComponent<ClassData>();

            Identifier = classData.AssignID();
            int stackOffset = StackStart(false);

            Extends?.WorkshopInit(translateInfo);

            for (int i = 0; i < ObjectVariables.Count; i++)
                ObjectVariables[i].SetArrayStore(classData.GetClassVariableStack(translateInfo.VarCollection, i + stackOffset));
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
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

        public override void BaseSetup(ActionSet actionSet, Element reference)
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

        public override void Delete(ActionSet actionSet, Element reference)
        {
            if (Extends != null && Extends.CanBeDeleted)
                Extends.Delete(actionSet, reference);

            foreach (ObjectVariable objectVariable in ObjectVariables)
                actionSet.AddAction(objectVariable.ArrayStore.SetVariable(
                    value: 0,
                    index: reference
                ));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            Extends?.AddObjectVariablesToAssigner(reference, assigner);
            for (int i = 0; i < ObjectVariables.Count; i++)
                ObjectVariables[i].AddToAssigner((Element)reference, assigner);
        }

        protected ObjectVariable AddObjectVariable(IIndexReferencer variable)
        {
            // Create an ObjectVariable
            ObjectVariable createdObjectVariable = new ObjectVariable(variable);
            // Add the ObjectVariable to the ObjectVariables list. This will assign the variable a stack when WorkshopInit executes.
            ObjectVariables.Add(createdObjectVariable);
            // Copy the variable to the serve object scope. This allows the variable to be accessed when doing className.variableName. 
            serveObjectScope.CopyVariable(variable);
            // Return the created ObjectVariable.
            return createdObjectVariable;
        }

        public override Scope GetObjectScope() => serveObjectScope;
        public override Scope ReturningScope() => staticScope;

        public override CompletionItem GetCompletion() => new CompletionItem()
        {
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
        public IIndexReferencer Variable { get; }
        public IndexReference ArrayStore { get; private set; }

        public ObjectVariable(IIndexReferencer variable)
        {
            Variable = variable;
        }

        public void SetArrayStore(IndexReference store)
        {
            ArrayStore = store;
        }

        public void AddToAssigner(Element reference, VarIndexAssigner assigner)
        {
            assigner.Add(Variable, ArrayStore.CreateChild(reference));
        }

        public void Init(ActionSet actionSet, Element reference)
        {
            if (Variable is Var var && var.InitialValue != null)
            {
                actionSet.AddAction(ArrayStore.SetVariable(
                    value: (Element)var.InitialValue.Parse(actionSet),
                    index: reference
                ));
            }
        }

        /// <summary>Creates a direct reference to the ArrayStore.</summary>
        public IndexReference Spot(Element reference) => ArrayStore.CreateChild(reference);

        /// <summary>Gets the value from a reference.</summary>
        public Element Get(Element reference) => ArrayStore.Get()[reference];

        /// <summary>Gets the value from the current context's object reference.</summary>
        public Element Get(ActionSet actionSet) => Get((Element)actionSet.CurrentObject);

        public void Set(ActionSet actionSet, Element reference, Element value)
        {
            actionSet.AddAction(ArrayStore.SetVariable(value: value, index: reference));
        }
    }

    public class ClassData : IComponent
    {
        public const string ObjectVariableTag = "_objectVariable_";
        public const string ClassIndexesTag = "_classIndexes";
        public DeltinScript DeltinScript { get; set; }
        public IndexReference ClassIndexes { get; private set; }
        private List<IndexReference> VariableStacks { get; } = new List<IndexReference>();
        private int AssignClassID = 0;

        public void Init()
        {
            ClassIndexes = DeltinScript.VarCollection.Assign(ClassIndexesTag, true, false);
            DeltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(0, null, Constants.MAX_ARRAY_LENGTH));
            //set the first element to -1. This makes it so that no instance == 0
            DeltinScript.InitialGlobal.ActionSet.AddAction(ClassIndexes.SetVariable(-1, null, 0));
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

        public IndexReference GetClassVariableStack(VarCollection collection, int index)
        {
            if (index > VariableStacks.Count) throw new Exception("Variable stack skipped");
            if (index == VariableStacks.Count)
                VariableStacks.Add(collection.Assign(ObjectVariableTag + index, true, false));

            return VariableStacks[index];
        }

        public int AssignID()
        {
            AssignClassID++;
            return AssignClassID;
        }
    }

    /// <summary>Links variables with the type of a class with debugger variables discovered from the action stream.</summary>
    public class ClassDebugVariableResolver : IDebugVariableResolver
    {
        public ClassType Class { get; }

        public ClassDebugVariableResolver(ClassType @class)
        {
            Class = @class;
        }

        public DBPVariable GetVariable(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return null;

            // Create the variable.
            DBPVariable variable = new DBPVariable(debugVariable, Class.Name);
            variable.namedVariables = Class.ObjectVariables.Count;
            variable.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);

            return variable;
        }

        public EvaluateResponse GetEvaluation(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return EvaluateResponse.Empty;

            // Create the evaluation response.
            IDebugVariable.ApplyReference(collection, debugVariable);
            EvaluateResponse response = new EvaluateResponse(collection, debugVariable);
            response.namedVariables = Class.ObjectVariables.Count;

            return response;
        }

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
        {
            // Use the default resolver if the value is not a number.
            if (parent.Value is CsvNumber == false)
                return new DefaultResolver().GetChildren(collection, parent);

            // The class reference of the parent variable.
            int reference = (int)((CsvNumber)parent.Value).Value;

            IDebugVariable[] variables = new IDebugVariable[Class.ObjectVariables.Count];
            for (int i = 0; i < variables.Length; i++)
            {
                CsvPart value = new CsvNull();

                // Get the related object variable array.
                var objectVariableArray = collection.ActionStream.Variables.FirstOrDefault(v => v.Name == ClassData.ObjectVariableTag + i);
                if (objectVariableArray != null && objectVariableArray.Value is Csv.CsvArray csvArray && reference < csvArray.Values.Length)
                    value = csvArray.Values[reference];

                variables[i] = new ChildDebugVariable(
                    // Child variable resolver
                    Class.ObjectVariables[i].Variable.Type()?.DebugVariableResolver ?? new DefaultResolver(),
                    // Value
                    value,
                    // Name
                    Class.ObjectVariables[i].Variable.Name,
                    // Type
                    Class.ObjectVariables[i].Variable.Type()?.GetName() ?? "define"
                );
                collection.Add(variables[i]);
            }

            return variables;
        }
    }
}