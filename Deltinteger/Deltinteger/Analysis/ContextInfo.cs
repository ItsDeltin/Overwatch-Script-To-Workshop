using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Scopes;
using DS.Analysis.Expressions;
using DS.Analysis.Expressions.Dot;
using DS.Analysis.Statements;
using DS.Analysis.Variables.Builder;

namespace DS.Analysis
{
    class ContextInfo
    {
        public DeltinScriptAnalysis Analysis { get; }
        public File File { get; }
        public Scope Scope { get; private set; }
        public Scope Getter { get; private set; }
        public ContextKind ContextKind { get; private set; }

        public ContextInfo(DeltinScriptAnalysis analysis, File file, Scope scope)
        {
            Analysis = analysis;
            File = file;
            Scope = scope;
            Getter = scope;
        }

        private ContextInfo(ContextInfo other)
        {
            Analysis = other.Analysis;
            File = other.File;
            Scope = other.Scope;
            Getter = other.Getter;
            ContextKind = other.ContextKind;
        }


        public ContextInfo ClearTail() => new ContextInfo(this);

        public ContextInfo ClearHead() => new ContextInfo(this);

        public ContextInfo SetSourceExpression(Expression source) => new ContextInfo(this);

        public ContextInfo SetScope(Scope scope) => new ContextInfo(this) { Scope = scope };


        public Expression GetExpression(IParseExpression expressionContext)
        {
            switch (expressionContext)
            {
                // Identifier
                case Identifier identifier: return new IdentifierExpression(this, identifier, /* todo */ null);
                // True/false
                case BooleanExpression boolean: return new BooleanAction(this, boolean);
                // Operator
                case BinaryOperatorExpression op:
                    if (op.IsDotExpression())
                        return new DotExpression(this, op);
                    else
                        break; // todo
            }

            throw new NotImplementedException(expressionContext.GetType().ToString());
        }


        public Statement GetStatement(IParseStatement statementContext)
        {
            switch (statementContext)
            {
                // Variable declaration
                case VariableDeclaration variableDeclaration:
                    return new VariableDeclarationStatement(new VariableBuilder(new VariableContextHandler(variableDeclaration)).GetVariable(this));

                // If statement
                case If @if:
                    return new IfStatement(this, @if);
            }

            throw new NotImplementedException(statementContext.GetType().ToString());
        }


        public BlockAction GetBlock(Block block)
        {
            var context = SetScope(Scope.CreateChild());

            var statements = new Statement[block.Statements.Count];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = context.GetStatement(block.Statements[i]);
            
            return new BlockAction(statements);
        }
    }
}