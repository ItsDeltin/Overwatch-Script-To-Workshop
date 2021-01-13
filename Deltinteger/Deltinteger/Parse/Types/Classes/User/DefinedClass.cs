using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClass : ClassType
    {
        public Scope OperationalScope { get; }
        private readonly ParseInfo _parseInfo;
        private readonly DefinedClassInitializer _definedInitializer;

        public DefinedClass(ParseInfo parseInfo, DefinedClassInitializer initializer, CodeType[] generics) : base(initializer.Name, initializer)
        {
            _parseInfo = parseInfo;
            _definedInitializer = initializer;
            Generics = generics;
            var anonymousTypeLinker = new InstanceAnonymousTypeLinker(initializer.AnonymousTypes, generics);

            OperationalScope = new Scope(); // todooo
            ServeObjectScope = new Scope();
            StaticScope = new Scope();

            // Add elements to scope.
            ObjectVariables = new ObjectVariable[initializer.ObjectVariables.Count];
            foreach (var element in initializer.DeclaredElements)
            {
                var instance = element.AddInstance(this, anonymousTypeLinker);

                // Function
                if (instance is IMethod method && method.Attributes.Virtual)
                    VirtualFunctions.Add(method);
                
                // Variable
                else if (instance is IVariableInstance variableInstance)
                {
                    int objectVariableIndex = Array.IndexOf(initializer.ObjectVariables.ToArray(), variableInstance.Provider);
                    ObjectVariables[objectVariableIndex] = new ObjectVariable(variableInstance);
                }
            }
            
            Constructors = new[] {
                new Constructor(this, initializer.DefinedAt, AccessLevel.Public)
            };

            parseInfo.TranslateInfo.AddWorkshopInit(this);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Run the constructor.
            AddObjectVariablesToAssigner((Element)newClassInfo.ObjectReference.GetVariable(), actionSet.IndexAssigner);
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