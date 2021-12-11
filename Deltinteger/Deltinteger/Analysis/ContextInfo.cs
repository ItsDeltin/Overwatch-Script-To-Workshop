using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Scopes;
using DS.Analysis.Expressions;
using DS.Analysis.Expressions.Dot;
using DS.Analysis.Expressions.Identifiers;
using DS.Analysis.Statements;
using DS.Analysis.Variables.Builder;
using DS.Analysis.Structure.DataTypes;
using DS.Analysis.Structure.Methods;
using DS.Analysis.Structure.Modules;
using DS.Analysis.Structure.TypeAlias;
using DS.Analysis.Structure.Variables;
using DS.Analysis.Types;
using DS.Analysis.Diagnostics;

namespace DS.Analysis
{
    class ContextInfo
    {
        public DeltinScriptAnalysis Analysis { get; }
        public ScriptFile File { get; }
        public Scope Scope { get; private set; }
        public Scope Getter { get; private set; }
        public IScopeAppender ScopeAppender { get; private set; }
        public IParentElement Parent { get; private set; }
        public ContextKind ContextKind { get; private set; }
        public IEnumerable<string> ModulePath { get; private set; } = new string[0];

        public FileDiagnostics Diagnostics => File.Diagnostics;
        public PostAnalysisOperation PostAnalysisOperations => File.Analysis.PostAnalysisOperations;

        public ContextInfo(DeltinScriptAnalysis analysis, ScriptFile file, Scope scope)
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
            ScopeAppender = other.ScopeAppender;
            Parent = other.Parent;
            ContextKind = other.ContextKind;
            ModulePath = other.ModulePath;
        }


        public ContextInfo ClearTail() => new ContextInfo(this);

        public ContextInfo ClearHead() => new ContextInfo(this);

        public ContextInfo SetSourceExpression(Expression source) => new ContextInfo(this);

        public ContextInfo SetScope(Scope scope) => new ContextInfo(this) { Scope = scope };

        public ContextInfo AddSource(IScopeSource source) => new ContextInfo(this) { Scope = Scope.CreateChild(source) };

        public ContextInfo SetScopeAppender(IScopeAppender appender) => new ContextInfo(this) { ScopeAppender = appender };

        public ContextInfo AddAppendableSource<T>(T appendableSource)
            where T : IScopeSource, IScopeAppender
            => new ContextInfo(this)
            {
                Scope = Scope.CreateChild(appendableSource),
                ScopeAppender = appendableSource
            };

        public ContextInfo SetParent(IParentElement parent) => new ContextInfo(this) { Parent = parent };

        public ContextInfo AppendToModulePath(string moduleName) => new ContextInfo(this) { ModulePath = ModulePath.Append(moduleName) };

        public ContextInfo SetModulePath(IEnumerable<string> path) => new ContextInfo(this) { ModulePath = path };


        public Expression GetExpression(IParseExpression expressionContext)
        {
            switch (expressionContext)
            {
                // Group
                case ExpressionGroup group: return GetExpression(group.Expression);
                // Identifier
                case Identifier identifier: return new IdentifierExpression(this, identifier);
                // True/false
                case BooleanExpression boolean: return new BooleanAction(this, boolean);
                // Number
                case NumberExpression number: return new Number(number.Value);
                // Operator
                case BinaryOperatorExpression op:
                    if (op.IsDotExpression())
                        return new DotExpression(this, op);
                    else
                        break; // todo
                // Unknown
                case MissingElement missingElement:
                    return new UnknownExpression();
            }

            throw new NotImplementedException(expressionContext.GetType().ToString());
        }


        public Statement StatementFromSyntax(IParseStatement syntax)
        {
            switch (syntax)
            {
                // Variable
                case VariableDeclaration variableDeclaration:
                    return new DeclarationStatement(this, new DeclaredVariable(this, new VariableContextHandler(variableDeclaration)));

                // Data Type
                case ClassContext dataTypeDeclaration:
                    return new DeclarationStatement(this, new DeclaredDataType(this, new DataTypeContentProvider(dataTypeDeclaration)));

                // Method
                case FunctionContext methodDeclaration:
                    return new DeclarationStatement(this, new DeclaredMethod(this, new MethodContentProvider(methodDeclaration)));

                // Module
                case ModuleContext moduleDeclaration:
                    return new DeclarationStatement(this, new DeclaredModule(this, new ModuleContentProvider(moduleDeclaration)));

                // Type alias
                case TypeAliasContext typeAlias:
                    return new DeclarationStatement(this, new DeclaredTypeAlias(this, new TypeAliasProvider(typeAlias)));

                // If statement
                case If @if:
                    return new IfStatement(this, @if);

                // Import
                case Import import:
                    return new ImportStatement(this, import);

                // Block
                case Block block:
                    return new BlockStatement(Block(block));

                // Expression
                case ExpressionStatementSyntax expression:
                    return new ExpressionStatement(this, expression);

                // Assignment
                case Assignment assignment:
                    return new AssignmentStatement(this, assignment);
            }
            throw new NotImplementedException(syntax.GetType().ToString());
        }


        public BlockAction Block(Block block, ScopeSource scopeSource = null) => Block(block.Statements.ToArray(), scopeSource);

        public BlockAction Block(IParseStatement[] statementSyntaxes, ScopeSource scopeSource = null)
        {
            scopeSource = scopeSource ?? new ScopeSource();
            var current = AddAppendableSource(scopeSource);

            var statements = new Statement[statementSyntaxes.Length];
            for (int i = 0; i < statements.Length; i++)
            {
                statements[i] = current.StatementFromSyntax(statementSyntaxes[i]);

                // If the statement is providing a ScopeSource,
                var continueWith = statements[i].AddSourceToContext();
                if (continueWith != null)
                    // Then add it to the context.
                    current = current.AddSource(continueWith);
            }

            return new BlockAction(statements, scopeSource);
        }


        public IDisposable Error(string message, DocRange range) => File.Diagnostics.Error(message, range);


        public IGetIdentifier CreateStructuredIdentifier(string name, CodeType[] typeArgs, Func<ScopedElement, bool> predicate) =>
            new GetStructuredIdentifier(name, typeArgs, Parent?.GetIdentifier, GetStructuredIdentifier.PredicateSearch(predicate));

        public IGetIdentifier CreateStructuredIdentifier(string name, Func<ScopedElement, bool> predicate) =>
            CreateStructuredIdentifier(name, null, predicate);
    }
}