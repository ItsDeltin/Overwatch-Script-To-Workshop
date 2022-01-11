using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    using Utility;

    /// <summary>
    /// A list of modules and types separated by dots which results in a data type.
    /// </summary>
    class TypeTree : IDisposableTypeDirector
    {
        readonly ObserverCollection<CodeType> observers = Helper.CreateTypeObserver();

        readonly ContextInfo context;
        readonly INamedType[] partSyntaxes;
        readonly TypeTreeNode[] parts;

        IDisposable resultingDiagnostic;


        public TypeTree(ContextInfo context, INamedType[] partSyntaxes)
        {
            this.context = context;
            this.partSyntaxes = partSyntaxes;
            parts = new TypeTreeNode[partSyntaxes.Length];

            SubscribeToPartIndex(0, context);
        }

        void SubscribeToPartIndex(int index, ContextInfo context)
        {
            // Create the error handler for the tree part.
            var errorHandler = new TypeIdentifierErrorHandler(context, context.File.Diagnostics.CreateNamedToken(partSyntaxes[index].Identifier));

            // Create the node.
            parts[index] = new TypeTreeNode(context, errorHandler, partSyntaxes[index], result =>
            {
                // If this is the last part, notify the observers.
                if (index == parts.Length - 1)
                {
                    if (result.Type == null)
                    {
                        // Not a type.
                        // Make sure the identifier token exists before adding the error.
                        if (partSyntaxes[index].Identifier)
                            resultingDiagnostic = context.Error(partSyntaxes[index].Identifier + " is a module, not a type", partSyntaxes[index].Identifier);

                        observers.Set(Standard.StandardTypes.Unknown.Instance);
                    }
                    else
                        observers.Set(result.Type);
                }
                // Otherwise, refresh the proceeding parts.
                else
                {
                    Dispose(index + 1);
                    SubscribeToPartIndex(index + 1, context.SetScope(result.Scope).SetParent(result.ParentElement));
                }
            });
        }


        public void Dispose()
        {
            Dispose(0);
            observers.Complete();
        }

        void Dispose(int startingIndex)
        {
            resultingDiagnostic?.Dispose();
            resultingDiagnostic = null;

            for (int i = startingIndex; i < parts.Length; i++)
            {
                parts[i]?.Dispose();
                parts[i] = null;
            }
        }

        public IDisposable Subscribe(IObserver<CodeType> observer) => observers.Add(observer);
    }
}