using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Parser
    {
        public Lexer Lexer { get; }
        public int Token { get; private set; }
        public Token Current => Lexer.Tokens[Token];
        public Token CurrentOrLast => Lexer.Tokens[Math.Min(Token, Lexer.Tokens.Count - 1)];
        public TokenType Kind => IsFinished ? TokenType.EOF : Current.TokenType;
        public int TokenCount => Lexer.Tokens.Count;
        public bool IsFinished => Token >= Lexer.Tokens.Count;

        public Stack<OperatorInfo> Operators { get; } = new Stack<OperatorInfo>();
        public Stack<IParseExpression> Operands { get; } = new Stack<IParseExpression>();
        public Stack<TokenCapture> TokenCaptureStack { get; } = new Stack<TokenCapture>();
        public List<TokenCapture> NodeCaptures { get; } = new List<TokenCapture>();
        public List<IParserError> Errors { get; } = new List<IParserError>();
        private readonly RootContext _last;
        private readonly IncrementInfo _incrementInfo;

        private int LookaheadDepth = 0;

        public Parser(Lexer lexer, RootContext last = null, IncrementInfo incrementInfo = null)
        {
            Lexer = lexer;
            _last = last;
            _incrementInfo = incrementInfo;
        }

        Token Consume()
        {
            if (Token < Lexer.Tokens.Count)
            {
                Token++;
                return Lexer.Tokens[Token - 1];
            }
            return null;
        }

        void StartTokenCapture()
        {
            if (LookaheadDepth == 0)
                TokenCaptureStack.Push(new TokenCapture(Token));
        }

        T EndTokenCapture<T>(T node)
        {
            if (LookaheadDepth == 0)
            {
                var capture = TokenCaptureStack.Pop();
                capture.Finish(Token, node);
                if (capture.IsValid) NodeCaptures.Add(capture);
            }
            return node;
        }

        /// <summary>Gets an existing parsing tree at the current position.</summary>
        /// <param name="node">The resulting node.</param>
        /// <typeparam name="T">The expected node type.</typeparam>
        /// <returns>A boolean determining whether a compatible node was found.</returns>
        bool GetIncrementalNode<T>(out T node)
        {
            // If the last syntax tree was not provided or the current token is inside the change range, return null.
            if (_last == null || _incrementInfo == null || (Token >= _incrementInfo.ChangeStart && Token <= _incrementInfo.ChangeEnd))
            {
                node = default(T);
                return false;
            }

            // Iterate through each cached node.
            foreach (var capture in _last.NodeCaptures) 
                // If the node's type is equal to the expected node type
                // and the cached node's start token index is equal to the current token's index (adjusted for token difference if the current token is past the change position) 
                if (capture.Node.GetType() == typeof(T) && capture.StartToken + (Token > _incrementInfo.ChangeStart ? _incrementInfo.Delta : 0) == Token)
                {
                    // Then return the node then advance by the number of tokens in the cached node.
                    node = (T)capture.Node;
                    Token += capture.Length;
                    return true;
                }
            
            // No matching node was found.
            node = default(T);
            return false;
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
            if (Is(type))
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
        public IParseExpression GetExpressionWithArray()
        {
            IParseExpression expression = GetSubExpression();

            // Get the array index.
            while (ParseOptional(TokenType.SquareBracket_Open))
            {
                IParseExpression index = GetContainExpression();
                // End the closing square bracket.
                ParseExpected(TokenType.SquareBracket_Close);
                // Update the expression.
                expression = new ValueInArray(expression, index);
            }
            return expression;
        }

        IParseExpression GetSubExpression()
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
                // Null
                case TokenType.Null: return new NullExpression(Consume());
                // This
                case TokenType.This: return new ThisExpression(Consume());
                // Functions and identifiers
                case TokenType.Identifier: return IdentifierOrFunction();
                // New
                case TokenType.New: return ParseNew();
                // Array
                case TokenType.SquareBracket_Open: return ParseCreateArray();
                // Expression group
                case TokenType.Parentheses_Open: return ParseGroup();
                // Type cast
                case TokenType.LessThan:
                    // Make sure this is actually a type cast and not an operator.
                    if (IsTypeCast()) return ParseTypeCast();
                    goto default;
                // Unknown node
                default:
                    AddError(new InvalidExpressionTerm(CurrentOrLast));
                    return MissingElement();
            }
        }

        /// <summary>Parses an expression and handles operators. The caller must call 'Operands.Pop()', which is also used to get the resulting expression.</summary>
        void GetExpressionWithOperators()
        {
            // Push the expression
            Operands.Push(GetExpressionWithArray());

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
            GetExpressionWithOperators();
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
                var values = ParseParameterValuesIfNotClosing();
                ParseExpected(TokenType.Parentheses_Close);

                // Return function
                return new FunctionExpression(identifier, values);
            }
        }

        public List<ParameterValue> ParseParameterValuesIfNotClosing() => Is(TokenType.Parentheses_Close) ? new List<ParameterValue>() : ParseParameterValues();

        /// <summary>Parses the inner parameter values of a function.</summary>
        /// <returns></returns>
        public List<ParameterValue> ParseParameterValues()
        {
            // Get the parameters.
            List<ParameterValue> values = new List<ParameterValue>();
            bool getValues = true;
            while (getValues)
            {
                var expression = GetContainExpression();
                getValues = ParseOptional(TokenType.Comma, out Token comma);
                values.Add(new ParameterValue(expression, comma));
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
            StartTokenCapture();
            if (GetIncrementalNode(out Block block)) return EndTokenCapture(block);

            // Open block
            ParseExpected(TokenType.CurlyBracket_Open);

            // List of statements in the block.
            var statements = new List<IParseStatement>();
            while (TryParseStatementOrBlock(out var statement)) statements.Add(statement);

            // Close block
            ParseExpected(TokenType.CurlyBracket_Close);

            // Create the block.
            var result = new Block(statements);

            EndTokenCapture(result);

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
                case TokenType.While: return ParseWhile();
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
            StartTokenCapture();
            if (GetIncrementalNode(out Return ret)) return EndTokenCapture(ret);

            var returnToken = ParseExpected(TokenType.Return);
            var expression = GetContainExpression();
            ParseSemicolon();
            return EndTokenCapture(new Return(returnToken, expression));
        }

        If ParseIf()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out If @if)) return EndTokenCapture(@if);

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

            return EndTokenCapture(new If(expression, statement, elifs, els));
        }

        For ParseFor()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out For @for)) return EndTokenCapture(@for);

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
            return EndTokenCapture(new For(initializer, condition, iterator, statement));
        }

        While ParseWhile()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out While node)) return EndTokenCapture(node);

            ParseExpected(TokenType.While);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the condition.
            var condition = GetContainExpression();

            // End the while's condition closing parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            // Get the while's statement.
            var statement = ParseStatement();

            // Done
            return EndTokenCapture(new While(condition, statement));
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

        bool IsDeclaration() => Lookahead(() => {
            ParseAttributes();
            return ParseType().LookaheadValid && ParseExpected(TokenType.Identifier) && (
                // This is a declaration if the following token is:
                Is(TokenType.Semicolon) ||   // End declaration statement.
                Is(TokenType.Equal) ||       // Initial value.
                Is(TokenType.Exclamation) || // Extended collection marker.
                Is(TokenType.Number) ||      // Assigned workshop ID.
                Is(TokenType.Colon) ||       // Macro variable value.
                IsFinished                   // EOF was reached.
            );
        });

        bool IsFunctionDeclaration() => Lookahead(() => {
            ParseAttributes();
            return ParseType().LookaheadValid && ParseExpected(TokenType.Identifier) && ParseExpected(TokenType.Parentheses_Open);
        });

        /// <summary>Determines if the current context is a type cast.</summary>
        bool IsTypeCast()
        {
            if (!Is(TokenType.LessThan))
                return false;
            
            return Lookahead(() => {
                // Consume the less-than token.
                Consume();

                // Parse the type.
                return Is(TokenType.GreaterThan) || (ParseType().LookaheadValid && Is(TokenType.GreaterThan));
            });
        }

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
        NewExpression ParseNew()
        {
            ParseExpected(TokenType.New);

            // Parse the class identifier.
            var identifier = ParseExpected(TokenType.Identifier);
            
            // Start the parentheses.
            ParseExpected(TokenType.Parentheses_Open);

            // Parse the parameters.
            List<ParameterValue> parameterValues = ParseParameterValuesIfNotClosing();

            // End the parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            return new NewExpression(identifier, parameterValues);
        }

        CreateArray ParseCreateArray()
        {
            // Start the array creation.
            Token left = ParseExpected(TokenType.Parentheses_Open);

            // Create the list that stores the parsed values.
            List<ParameterValue> values = null;

            if (!Is(TokenType.SquareBracket_Close))
                values = ParseParameterValues();
            else
                values = new List<ParameterValue>();

            // End the array creation.
            Token right = ParseExpected(TokenType.Parentheses_Close);

            return new CreateArray(values, left, right);
        }

        ExpressionGroup ParseGroup()
        {
            // Start the parentheses.
            var left = ParseExpected(TokenType.Parentheses_Open);
            
            // Get the expression.
            var expression = GetContainExpression();

            // End the parentheses.
            var right = ParseExpected(TokenType.Parentheses_Close);

            return new ExpressionGroup(expression, left, right);
        }

        TypeCast ParseTypeCast()
        {
            // Parse the opening angled bracket.
            ParseExpected(TokenType.LessThan);
            
            // Parse the type.
            var type = ParseType();

            // Parse the closing angled bracket.
            ParseExpected(TokenType.GreaterThan);

            // Parse the expression.
            var expression = GetContainExpression();

            // Done.
            return new TypeCast(type, expression);
        }

        /// <summary>Parses the root of a file.</summary>
        public RootContext Parse()
        {
            var context = new RootContext();
            while (!IsFinished) ParseScriptRootElement(context);
            context.NodeCaptures = NodeCaptures;
            return context;
        }

        /// <summary>Parses a single import, rule, variable, class, etc.</summary>
        /// <returns>Determines wether an element was parsed.</returns>
        void ParseScriptRootElement(RootContext context)
        {
            // Return false if the EOF was reached.
            switch (Kind)
            {
                // Rule
                case TokenType.Rule:
                    context.Rules.Add(ParseRule());
                    break;
                
                // Class
                case TokenType.Class:
                    context.Classes.Add(ParseClass());
                    break;
                
                // Others
                default:
                    // Variable declaration
                    if (IsDeclaration()) context.RuleLevelVariables.Add(ParseDeclaration());
                    // Function declaration
                    else if (IsFunctionDeclaration()) context.Functions.Add(ParseFunctionDeclaration());
                    // Unknown
                    else
                    {
                        // TODO: error
                        Consume();
                    }
                    break;
            }
        }

        /// <summary>Parses a rule.</summary>
        /// <param name="context">If Kind is not TokenType.Rule, this out parameter will be null.</param>
        /// <returns>If Kind is not TokenType.Rule, false will be returned. Otherwise, true is returned.</returns>
        RuleContext ParseRule()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out RuleContext rule)) return EndTokenCapture(rule);

            Token ruleToken = ParseExpected(TokenType.Rule);

            // Colon
            ParseExpected(TokenType.Colon);

            Token name = ParseExpected(TokenType.String);
            Token order = ParseOptional(TokenType.Number);

            // Get the conditions
            List<IfCondition> conditions = new List<IfCondition>();
            while (TryGetIfStatement(out var condition)) conditions.Add(condition);

            // Get the block.
            TryParseStatementOrBlock(out var statement);

            return EndTokenCapture(new RuleContext(name, conditions, statement));
        }

        /// <summary>Parses a class.</summary>
        ClassContext ParseClass()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out ClassContext @class)) return EndTokenCapture(@class);

            ParseExpected(TokenType.Class);
            var identifier = ParseExpected(TokenType.Identifier);
            ParseExpected(TokenType.CurlyBracket_Open);

            ClassContext context = new ClassContext(identifier);

            // Get the class elements.
            while(!Is(TokenType.CurlyBracket_Close) && !IsFinished)
            {
                if (IsFunctionDeclaration()) context.Functions.Add(ParseFunctionDeclaration());
                else if (IsDeclaration()) context.Variables.Add(ParseDeclaration());
                else break;
            }
            ParseExpected(TokenType.CurlyBracket_Close);

            return EndTokenCapture(context);
        }

        /// <summary>Parses a function or parametered macro.</summary>
        FunctionContext ParseFunctionDeclaration()
        {
            // Parse the accessor and other attributes.
            var attributes = ParseAttributes();

            // If the return type is void, don't parse the type.
            ParseType type = null;
            if (!ParseOptional(TokenType.Void))
                type = ParseType();
            
            // Get the identifier.
            var identifier = ParseExpected(TokenType.Identifier);
            
            // Start the parameter list.
            ParseExpected(TokenType.Parentheses_Open);

            // Get the parameters.
            var parameters = new List<Declaration>();
            if (!Is(TokenType.Parentheses_Close))
            {
                do {
                    parameters.Add(ParseDeclaration());
                }
                while (ParseOptional(TokenType.Comma));
            }

            // End the parameter list.
            ParseExpected(TokenType.Parentheses_Close);

            // Macro
            if (ParseOptional(TokenType.Colon))
            {
                // Get the macro's value.
                var value = GetContainExpression();
                ParseSemicolon();
                return new FunctionContext(attributes, type, identifier, parameters, value);
            }
            // Normal function
            else
            {
                // Get the function's block.
                Block block = ParseBlock();
                return new FunctionContext(attributes, type, identifier, parameters, block);
            }
        }

        /// <summary>Parses a list of attributes.</summary>
        /// <returns>The resulting attribute tokens.</returns>
        AttributeTokens ParseAttributes()
        {
            AttributeTokens tokens = new AttributeTokens();

            while (true)
            {
                Token token;
                if (ParseOptional(TokenType.Public, out token)) tokens.Public = token;
                else if (ParseOptional(TokenType.Private, out token)) tokens.Private = token;
                else if (ParseOptional(TokenType.Protected, out token)) tokens.Protected = token;
                else if (ParseOptional(TokenType.Static, out token)) tokens.Static = token;
                else if (ParseOptional(TokenType.Override, out token)) tokens.Override = token;
                else if (ParseOptional(TokenType.Virtual, out token)) tokens.Virtual = token;
                else if (ParseOptional(TokenType.Recursive, out token)) tokens.Recursive = token;
                else if (ParseOptional(TokenType.GlobalVar, out token)) tokens.GlobalVar = token;
                else if (ParseOptional(TokenType.PlayerVar, out token)) tokens.PlayerVar = token;
                else break;
                tokens.AllAttributes.Add(token);
            }

            return tokens;
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