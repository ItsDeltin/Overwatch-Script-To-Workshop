using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Structure.Variables;
using DS.Analysis.Structure.DataTypes;
using DS.Analysis.Structure.Methods;
using DS.Analysis.Structure.Modules;
using DS.Analysis.Variables.Builder;

namespace DS.Analysis.Structure.Utility
{
    static partial class StructureUtility
    {
        public static AbstractDeclaredElement DeclarationFromSyntax(ContextInfo contextInfo, IDeclaration declaration)
        {
            switch (declaration)
            {
                // Variable declaration
                case VariableDeclaration variableDeclaration:
                    return new DeclaredVariable(contextInfo, new VariableContextHandler(variableDeclaration));

                // Data type
                case ClassContext dataTypeContext:
                    return new DeclaredDataType(contextInfo, new UserContentProvider(dataTypeContext));

                // Method
                case FunctionContext functionContext:
                    return new DeclaredMethod(contextInfo, new MethodContentProvider(functionContext));

                // Module
                case ModuleContext moduleContext:
                    return new DeclaredModule(contextInfo, new ModuleContentProvider(moduleContext));

                // Unknown type
                default:
                    throw new NotImplementedException(declaration.GetType().ToString());
            }
        }

        public static AbstractDeclaredElement[] DeclarationsFromSyntax(ContextInfo contextInfo, List<IDeclaration> declarations)
        {
            var result = new AbstractDeclaredElement[declarations.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = DeclarationFromSyntax(contextInfo, declarations[i]);
                result[i].AddToScope(contextInfo.ScopeAppender);
            }
            return result;
        }
    }
}