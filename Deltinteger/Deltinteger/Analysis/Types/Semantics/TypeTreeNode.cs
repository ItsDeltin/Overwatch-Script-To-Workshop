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
    class TypeTreeNode : IDisposable
    {
        readonly ContextInfo context; // The current context.
        readonly ITypeIdentifierErrorHandler errorHandler; // Error handler.
        readonly Action<TypePartResult> onChange; // The action to broadcast updates to.
        readonly string name; // The name of the type or module.
        readonly ScopeWatcher scopeWatcher; // The scope watcher.
        readonly IDisposableTypeDirector[] typeArgDirectors; // The type arguments.
        readonly IDisposable typeArgSubscriptions; // The subscriptions to the type arguments.

        CodeType[] typeArgs; // The actual CodeTypes of the type arguments. Is the same length as 'typeArgDirectors'.
        bool readyToUpdate; // Will be set to true once the ScopeWatcher provides a value.

        ITypePartHandler partHandler; // The current part handler.
        IDisposable partSubscription; // The subscription to the current part handler.

        public TypeTreeNode(ContextInfo context, ITypeIdentifierErrorHandler errorHandler, INamedType namedType, Action<TypePartResult> onChange)
        {
            this.context = context;
            this.errorHandler = errorHandler;
            this.onChange = onChange;
            name = namedType.Identifier?.Text;

            // Get the type arguments.
            typeArgDirectors = namedType.TypeArgs.Select(typeArgSyntax => TypeFromContext.TypeReferenceFromSyntax(context, typeArgSyntax)).ToArray();
            typeArgSubscriptions = Utility.Helper.Observe<CodeType>(typeArgDirectors, typeArgs =>
            {
                this.typeArgs = typeArgs;
                Update();
            });


            if (name != null)
            {
                scopeWatcher = context.Scope.Watch();
                // The IDisposable created here will be not be needed since ScopeWatcher.Dispose will handle it.
                scopeWatcher.Subscribe(change =>
                {
                    partHandler = GetPartHandler(change.Elements);
                    readyToUpdate = true;
                    Update();
                });
            }
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
            partSubscription = partHandler.Get(Observer.Create<TypePartResult>(onChange), new ProviderArguments(typeArgs, context.Parent));
        }


        public void Dispose()
        {
            scopeWatcher?.Dispose();
            typeArgDirectors.Dispose();
            typeArgSubscriptions.Dispose();
            errorHandler.Dispose();
            partSubscription?.Dispose();
        }
    }
}