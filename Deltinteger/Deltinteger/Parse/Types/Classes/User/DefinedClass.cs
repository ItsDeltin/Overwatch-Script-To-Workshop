using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClass : ClassType
    {
        readonly ParseInfo _parseInfo;
        readonly DefinedClassInitializer _definedInitializer;
        readonly InstanceAnonymousTypeLinker _typeLinker;

        public DefinedClass(ParseInfo parseInfo, DefinedClassInitializer initializer, CodeType[] generics) : base(initializer.Name, initializer)
        {
            CanBeDeleted = true;
            Generics = generics;

            _parseInfo = parseInfo;
            _definedInitializer = initializer;
            _typeLinker = new InstanceAnonymousTypeLinker(initializer.GenericTypes, generics);

            Description = initializer.Doc?.Description;
        }

        protected override void Setup()
        {
            base.Setup();

            Extends = _definedInitializer.Extends?.GetRealType(_typeLinker);

            ObjectScope = new Scope();
            StaticScope = new Scope();

            // Add elements to scope.
            foreach (var element in _definedInitializer.DeclaredElements)
            {
                var instance = element.AddInstance(this, _typeLinker);

                // Virtual function
                if (element is DefinedMethodProvider provider && provider.Virtual)
                    Elements.AddVirtualFunction((IMethod)instance);

                // Virtual variable
                if (element is Var var && var.Virtual)
                    Elements.AddVirtualVariable((IVariableInstance)instance);
            }

            if (Extends is ClassType classType)
            {
                classType.Elements.AddToScope(_parseInfo.TranslateInfo, ObjectScope, true);
                classType.Elements.AddToScope(_parseInfo.TranslateInfo, StaticScope, false);
            }

            // Add constructors.
            Constructors = new Constructor[_definedInitializer.Constructors.Length];
            for (int i = 0; i < _definedInitializer.Constructors.Length; i++)
                Constructors[i] = _definedInitializer.Constructors[i].GetInstance(this, _typeLinker);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Run the constructor.
            AddObjectVariablesToAssigner(actionSet.ToWorkshop, new(newClassInfo.ObjectReference), actionSet.IndexAssigner);
            newClassInfo.Constructor.Parse(actionSet.New((Element)newClassInfo.ObjectReference.GetVariable()), newClassInfo.Parameters);
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            var newGenerics = new CodeType[Generics.Length];

            for (int i = 0; i < newGenerics.Length; i++)
            {
                if (Generics[i] is AnonymousType at && instanceInfo.Links.ContainsKey(at))
                    newGenerics[i] = instanceInfo.Links[at];
                else
                    newGenerics[i] = Generics[i].GetRealType(instanceInfo);
            }

            return _definedInitializer.GetInstance(new GetInstanceInfo(newGenerics));
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.Elements.AddDeclarationCall(_definedInitializer, new DeclarationCall(callRange, false));
            parseInfo.Script.AddDefinitionLink(callRange, _definedInitializer.DefinedAt);
        }
    }
}