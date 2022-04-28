using System;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics.Path
{
    using Core;
    using Scopes;

    class TypeTreeNode : IDotCrumbNode
    {
        // The node's identifier.
        readonly string name;

        // The node's error handler.
        readonly ITypeIdentifierErrorHandler errorHandler;

        // Context of the node.
        ContextInfo Context => helper.ContextInfo;

        readonly CrumbNodeFactoryHelper helper;

        readonly Utility2.SetDirectorType broadcastType;

        // The node's type argument directors.
        readonly IDisposableTypeDirector[] typeArgDirectors;

        // Manages the objects that the node depends on.
        readonly DependencyHandler dependencyHandler;

        CodeType[] typeArgs; // The current type arguments.
        ITypeNodeManager partHandler; // The current part handler.
        ITypeNodeInstance partInstance;

        public TypeTreeNode(CrumbNodeFactoryHelper helper, INamedType namedType, ITypeIdentifierErrorHandler errorHandler, Utility2.SetDirectorType broadcastType)
        {
            this.name = namedType.Identifier;
            this.errorHandler = errorHandler;
            this.helper = helper;
            this.broadcastType = broadcastType;

            // Create the dependency handler.
            dependencyHandler = new DependencyHandler(Context.Analysis);

            // Get the type arg directors.
            typeArgDirectors = namedType.TypeArgs.Select(typeArgSyntax => TypeFromContext.TypeReferenceFromSyntax(Context, typeArgSyntax)).ToArray();

            // Update when type arguments change.
            dependencyHandler.DependOn(helper =>
            {
                typeArgs = typeArgDirectors.Select(director => director.Type).ToArray();
                Update();
            }, typeArgDirectors);

            // Update when the scope updates.
            dependencyHandler.DependOn(helper =>
            {
                GetPartHandler();
                Update();
            }, Context.Scope);
        }

        void GetPartHandler()
        {
            // Reset the current error.
            errorHandler.Clear();

            // Find the scoped element.
            foreach (var element in Context.Scope.Elements.Where(e => e.Name == name && e.TypePartHandler != null))
                if (element.TypePartHandler.Valid(errorHandler, typeArgs.Length))
                {
                    // Found
                    partHandler = element.TypePartHandler;
                    return;
                }

            // Not found
            errorHandler.NoTypesMatchName();
            partHandler = UnknownTypePartHandler.Instance;
        }

        void Update()
        {
            // Make sure we have a part handler already
            if (partHandler == null)
                return;

            partInstance?.Dispose();
            partInstance = partHandler.GetPartInfo(new ProviderArguments(typeArgs, Context.Parent));

            helper.UpdateScope(partInstance.ScopeSource.Elements);

            if (broadcastType != null)
            {
                if (partInstance.Type == null)
                {
                    if (!errorHandler.HasError())
                        errorHandler.GotModuleExpectedType();
                    broadcastType(Types.StandardType.Unknown.Instance);
                }
                else
                    broadcastType(partInstance.Type);
            }
        }

        // IDotCrumbNode
        public IScopeSource ScopeSource => partInstance.ScopeSource;

        public void Dispose()
        {
            dependencyHandler.Dispose();
            errorHandler.Dispose();
            partInstance?.Dispose();
        }

        public ContextInfo SetChildContext(ContextInfo current) => current;
    }
}