using System;
using DS.Analysis.Structure.Variables;
using DS.Analysis.Structure.DataTypes;
using DS.Analysis.Structure.Methods;
using DS.Analysis.Utility;
using DS.Analysis.Statements;
using DS.Analysis.Variables.Builder;
using DS.Analysis.Scopes;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure
{
    class StructureContext
    {
        public ScopeSource ScopeSource { get; private set; }

        public StructureContext(ScopeSource scopeSource)
        {
            ScopeSource = scopeSource;
        }

        public StructureContext(StructureContext other)
        {
            ScopeSource = other.ScopeSource;
        }

        public StructureContext SetScopeSource(ScopeSource source) => new StructureContext(this) { ScopeSource = source };

        public Statement StatementFromSyntax(IParseStatement syntax)
        {
            switch (syntax)
            {
                // Variable declaration
                case VariableDeclaration variableDeclaration:
                    return new DeclarationStatement(new DeclaredVariable(new VariableContextHandler(variableDeclaration)));
                
                // Type declaration
                case ClassContext dataTypeDeclaration:
                    return new DeclarationStatement(new DeclaredDataType(this, new DataTypeContentProvider(dataTypeDeclaration)));
                
                // Method declaration
                case FunctionContext methodDeclaration:
                    return new DeclarationStatement(new DeclaredMethod(this, new MethodContentProvider(methodDeclaration)));

                // If statement
                case If @if:
                    return new IfStatement(this, @if);
                
                // Import
                case Import import:
                    return new ImportStatement(this, import);
            }
            throw new NotImplementedException(syntax.GetType().ToString());
        }

        public BlockAction Block(Block block) => Block(block.Statements.ToArray());

        public BlockAction Block(IParseStatement[] statementSyntaxes)
        {
            var scopeSource = new ScopeSource();

            var statements = new Statement[statementSyntaxes.Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = SetScopeSource(scopeSource).StatementFromSyntax(statementSyntaxes[i]);
            
            return new BlockAction(statements, scopeSource);
        }
    }
}