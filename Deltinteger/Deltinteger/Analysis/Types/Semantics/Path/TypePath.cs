using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics.Path
{
    using Core;
    using DS.Analysis.Scopes;

    class TypePath
    {
        public static IDisposableTypeDirector CreateTypeTree(ContextInfo context, INamedType[] partSyntaxes) =>
            Utility2.CreateDirector((setType, _) =>
            {
                DotCrumb dotCrumb = new DotCrumb(context);

                // Get the type nodes.
                for (int i = 0; i < partSyntaxes.Length; i++)
                {
                    bool isLast = i == partSyntaxes.Length - 1;

                    dotCrumb.AddNode(helper =>
                    {
                        // Create the error handler for the tree part.
                        var errorHandler = new TypeIdentifierErrorHandler(helper.ContextInfo, helper.ContextInfo.File.Diagnostics.CreateNamedToken(partSyntaxes[i].Identifier));

                        // Create the TypeTreeNode.
                        return new TypeTreeNode(helper, partSyntaxes[i], errorHandler, isLast ? setType : null);
                    }, isLast);
                }

                return dotCrumb;
            });
    }
}