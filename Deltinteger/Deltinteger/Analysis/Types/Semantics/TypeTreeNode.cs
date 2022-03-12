using System;
using System.Reactive;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    using Scopes;
    using Utility;
    using Core;

    /// <summary>
    /// A node in a type tree. May be a module or type.
    /// </summary>
    class TypeTreeNode : IDisposable
    {
        readonly ContextInfo context; // The current context.
        readonly ITypeIdentifierErrorHandler errorHandler; // Error handler.
        readonly string name; // The name of the type or module.
        readonly IDisposableTypeDirector[] typeArgDirectors; // The type arguments.

        readonly DependencyHandler dependencyHandler;

        CodeType[] typeArgs; // The actual CodeTypes of the type arguments. Is the same length as 'typeArgDirectors'.

        ITypePartHandler partHandler; // The current part handler.
        ITypePartInfo typePartInfo; // The subscription to the current part handler.

        public TypeTreeNode(ContextInfo context, ITypeIdentifierErrorHandler errorHandler, INamedType namedType, Action<TypePartInfo> onChange)
        {
            this.context = context;
            this.errorHandler = errorHandler;
            this.onChange = onChange;
            name = namedType.Identifier?.Text;

            dependencyHandler = new DependencyHandler(context.Analysis);

            // Get the type arguments.
            typeArgDirectors = namedType.TypeArgs.Select(typeArgSyntax => TypeFromContext.TypeReferenceFromSyntax(context, typeArgSyntax)).ToArray();
            dependencyHandler.AddDisposables(typeArgDirectors);

            dependencyHandler.DependOn(helper => Update(), typeArgDirectors);
            dependencyHandler.DependOn(helper =>
            {
                partHandler = GetPartHandler(context.Scope.Elements);
                Update();
            }, context.Scope);
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
            // Make sure we have a part handler already
            if (partHandler == null)
                return;

            partSubscription?.Dispose();
            typePartInfo = partHandler.GetPartInfo(new ProviderArguments(typeArgs, context.Parent));
        }


        public void Dispose()
        {
            dependencyHandler.Dispose();
            errorHandler.Dispose();
            partSubscription?.Dispose();
        }
    }
}