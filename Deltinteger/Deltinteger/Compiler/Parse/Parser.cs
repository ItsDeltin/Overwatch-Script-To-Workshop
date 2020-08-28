using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Parser
    {
        private readonly static TokenType[] ExpressionTokens = new TokenType[] { TokenType.Number, TokenType.Identifier, TokenType.True, TokenType.False };

        public Lexer Lexer { get; }
        public int Token { get; private set; }
        public Token Current => Lexer.Tokens[Token];
        public Token CurrentOrLast => Lexer.Tokens[Math.Min(Token, Lexer.Tokens.Count)];
        public TokenType Kind => Current.TokenType;
        public int TokenCount => Lexer.Tokens.Count;
        public bool IsFinished => Token >= Lexer.Tokens.Count;
        public List<RuleContext> Rules { get; } = new List<RuleContext>();

        public Stack<OperatorInfo> Operators { get; } = new Stack<OperatorInfo>();
        public Stack<IParseExpression> Operands { get; } = new Stack<IParseExpression>();
        public List<IParserError> Errors { get; } = new List<IParserError>();

        private int LookaheadDepth = 0;

        public Parser(Lexer lexer)
        {
            Lexer = lexer;
        }

        public Token Consume()
        {
            if (Token < Lexer.Tokens.Count)
            {
                Token++;
                return Lexer.Tokens[Token - 1];
            }
            return null;
        }

        void AddError(IParserError error)
        {
            // If we are currently doing a lookahead, don't add errors.
            if (LookaheadDepth != 0) return;

            // If the error being added overlaps other errors, do not add it.
            foreach (var existing in Errors)
                if (error.Range.DoOverlap(existing.Range))
                    return;
                
            // Error is good to go.
            Errors.Add(error);
        }

        void Lookahead(Action action)
        {
            LookaheadDepth++;
            int position = Token;
            action();
            Token = position;
            LookaheadDepth--;
        }

        T Lookahead<T>(Func<T> action)
        {
            T result = default(T);
            Lookahead(() => {
                result = action.Invoke();
            });
            return result;
        }

        bool Is(TokenType type) => !IsFinished && Kind == type;

        /// <summary>If the current token's type is equal to the specified type in the 'type' parameter,
        /// advance then return true. Otherwise, error then return false.</summary>
        /// <param name="type">The expected token type.</param>
        Token ParseExpected(TokenType type)
        {
            if (Kind == type)
                return Consume();
            AddError(ErrorExpected(type));
            return null;
        }

        Token ParseExpected(params TokenType[] types)
        {
            if (types.Contains(Kind))
                return Consume();
            AddError(ErrorExpected(types));
            return null;
        }

        /// <summary>If the current token's type is equal to the specified type in the 'type' parameter,
        /// the out parameter 'token' will be non-null and 'true' is returned. Otherwise, 'token' will be
        /// null and 'false' is returned.</summary>
        /// <param name="type">The expected token type.</param>
        /// <param name="token">The receieved token.</param>
        /// <returns>True if the current token's type matches 'type', false otherwise.</returns>
        bool ParseExpected(TokenType type, out Token token)
        {
            if (Kind == type)
            {
                token = Consume();
                return true;
            }
            AddError(ErrorExpected(type));
            token = null;
            return false;
        }

        IParserError ErrorExpected(params TokenType[] types) => new ExpectedTokenError(CurrentOrLast.Range, types);

        /// <summary></summary>
        /// <returns></returns>
        Token ParseOptional(TokenType type)
        {
            if (Is(type))
                return Consume();
            return null;
        }

        /// <summary></summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool ParseOptional(TokenType type, out Token result)
        {
            if (Is(type))
            {
                result = Consume();
                return true;
            }
            result = null;
            return false;
        }

        Token ParseSemicolon() => ParseExpected(TokenType.Semicolon);
        Token ParseOptionalSemicolon() => ParseOptional(TokenType.Semicolon);

        // Operators
        void PushOperator(OperatorInfo op)
        {
            while (CompilerOperator.Compare(Operators.Peek().Operator, op.Operator))
                PopOperator();
            Operators.Push(op);
        }

        void PopOperator()
        {
            var op = Operators.Pop();
            if (op.Type == OperatorType.Binary)
            {
                // Binary
                var right = Operands.Pop();
                var left = Operands.Pop();
                Operands.Push(new BinaryOperatorExpression(left, right, op));
            }
            else if (op.Type == OperatorType.Unary)
            {
                // Unary
                var value = Operands.Pop();
                Operands.Push(new UnaryOperatorExpression(value, op));
            }
            else
            {
                // Ternary
                var op2 = Operators.Pop();
                var rhs = Operands.Pop();
                var middle = Operands.Pop();
                var lhs = Operands.Pop();
                Operands.Push(new TernaryExpression(lhs, middle, rhs));
            }
        }

        bool TryParseBinaryOperator(out OperatorInfo operatorInfo)
        {
            foreach (var op in CompilerOperator.BinaryOperators)
                if (ParseOptional(op.RelatedToken, out Token token))
                {
                    operatorInfo = new OperatorInfo(op, token);
                    return true;
                }
            
            operatorInfo = null;
            return false;
        }

        // Expressions
        /// <summary>Parses the current expression. In most cases, 'GetContainExpression' should be called instead.</summary>
        /// <returns>The resulting expression.</returns>
        public IParseExpression GetNextExpression()
        {
            switch (Kind)
            {
                // Booleans
                case TokenType.True: return new BooleanExpression(Consume(), true);
                case TokenType.False: return new BooleanExpression(Consume(), false);
                // Numbers
                case TokenType.Number: return new NumberExpression(Consume());
                // Strings
                case TokenType.String: return new StringExpression(Consume());
                // Functions and identifiers
                case TokenType.Identifier: return IdentifierOrFunction();
                // Unknown node
                default:
                    AddError(new InvalidExpressionTerm(Current));
                    return MissingElement();
            }
        }

        /// <summary>Parses an expression and handles operators. The caller must call 'Operands.Pop()', which is also used to get the resulting expression.</summary>
        public void GetExpressionOperatorInfo()
        {
            // Push the expression
            Operands.Push(GetNextExpression());

            // Binary operator
            while (TryParseBinaryOperator(out OperatorInfo op))
            {
                PushOperator(op);
                op.Operator.RhsHandler.Get(this);
            }
            while (Operators.Peek().Precedence > 0)
                PopOperator();
        }

        /// <summary>Contains the operator stack and parses an expression.</summary>
        /// <returns>The resulting expression.</returns>
        public IParseExpression GetContainExpression()
        {
            Operators.Push(OperatorInfo.Sentinel);
            GetExpressionOperatorInfo();
            Operators.Pop();
            return Operands.Pop();
        }

        /// <summary>Parses an identifier or a function.</summary>
        /// <returns>An 'Identifier' or 'FunctionExpression'.</returns>
        public IParseExpression IdentifierOrFunction()
        {
            Token identifier = ParseExpected(TokenType.Identifier);

            // Parse array
            var indices = new List<ArrayIndex>();
            while (ParseOptional(TokenType.SquareBracket_Open, out var left))
            {
                var expression = GetContainExpression();
                var right = ParseExpected(TokenType.SquareBracket_Close);
                indices.Add(new ArrayIndex(expression, left, right));
            }

            // Match parentheses start.
            if (indices.Count != 0 || ParseOptional(TokenType.Parentheses_Open) == null)
                return MakeIdentifier(identifier, indices);
            else
            {
                // Parse parameters.
                var values = Is(TokenType.Parentheses_Close) ? new List<FunctionParameter>() : ParseParameterValues();
                ParseExpected(TokenType.Parentheses_Close);

                // Return function
                return new FunctionExpression(identifier, values);
            }
        }

        /// <summary>Parses the inner parameter values of a function.</summary>
        /// <returns></returns>
        public List<FunctionParameter> ParseParameterValues()
        {
            // Get the parameters.
            List<FunctionParameter> values = new List<FunctionParameter>();
            bool getValues = true;
            while (getValues)
            {
                var expression = GetContainExpression();
                getValues = ParseOptional(TokenType.Comma, out Token comma);
                values.Add(new FunctionParameter(expression, comma));
            }

            return values;
        }

        // Statements and blocks
        bool TryParseStatementOrBlock(out IParseStatement statement)
        {
            // Parse a block if the current token is a curly bracket.
            if (Is(TokenType.CurlyBracket_Open))
            {
                statement = ParseBlock();
                return true;
            }

            // If the next token is a block finisher, return false.
            if (Is(TokenType.CurlyBracket_Close))
            {
                statement = null;
                return false;
            }

            // Parse the next statement.
            statement = ParseStatement();
            return !(statement is ExpressionStatement exprStatement && exprStatement.Expression is MissingElement);
        }

        /// <summary>Parses a block.</summary>
        /// <returns>The resulting block.</returns>
        Block ParseBlock()
        {
            // Open block
            ParseExpected(TokenType.CurlyBracket_Open);

            // List of statements in the block.
            var statements = new List<IParseStatement>();
            while (TryParseStatementOrBlock(out var statement)) statements.Add(statement);

            // Create the block.
            var result = new Block(statements);

            // Close block
            ParseExpected(TokenType.CurlyBracket_Close);

            // Done
            return result;
        }

        IParseStatement ParseStatement()
        {
            switch (Kind)
            {
                // Block
                case TokenType.CurlyBracket_Open: return ParseBlock();
                // Continue and break
                case TokenType.Break: return ParseBreak();
                case TokenType.Continue: return ParseContinue();
                // Return
                case TokenType.Return: return ParseReturn();
                // If
                case TokenType.If:
                case TokenType.Else: // Error handling in the case of an else with no if.
                    return ParseIf();
                case TokenType.For: return ParseFor();
            }

            if (IsDeclaration()) return ParseDeclaration();
            return ParseExpressionStatement();
        }

        IParseStatement ParseExpressionStatement()
        {
            var expression = GetContainExpression();

            // Default if the current token is a semicolon.
            if (ParseOptional(TokenType.Semicolon))
                return ExpressionStatement(expression);
            
            // Assignment
            if (Kind.IsAssignmentOperator())
            {
                Token assignmentToken = Consume();

                // Get the value.
                var value = GetContainExpression();

                // Statement finished.
                ParseSemicolon();
                return new Assignment(expression, assignmentToken, value);
            }

            // Increment
            if (ParseOptional(TokenType.PlusPlus))
                return new Increment(expression, false);
            
            // Decrement
            if (ParseOptional(TokenType.MinusMinus))
                return new Increment(expression, true);
            
            // Default
            var result = ExpressionStatement(expression);
            ParseOptionalSemicolon();
            return result;
        }

        Break ParseBreak()
        {
            ParseExpected(TokenType.Break);
            ParseSemicolon();
            return new Break();
        }

        Continue ParseContinue()
        {
            ParseExpected(TokenType.Continue);
            ParseSemicolon();
            return new Continue();
        }

        Return ParseReturn()
        {
            var returnToken = ParseExpected(TokenType.Return);
            var expression = GetContainExpression();
            ParseSemicolon();
            return new Return(returnToken, expression);
        }

        If ParseIf()
        {
            ParseExpected(TokenType.If);

            // Parse the expression.
            ParseExpected(TokenType.Parentheses_Open);
            var expression = GetContainExpression();
            ParseExpected(TokenType.Parentheses_Close);

            // The if's statement.
            var statement = ParseStatement();

            // The list of else-ifs.
            var elifs = new List<ElseIf>();

            // The else block.
            Else els = null;

            // Get the else-ifs and elses.
            while (ParseOptional(TokenType.Else))
            {
                // Else if
                if (ParseOptional(TokenType.If))
                {
                    // Parse the else-if's expression.
                    ParseExpected(TokenType.Parentheses_Open);
                    var elifExpr = GetContainExpression();
                    ParseExpected(TokenType.Parentheses_Close);

                    // Parse the else-if's statement.
                    var elifStatement = ParseStatement();

                    elifs.Add(new ElseIf(elifExpr, elifStatement));
                }
                // Else
                else
                {
                    // Parse the else's block.
                    var elseStatement  = ParseStatement();
                    els = new Else(elseStatement);

                    // Since this is an else, break since we don't need any more else-if or elses.
                    break;
                }
            }

            return new If(expression, statement, elifs, els);
        }

        For ParseFor()
        {
            ParseExpected(TokenType.For);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the initializer.
            IParseStatement initializer = null;
            if (!ParseOptionalSemicolon())
            {
                initializer = ParseStatement();
                ParseSemicolon();
            }
            
            // Get the condition.
            IParseExpression condition = null;
            if (!ParseOptionalSemicolon())
            {
                condition = GetContainExpression();
                ParseSemicolon();
            }

            // Get the iterator.
            IParseStatement iterator = null;
            if (!ParseOptionalSemicolon())
            {
                iterator = ParseStatement();
                ParseSemicolon();
            }

            // End the for parameters.
            ParseExpected(TokenType.Parentheses_Close);

            // Get the for's statement.
            var statement = ParseStatement();

            // Done
            return new For(initializer, condition, iterator, statement);
        }

        ParseType ParseType()
        {
            // Get the type name.
            var identifier = ParseExpected(TokenType.Identifier, TokenType.Define);

            var typeArgs = new List<ParseType>();

            // Get the type arguments.
            if (ParseOptional(TokenType.LessThan))
            {
                do {
                    typeArgs.Add(ParseType());
                }
                while (ParseOptional(TokenType.Comma));

                ParseExpected(TokenType.GreaterThan);
            }

            // Get the array indices
            int arrayCount = 0;
            while (ParseOptional(TokenType.SquareBracket_Open))
            {
                ParseExpected(TokenType.SquareBracket_Close);
                arrayCount++;
            }
            
            return new ParseType(identifier, typeArgs, arrayCount);
        }

        bool IsDeclaration() => Is(TokenType.Define) || Lookahead(
            () => ParseType().Identifier != null && ParseExpected(TokenType.Identifier) && new TokenType[] { TokenType.Semicolon, TokenType.Equal, TokenType.Exclamation }.Contains(Kind)
        );

        Declaration ParseDeclaration()
        {
            var type = ParseType();
            var identifier = ParseExpected(TokenType.Identifier);
            Token assignmentToken = null;
            IParseExpression initialValue = null;

            // Initial value
            if (Kind.IsAssignmentOperator())
            {
                assignmentToken = Consume();

                // Get the value.
                initialValue = GetContainExpression();
            }

            return new Declaration(type, identifier, assignmentToken, initialValue);
        }

        /// <summary>Parses the root of a file.</summary>
        public void Parse()
        {
            while (TryParseRule(out RuleContext rule)) Rules.Add(rule);
        }

        /// <summary>Parses a rule.</summary>
        /// <param name="context">If Kind is not TokenType.Rule, this out parameter will be null.</param>
        /// <returns>If Kind is not TokenType.Rule, false will be returned. Otherwise, true is returned.</returns>
        public bool TryParseRule(out RuleContext context)
        {
            Token ruleToken = ParseOptional(TokenType.Rule);
            if (!ruleToken)
            {
                context = null;
                return false;
            }

            // Colon
            ParseExpected(TokenType.Colon);

            Token name = ParseExpected(TokenType.String);
            Token order = ParseOptional(TokenType.Number);

            // Get the conditions
            List<IfCondition> conditions = new List<IfCondition>();
            while (TryGetIfStatement(out var condition)) conditions.Add(condition);

            // Get the block.
            TryParseStatementOrBlock(out var statement);

            context = new RuleContext(name, conditions, statement);
            return true;
        }

        /// <summary>Parses an if condition. The block is not included.</summary>
        /// <param name="condition">The resulting condition.</param>
        /// <returns>Returns true if 'Kind' is 'TokenType.If'.</returns>
        bool TryGetIfStatement(out IfCondition condition)
        {
            condition = new IfCondition();

            if (!ParseOptional(TokenType.If, out condition.If))
            {
                condition = null;
                return false;
            }

            condition.LeftParen = ParseExpected(TokenType.Parentheses_Open);
            condition.Expression = GetContainExpression();
            condition.RightParen = ParseExpected(TokenType.Parentheses_Close);
            return true;
        }

        Identifier MakeIdentifier(Token identifier, List<ArrayIndex> indices) => new Identifier(identifier, indices);
        MissingElement MissingElement() => new MissingElement();
        IParseStatement ExpressionStatement(IParseExpression expression)
        {
            if (expression is IParseStatement statement) return statement;
            return new ExpressionStatement(expression);
        }
    }
}