using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Structure.RuleContentProvider;
using DS.Analysis.Structure.DataTypes;
using DS.Analysis.Structure.Methods;

namespace DS.Analysis.Structure.Utility
{
    static partial class StructureUtility
    {
        public static AbstractDeclaredElement DeclarationFromSyntax(IDeclaration declaration)
        {
            switch (declaration)
            {
                // Workshop rule
                case RuleContext ruleContext:
                    return new GenericRuleDeclaration(new DeclaredRuleContentProvider(ruleContext));
                
                // Data type
                case ClassContext dataTypeContext:
                    return new DeclaredDataType(new DeclaredDataTypeContentProvider(dataTypeContext));

                // Method
                case FunctionContext functionContext:
                    return new GenericMethodDeclaration(new DeclaredMethodContentProvider(functionContext));
                
                // Unknown type
                default:
                    throw new NotImplementedException(declaration.GetType().ToString());
            }
        }
        
        public static AbstractDeclaredElement[] DeclarationsFromSyntax(List<IDeclaration> declarations)
        {
            var result = new AbstractDeclaredElement[declarations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = DeclarationFromSyntax(declarations[i]);
            return result;
        }

        public static AbstractDeclaredElement[] DeclarationsFromSyntax(RootContext context)
        {
            var elements = new List<AbstractDeclaredElement>();

            foreach (var declaration in context.Declarations)
                elements.Add(DeclarationFromSyntax(declaration));
            
            foreach (var dataType in context.Classes)
                elements.Add(new DeclaredDataType(new DeclaredDataTypeContentProvider(dataType)));
            
            return elements.ToArray();
        }
    }
}