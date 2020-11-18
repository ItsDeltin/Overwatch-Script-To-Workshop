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
        private readonly ParseInfo _parseInfo;
        public Scope OperationalScope { get; }
        private DefinedClassInitializer DefinedInitializer => (DefinedClassInitializer)Initializer;

        public DefinedClass(ParseInfo parseInfo, DefinedClassInitializer initializer, CodeType[] generics) : base(initializer)
        {
            _parseInfo = parseInfo;
            Generics = generics;
            var anonymousTypeLinker = new InstanceAnonymousTypeLinker(initializer.AnonymousTypes, generics);

            OperationalScope = new Scope(); // todooo
            ServeObjectScope = new Scope();
            StaticScope = new Scope();

            // Add elements to scope.
            foreach (var element in initializer.DeclaredElements)
                element.AddInstance(this, anonymousTypeLinker);
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

        public override CodeType GetRealerType(InstanceAnonymousTypeLinker instanceInfo)
        {
            var newGenerics = new CodeType[Generics.Length];

            for (int i = 0; i < newGenerics.Length; i++)
            {
                if (Generics[i] is AnonymousType at && instanceInfo.Links.ContainsKey(at))
                    newGenerics[i] = instanceInfo.Links[at];
                else
                    newGenerics[i] = Generics[i];
            }

            return DefinedInitializer.GetInstance(new GetInstanceInfo(newGenerics));
        }
    }
}