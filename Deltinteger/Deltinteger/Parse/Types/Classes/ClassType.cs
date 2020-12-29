using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class ClassType : CodeType, IScopeAppender
    {
        public ClassInitializer Initializer { get; protected set; }

        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        public Scope StaticScope { get; set; }
        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        public Scope ServeObjectScope { get; set; }

        public int Identifier => Initializer.Identifier;

        public List<ObjectVariable> ObjectVariables { get; } = new List<ObjectVariable>();

        public ClassType(ClassInitializer initializer) : base(initializer.Name)
        {
            Initializer = initializer;
        }

        public override void ResolveElements() => Initializer.ResolveElements();
        public override void WorkshopInit(DeltinScript translateInfo) => Initializer.WorkshopInit(translateInfo);
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

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner) => Initializer.AddObjectVariablesToAssigner(reference, assigner);

        public override Scope GetObjectScope() => ServeObjectScope;
        public override Scope ReturningScope() => StaticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };

        public virtual void AddObjectBasedScope(IMethod function) => ServeObjectScope.CopyMethod(function);
        public virtual void AddStaticBasedScope(IMethod function) => StaticScope.CopyMethod(function);
        public virtual void AddObjectBasedScope(IVariableInstance variable) => ServeObjectScope.CopyVariable(variable);
        public virtual void AddStaticBasedScope(IVariableInstance variable) => StaticScope.CopyVariable(variable);
    }
}