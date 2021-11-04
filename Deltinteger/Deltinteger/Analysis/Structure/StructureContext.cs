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
        public ScriptFile File { get; }
        public ScopeSource ScopeSource { get; private set; }

        public StructureContext(ScriptFile file, ScopeSource scopeSource)
        {
            File = file;
            ScopeSource = scopeSource;
        }

        public StructureContext(StructureContext other)
        {
            File = other.File;
            ScopeSource = other.ScopeSource;
        }

        public StructureContext SetScopeSource(ScopeSource source) => new StructureContext(this) { ScopeSource = source };

        public Statement StatementFromSyntax(IParseStatement syntax)
        {
            switch (syntax)
            {
                // Variable declaration
                case VariableDeclaration variableDeclaration:
                    return new DeclarationStatement(this, new DeclaredVariable(new VariableContextHandler(variableDeclaration)));
                
                // Type declaration
                case ClassContext dataTypeDeclaration:
                    return new DeclarationStatement(this, new DeclaredDataType(this, new DataTypeContentProvider(dataTypeDeclaration)));
                
                // Method declaration
                case FunctionContext methodDeclaration:
                    return new DeclarationStatement(this, new DeclaredMethod(this, new MethodContentProvider(methodDeclaration)));

                // If statement
                case If @if:
                    return new IfStatement(this, @if);
                
                // Import
                case Import import:
                    return new ImportStatement(this, import);
            }
            throw new NotImplementedException(syntax.GetType().ToString());
        }

        public BlockAction Block(Block block, ScopeSource scopeSource = null) => Block(block.Statements.ToArray(), scopeSource);

        public BlockAction Block(IParseStatement[] statementSyntaxes, ScopeSource scopeSource = null)
        {
            scopeSource = scopeSource ?? new ScopeSource();

            var statements = new Statement[statementSyntaxes.Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = SetScopeSource(scopeSource).StatementFromSyntax(statementSyntaxes[i]);
            
            return new BlockAction(statements, scopeSource);
        }
    }
}