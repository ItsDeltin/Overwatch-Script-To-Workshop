using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClass : ClassType
    {
        public Scope OperationalScope { get; }
        private readonly ParseInfo _parseInfo;
        private readonly DefinedClassInitializer _definedInitializer;

        public DefinedClass(ParseInfo parseInfo, DefinedClassInitializer initializer, CodeType[] generics) : base(initializer.Name, initializer)
        {
            CanBeExtended = true;
            CanBeDeleted = true;

            _parseInfo = parseInfo;
            _definedInitializer = initializer;
            Generics = generics;
            var anonymousTypeLinker = new InstanceAnonymousTypeLinker(initializer.AnonymousTypes, generics);

            OperationalScope = new Scope(); // todooo
            ServeObjectScope = new Scope();
            StaticScope = new Scope();

            // Add elements to scope.
            ObjectVariables = new ObjectVariable[initializer.ObjectVariables.Count];
            var initializedVariables = new List<IVariableInstance>();
            foreach (var element in initializer.DeclaredElements)
            {
                var instance = element.AddInstance(this, anonymousTypeLinker);

                // Function
                if (instance is IMethod method && method.Attributes.Virtual)
                    VirtualFunctions.Add(method);
                
                // Variable
                else if (instance is IVariableInstance variableInstance)
                {
                    initializedVariables.Add(variableInstance);

                    int objectVariableIndex = Array.IndexOf(initializer.ObjectVariables.ToArray(), variableInstance.Provider);
                    ObjectVariables[objectVariableIndex] = new ObjectVariable(variableInstance);
                }
            }

            Variables = initializedVariables.ToArray();

            // Add constructors.
            Constructors = new Constructor[_definedInitializer.Constructors.Length];
            for (int i = 0; i < _definedInitializer.Constructors.Length; i++)
                Constructors[i] = _definedInitializer.Constructors[i].GetInstance(this, anonymousTypeLinker);

            parseInfo.TranslateInfo.AddWorkshopInit(this);
        }

        public override bool Is(CodeType other)
        {
            // Make sure the other is a DefinedClass with the same provider.
            if (!(other is DefinedClass definedClass && definedClass._definedInitializer == _definedInitializer))
                return false;

            // Check if the generics match.
            for (int i = 0; i < Generics.Length; i++)
                if (!Generics[i].Is(other.Generics[i]))
                    return false;

            return true;
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Run the constructor.
            AddObjectVariablesToAssigner(actionSet.ToWorkshop, (Element)newClassInfo.ObjectReference.GetVariable(), actionSet.IndexAssigner);
            newClassInfo.Constructor.Parse(actionSet.New((Element)newClassInfo.ObjectReference.GetVariable()), newClassInfo.ConstructorValues, null);
        }

        public override void AddObjectBasedScope(IMethod function)
        {
            base.AddObjectBasedScope(function);
            OperationalScope.CopyMethod(function);
            ServeObjectScope.CopyMethod(function);
        }

        public override void AddStaticBasedScope(IMethod function)
        {
            base.AddStaticBasedScope(function);
            OperationalScope.CopyMethod(function);
            StaticScope.CopyMethod(function);
        }

        public override void AddObjectBasedScope(IVariableInstance variable)
        {
            base.AddObjectBasedScope(variable);
            OperationalScope.CopyVariable(variable);
            ServeObjectScope.CopyVariable(variable);
        }

        public override void AddStaticBasedScope(IVariableInstance variable)
        {
            base.AddStaticBasedScope(variable);
            OperationalScope.CopyVariable(variable);
            StaticScope.CopyVariable(variable);
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            var newGenerics = new CodeType[Generics.Length];

            for (int i = 0; i < newGenerics.Length; i++)
            {
                if (Generics[i] is AnonymousType at && instanceInfo.Links.ContainsKey(at))
                    newGenerics[i] = instanceInfo.Links[at];
                else
                    newGenerics[i] = Generics[i];
            }

            return _definedInitializer.GetInstance(new GetInstanceInfo(newGenerics));
        }
    }
}