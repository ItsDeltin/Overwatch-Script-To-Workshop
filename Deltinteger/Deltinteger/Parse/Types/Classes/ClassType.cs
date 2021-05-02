using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class ClassType : CodeType, IScopeAppender
    {
        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        public Scope StaticScope { get; set; }
        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        public Scope ServeObjectScope { get; set; }
        /// <summary></summary>
        public Scope OperationalScope { get; set; }

        // TODO: remove this bad boy
        public int Identifier { get; set; }

        public ObjectVariable[] ObjectVariables { get; protected set; }
        public IVariableInstance[] Variables { get; protected set; }

        // The provider that created this ClassType.
        public IClassInitializer Provider { get; }

        public ClassElements Elements { get; }

        public ClassType(string name, IClassInitializer provider) : base(name)
        {
            Provider = provider;
            Elements = new ClassElements(this);
        }

        public ClassType(string name) : base(name) {}

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            ClassData classData = translateInfo.GetComponent<ClassData>();
            Identifier = classData.AssignID();

            int stackOffset = StackStart(false);
            Extends?.WorkshopInit(translateInfo);

            foreach (var objectVariable in ObjectVariables)
            {
                objectVariable.SetArrayStore(translateInfo, stackOffset);
                stackOffset += objectVariable.StackCount;
            }
        }

        private int StackStart(bool inclusive)
        {
            int extStack = 0;
            if (Extends != null) extStack = ((ClassType)Extends).StackStart(true);
            if (inclusive) foreach (var variable in ObjectVariables) extStack += variable.StackCount;
            return extStack;
        }

        public override void BaseSetup(ActionSet actionSet, Element reference)
        {
            if (Extends != null)
                Extends.BaseSetup(actionSet, reference);

            foreach (ObjectVariable variable in ObjectVariables)
                variable.Init(actionSet, reference);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, WorkshopParameter[] parameters)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            ClassData classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Classes are stored in the class array (`classData.ClassArray`),
            // this stores the index where the new class is created at.
            var classReference = Create(actionSet, classData);

            New(actionSet, new NewClassInfo(classReference, constructor, parameters));

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

        protected virtual void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Parse the constructor.
            newClassInfo.Constructor.Parse(actionSet, newClassInfo.Parameters);
        }

        public override void Delete(ActionSet actionSet, Element reference)
        {
            if (Extends != null && Extends.CanBeDeleted)
                Extends.Delete(actionSet, reference);

            // TODO: delete
            // foreach (ObjectVariable objectVariable in ObjectVariables)
            //     actionSet.AddAction(objectVariable.ArrayStore.SetVariable(
            //         value: 0,
            //         index: reference
            //     ));
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            Extends?.AddObjectVariablesToAssigner(toWorkshop, reference, assigner);

            var classInitializer = toWorkshop.ClassInitializer;
            var initInfo = classInitializer.InitializedClassFromProvider(Provider);
            initInfo.AddVariableInstancesToAssigner(Variables, reference, assigner);
        }

        public override Scope GetObjectScope() => ServeObjectScope;
        public override Scope ReturningScope() => StaticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };

        public virtual void AddObjectBasedScope(IMethod function)
        {
            Elements.Add(function, true);
            ServeObjectScope.CopyMethod(function);
        }
        public virtual void AddStaticBasedScope(IMethod function)
        {
            Elements.Add(function, false);
            StaticScope.CopyMethod(function);
        }
        public virtual void AddObjectBasedScope(IVariableInstance variable)
        {
            Elements.Add(variable, true);
            ServeObjectScope.CopyVariable(variable);
        }
        public virtual void AddStaticBasedScope(IVariableInstance variable)
        {
            Elements.Add(variable, false);
            StaticScope.CopyVariable(variable);
        }
    
        public class ClassElements
        {
            readonly ClassType _classType;
            readonly List<IMethod> _virtualMethods = new List<IMethod>();
            readonly List<ClassElement> _scopeableElements = new List<ClassElement>();

            public ClassElements(ClassType classType)
            {
                _classType = classType;
            }

            public void AddVirtualFunction(IMethod method) => _virtualMethods.Add(method);

            public IMethod GetVirtualFunction(DeltinScript deltinScript, string name, CodeType[] parameterTypes)
            {
                // Loop through each virtual function.
                foreach (var virtualFunction in _virtualMethods)
                    // If the function's name matches and the parameter lengths are the same.
                    if (virtualFunction.Name == name && parameterTypes.Length == virtualFunction.Parameters.Length)
                    {
                        bool matches = true;
                        // Loop though the parameters.
                        for (int i = 0; i < parameterTypes.Length; i++)
                            // Make sure the parameter types match.
                            if (!parameterTypes[i].Is(virtualFunction.Parameters[i].GetCodeType(deltinScript)))
                            {
                                matches = false;
                                break;
                            }
                        
                        if (matches)
                            return virtualFunction;
                    }
                
                if (_classType.Extends != null) return ((ClassType)_classType.Extends).Elements.GetVirtualFunction(deltinScript, name, parameterTypes);
                return null;
            }

            public void Add(IScopeable scopeable, bool instance) => _scopeableElements.Add(new ClassElement(scopeable, instance));

            public void AddToScope(Scope scope, bool instance)
            {
                // Add elements from this class.
                foreach (var scopeable in _scopeableElements)
                    if (ValidAccessLevel(scopeable.Scopeable.AccessLevel) && (!scopeable.IsInstance || instance))
                        scope.AddNative(scopeable.Scopeable);
                
                // Add parent elements.
                (_classType.Extends as ClassType)?.Elements.AddToScope(scope, instance);
            }

            static bool ValidAccessLevel(AccessLevel accessLevel) => accessLevel == AccessLevel.Public || accessLevel == AccessLevel.Protected;

            struct ClassElement
            {
                public IScopeable Scopeable;
                public bool IsInstance;

                public ClassElement(IScopeable scopeable, bool isInstance)
                {
                    Scopeable = scopeable;
                    IsInstance = isInstance;
                }
            }
        }
    }
}