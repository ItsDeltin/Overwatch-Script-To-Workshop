using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public abstract class ClassInitializer : ICodeTypeInitializer, IResolveElements
    {
        public string Name { get; }
        public string Documentation { get; protected set; }
        public virtual int GenericsCount { get; protected set; }
        public CodeType Extends { get; protected set; }
        public Constructor[] Constructors { get; protected set; }
        public CodeType WorkingInstance { get; protected set; }

        /// <summary>Determines if the class elements were resolved.</summary>
        protected bool _elementsResolved = false;

        protected List<IVariable> _objectVariables = new List<IVariable>();
        public IReadOnlyList<IVariable> ObjectVariables => _objectVariables;

        public ClassInitializer(string name)
        {
            Name = name;
        }

        public abstract bool BuiltInTypeMatches(Type type);

        public virtual void ResolveElements()
        {
            if (_elementsResolved) return;
            _elementsResolved = true;
            if (Extends != null) ((ClassType)Extends).ResolveElements();
        }

        public virtual CodeType GetInstance() => new ClassType(Name);
        public virtual CodeType GetInstance(GetInstanceInfo instanceInfo) => new ClassType(Name);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };

        public void AddVariable(IVariable var) => _objectVariables.Add(var);
    }
}