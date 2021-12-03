using System;
using System.Reactive;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    using Scopes;
    using Utility;

    /// <summary>
    /// A node in a type tree. May be a module or type.
    /// </summary>
    class TypeTreeNode : IObservable<TypePartResult>, IDisposable
    {
        readonly ObserverCollection<TypePartResult> observers = new ValueObserverCollection<TypePartResult>(new TypePartResult(null, Scope.Empty));

        readonly ContextInfo context;
        readonly ITypeIdentifierErrorHandler errorHandler;
        readonly string name;
        readonly ScopeWatcher scopeWatcher;
        readonly IDisposableTypeDirector[] typeArgDirectors;
        readonly IDisposable typeArgSubscriptions;

        CodeType[] typeArgs;
        bool readyToUpdate;

        ITypePartHandler partHandler;
        IDisposable partSubscription;

        public TypeTreeNode(ContextInfo context, ITypeIdentifierErrorHandler errorHandler, INamedType namedType)
        {
            this.context = context;
            this.errorHandler = errorHandler;
            name = namedType.Identifier;

            // Get the type arguments.
            typeArgDirectors = namedType.TypeArgs.Select(typeArgSyntax => TypeFromContext.TypeReferenceFromSyntax(context, typeArgSyntax)).ToArray();
            typeArgSubscriptions = Utility.Helper.Observe<CodeType>(typeArgDirectors, typeArgs =>
            {
                this.typeArgs = typeArgs;
                Update();
            });

            scopeWatcher = context.Scope.Watch();
            // The IDisposable created here will be not be needed since ScopeWatcher.Dispose will handle it.
            scopeWatcher.Subscribe(change =>
            {
                partHandler = GetPartHandler(change.Elements);
                readyToUpdate = true;
                Update();
            });
        }


        ITypePartHandler GetPartHandler(IEnumerable<ScopedElement> scopedElements)
        {
            // Reset the current error.
            errorHandler.Clear();

            // Find the scoped element.
            foreach (var element in scopedElements.Where(e => e.Name == name && e.TypePartHandler != null))
                if (element.TypePartHandler.Valid(errorHandler, typeArgs.Length))
                    // Found
                    return element.TypePartHandler;

            // Not found
            errorHandler.NoTypesMatchName();
            return UnknownTypePartHandler.Instance;
        }

        void Update()
        {
            if (!readyToUpdate)
                return;

            partSubscription?.Dispose();
            partSubscription = partHandler.Get(Observer.Create<TypePartResult>(observers.Set), new ProviderArguments(typeArgs, context.Parent));
        }


        public void Dispose()
        {
            scopeWatcher.Dispose();
            typeArgDirectors.Dispose();
            typeArgSubscriptions.Dispose();
            errorHandler.Dispose();
            partSubscription.Dispose();
        }

        public IDisposable Subscribe(IObserver<TypePartResult> observer) => observers.Add(observer);
    }
}