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

        public Stack<IOperatorInfo> Operators { get; } = new Stack<IOperatorInfo>();
        public Stack<IParseExpression> Operands { get; } = new Stack<IParseExpression>();
        public Stack<TokenCapture> TokenCaptureStack { get; } = new Stack<TokenCapture>();
        public Stack<int> TokenRangeStart { get; } = new Stack<int>();
        public Stack<bool> TernaryCheck { get; } = new Stack<bool>();
        public Stack<bool> StringCheck { get; } = new Stack<bool>();
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
            StringCheck.Push(false);
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

        T EndTokenCapture<T>(T node) where T: Node
        {
            if (LookaheadDepth == 0)
            {
                var capture = TokenCaptureStack.Pop();
                capture.Finish(Token, node);
                node.Range = new DocRange(TokenAtOrEnd(capture.StartToken).Range.Start, CurrentOrLast.Range.End);
                if (capture.IsValid) NodeCaptures.Add(capture);
            }
            return node;
        }

        void StartNode()
        {
            if (LookaheadDepth == 0)
                TokenRangeStart.Push(Token);
        }

        void StartNodeAtLast()
        {
            if (LookaheadDepth == 0)
                TokenRangeStart.Push(Token - 1);
        }

        T EndNode<T>(T node) where T: INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = new DocRange(Lexer.Tokens[TokenRangeStart.Pop()].Range.Start, CurrentOrLast.Range.End);
            return node;
        }

        T EndNodeWithoutPopping<T>(T node) where T: INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = new DocRange(Lexer.Tokens[TokenRangeStart.Peek()].Range.Start, CurrentOrLast.Range.End);
            return node;
        }

        T EndNodeFrom<T>(T node, DocPos start) where T: INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = start + CurrentOrLast.Range.End;
            return node;
        }

        void PopNodeStack()
        {
            if (LookaheadDepth == 0)
                TokenRangeStart.Pop();
        }

        T Node<T>(Func<T> func) where T: Node
        {
            StartNode();
            T r = func();
            return EndNode(r);
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
                if (capture.Node.GetType() == typeof(T) && 
                    !(capture.StartToken <= _incrementInfo.ChangeEnd && _incrementInfo.ChangeStart <= capture.StartToken + capture.Length) &&
                    capture.StartToken + (Token > _incrementInfo.ChangeStart ? _incrementInfo.Delta : 0) == Token)
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
            
            // Mark the current incremental token as containing an error.
            foreach (var item in TokenCaptureStack)
                item.HasError = true;

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

        bool Is(TokenType type) => Kind == type;

        bool Is(TokenType type, int lookahead) => lookahead >= TokenCount ? type == TokenType.EOF : type == Lexer.Tokens[Token + lookahead].TokenType;

        Token TokenAtOrEnd(int position) => Lexer.Tokens[Math.Min(Lexer.Tokens.Count - 1, position)];

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

        void Unexpected(bool root)
        {
            if ((root || Kind.IsSkippable()) && Kind != TokenType.EOF)
                AddError(new UnexpectedToken(Consume()));
        }

        Token ParseSemicolon(bool parse = true)
        {
            if (parse)
                return ParseExpected(TokenType.Semicolon);
            return null;
        }

        Token ParseOptionalSemicolon(bool parse = true)
        {
            if (parse)
                return ParseOptional(TokenType.Semicolon);
            return null;
        }

        // Operators
        void PushOperator(IOperatorInfo op)
        {
            while (CompilerOperator.Compare(Operators.Peek().Source, op.Source))
                PopOperator();
            Operators.Push(op);
        }

        void PopOperator()
        {
            var iop = Operators.Pop();
            if (iop is OperatorInfo op)
            {
                // Binary
                if (op.Type == OperatorType.Binary)
                {
                    var right = Operands.Pop();
                    var left = Operands.Pop();
                    Operands.Push(new BinaryOperatorExpression(left, right, op));
                }
                // Unary
                else if (op.Type == OperatorType.Unary)
                {
                    var value = Operands.Pop();
                    Operands.Push(new UnaryOperatorExpression(value, op));
                }
                // Extraneous left-hand ternary
                else if (op.Type == OperatorType.TernaryLeft)
                {
                    Operands.Pop();
                    AddError(new MissingTernaryHand(op.Token, false));
                }
                // Ternary
                else if (op.Type == OperatorType.TernaryRight)
                {
                    if (Operators.Peek() is OperatorInfo peek && peek.Type == OperatorType.TernaryLeft)
                    {
                        var op2 = Operators.Pop();
                        var rhs = Operands.Pop();
                        var middle = Operands.Pop();
                        var lhs = Operands.Pop();
                        Operands.Push(new TernaryExpression(lhs, middle, rhs));
                    }
                    // Missing left-hand ?
                    else
                    {
                        Operands.Pop();
                        AddError(new MissingTernaryHand(op.Token, true));
                    }
                }
                else throw new NotImplementedException();
            }
            else if (iop is TypeCastInfo typeCastOp)
            {
                var value = Operands.Pop();
                Operands.Push(EndNodeFrom(new TypeCast(typeCastOp.CastingTo, value), typeCastOp.StartPosition));
            }
        }

        bool TryParseBinaryOperator(out OperatorInfo operatorInfo)
        {
            foreach (var op in CompilerOperator.BinaryOperators)
            {
                // Do not parse '>' if we are in a formatted string and the following token is not an identifier.
                if (op.Operator == ">" && StringCheck.Peek() && !Lexer.Tokens[Token + 1].TokenType.IsStartOfExpression())
                    continue;
                
                // Do not parse right-hand ternary if the top operator is not a left-hand ternary operator.
                if (op.Type == OperatorType.TernaryRight && !TernaryCheck.Peek())
                    continue;

                if (ParseOptional(op.RelatedToken, out Token token))
                {
                    if (op.Type == OperatorType.TernaryLeft)
                        TernaryCheck.Push(true);
                    else if (op.Type == OperatorType.TernaryRight)
                        TernaryCheck.Pop();

                    operatorInfo = new OperatorInfo(op, token);
                    return true;
                }
            }
            
            operatorInfo = null;
            return false;
        }

        OperatorInfo ParseUnaryOperator()
        {
            // Operator !expr
            if (ParseOptional(TokenType.Exclamation, out var not))
                return new OperatorInfo(CompilerOperator.Not, not);
            
            // Operator -expr
            else if (ParseOptional(TokenType.Subtract, out var inverse))
                return new OperatorInfo(CompilerOperator.Inv, inverse);
            
            // No unary found.
            return null;
        }

        // Expressions
        NumberExpression ParseNumber()
        {
            StartNode();

            // Negative number.
            bool negative = ParseOptional(TokenType.Subtract);

            // Get the number.
            Token numberToken = ParseExpected(TokenType.Number);

            // Parse the value.
            double value = 0;
            if (numberToken)
            {
                value = double.Parse(numberToken.Text);
                if (negative) value = -value;
            }

            // Done.
            return EndNode(new NumberExpression(value));
        }

        bool IsNumber() => Is(TokenType.Number) || (Is(TokenType.Subtract) && Is(TokenType.Number, 1));

        /// <summary>Parses the current expression. In most cases, 'GetContainExpression' should be called instead.</summary>
        /// <returns>The resulting expression.</returns>
        public IParseExpression GetExpressionWithArray()
        {
            IParseExpression expression = GetSubExpression();
            return GetArrayAndInvokes(expression);
        }

        public IParseExpression GetArrayAndInvokes(IParseExpression expression)
        {
            while (true)
            {
                // Get the array index.
                if (ParseOptional(TokenType.SquareBracket_Open))
                {
                    IParseExpression index = GetContainExpression();
                    // End the closing square bracket.
                    ParseExpected(TokenType.SquareBracket_Close);
                    // Update the expression.
                    expression = EndNodeFrom(new ValueInArray(expression, index, Lexer.Tokens[Token - 1]), expression.Range.Start);
                }
                // Invoke
                else if (ParseOptional(TokenType.Parentheses_Open))
                {
                    // Parse parameters.
                    var values = ParseParameterValues();
                    // End the parentheses.
                    ParseExpected(TokenType.Parentheses_Close);
                    // Update the expression.
                    expression = EndNodeFrom(new FunctionExpression(expression, values), expression.Range.Start);
                }
                // No more array indices or invocations.
                else break;
            }
            return expression;
        }

        IParseExpression GetSubExpression()
        {
            switch (Kind)
            {
                // Negative number or unary operator
                case TokenType.Subtract:
                    if (IsNumber()) return ParseNumber();
                    goto case TokenType.Exclamation;

                // Unary operator
                case TokenType.Exclamation:
                    var op = ParseUnaryOperator();
                    PushOperator(op);
                    var value = GetExpressionWithArray();
                    Operands.Push(value);
                    return value;
                
                // Numbers
                case TokenType.Number: return ParseNumber();
                // Booleans
                case TokenType.True: return new BooleanExpression(Consume(), true);
                case TokenType.False: return new BooleanExpression(Consume(), false);
                // Strings
                case TokenType.String: return Node(() => new StringExpression(null, Consume()));
                // Localized strings
                case TokenType.At: return Node(() => new StringExpression(Consume(), ParseExpected(TokenType.String)));
                // Null
                case TokenType.Null: return new NullExpression(Consume());
                // This
                case TokenType.This: return new ThisExpression(Consume());
                // Root
                case TokenType.Root: return new RootExpression(Consume());
                // New
                case TokenType.New: return ParseNew();
                // Array
                case TokenType.SquareBracket_Open: return ParseCreateArray();
                // Type cast
                case TokenType.LessThan:
                    // Make sure this is actually a type cast and not an operator.
                    if (IsTypeCast()) return ParseTypeCast();
                    // Formatted string
                    else if (Is(TokenType.String, 1) || Is(TokenType.At, 1)) return ParseFormattedString();
                    goto default;
                // Other
                default:
                    // Check if this is a lambda before expression group or identifier.
                    if (IsLambda())
                        return ParseLambda();
                    
                    // Functions and identifiers
                    else if (Is(TokenType.Identifier))
                        return Identifier();
                    
                    // Expression group.
                    else if (Is(TokenType.Parentheses_Open))
                        return ParseGroup();

                    // Unknown node
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
        IParseExpression GetContainExpression(bool stringCheck = false)
        {
            StringCheck.Push(stringCheck);
            TernaryCheck.Push(false);

            Operators.Push(OperatorInfo.Sentinel);
            GetExpressionWithOperators();
            Operators.Pop();

            StringCheck.Pop();
            TernaryCheck.Pop();

            return Operands.Pop();
        }

        /// <summary>Parses an identifier or a function.</summary>
        /// <returns>An 'Identifier' or 'FunctionExpression'.</returns>
        public IParseExpression Identifier()
        {
            StartNode();
            Token identifier = ParseExpected(TokenType.Identifier);

            // Parse array
            var indices = new List<ArrayIndex>();
            while (ParseOptional(TokenType.SquareBracket_Open, out var left))
            {
                var expression = GetContainExpression();
                var right = ParseExpected(TokenType.SquareBracket_Close);
                indices.Add(new ArrayIndex(expression, left, right));
            }

            return EndNode(MakeIdentifier(identifier, indices));
        }

        /// <summary>Parses the inner parameter values of a function.</summary>
        /// <returns></returns>
        List<ParameterValue> ParseParameterValues()
        {
            if (Is(TokenType.Parentheses_Close)) return new List<ParameterValue>();
            return ParseDelimitedList(TokenType.Parentheses_Close, IsStartOfExpression, () => {
                // Get the picky parameter name.
                Token pickyName = null;

                // Only get the picky parameter if the next 2 tokens is an identifier then a colon.
                if (Lookahead(() => ParseExpected(TokenType.Identifier) && ParseExpected(TokenType.Colon)))
                {
                    pickyName = ParseExpected(TokenType.Identifier);
                    ParseExpected(TokenType.Colon);
                }

                // Get the expression.
                var expression = GetContainExpression();

                // Add the parameter.
                return new ParameterValue(pickyName, expression);
            });
        }

        List<T> ParseDelimitedList<T>(TokenType terminator, Func<bool> elementDeterminer, Func<T> parseElement) 
        {
            var values = new List<T>();

            while (true)
            {
                if (elementDeterminer())
                {
                    int s = Token;

                    T lastElement = parseElement();
                    values.Add(lastElement);

                    if (ParseOptional(TokenType.Comma, out Token comma))
                    {
                        if (lastElement is IListComma listComma) listComma.NextComma = comma;
                        continue;
                    }

                    // Stop parsing list if this is the terminator.
                    if (Is(terminator))
                        break;
                    
                    // There is a missing comma or terminator.
                    ParseExpected(TokenType.Comma);

                    // No tokens were consumed.
                    if (s == Token)
                    {
                        // If the current token cannot be skipped, stop parsing elements.
                        if (!Kind.IsSkippable())
                            break;
                        // Otherwise, consume the current token then continue.
                        else
                            Consume();
                    }
                    continue;
                }

                if (Is(terminator))
                    break;
                
                // If the current token cannot be skipped, stop parsing elements.
                if (!Kind.IsSkippable())
                    break;
                // Otherwise, consume the current token.
                else
                    Unexpected(false);
            }

            return values;
        }

        List<T> ParseList<T>(TokenType terminator, Func<bool> isElement, Func<T> parseElement)
        {
            var elements = new List<T>();
            while (!Is(terminator))
            {
                if (isElement())
                {
                    int s = Token;

                    // Parse the element.
                    elements.Add(parseElement());

                    // No tokens were consumed.
                    if (s == Token)
                    {
                        // If the current token cannot be skipped, stop parsing elements.
                        if (!Kind.IsSkippable())
                            break;
                        // Otherwise, consume the current token then continue.
                        else
                            Consume();
                    }
                }
                else
                {
                    // If the current token cannot be skipped, stop parsing elements.
                    if (!Kind.IsSkippable())
                        break;
                    // Otherwise, consume the current token.
                    else
                        Unexpected(false);
                }
            }
            return elements;
        }

        /// <summary>Parses a block.</summary>
        /// <returns>The resulting block.</returns>
        Block ParseBlock()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out Block block)) return EndTokenCapture(block);

            // Open block
            ParseExpected(TokenType.CurlyBracket_Open);

            // List of statements in the block
            var statements = ParseList(TokenType.CurlyBracket_Close, () => Kind.IsStartOfStatement(), () => ParseStatement());

            // Close block
            ParseExpected(TokenType.CurlyBracket_Close);

            // Create the block
            var result = new Block(statements);

            EndTokenCapture(result);

            // Done
            return result;
        }

        IParseStatement ParseStatement(bool parseSemicolon = true)
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
                // Switch
                case TokenType.Switch: return ParseSwitch();
                // Loops
                case TokenType.For: return ParseFor();
                case TokenType.While: return ParseWhile();
                case TokenType.Foreach: return ParseForeach();
                // Delete
                case TokenType.Delete: return ParseDelete();
            }

            if (IsDeclaration(false)) return ParseDeclaration(parseSemicolon);
            return ParseExpressionStatement(parseSemicolon);
        }

        IParseStatement ParseExpressionStatement(bool parseSemicolon)
        {
            StartNode();
            var comment = ParseOptional(TokenType.ActionComment);
            var expression = GetContainExpression();

            // Default if the current token is a semicolon.
            if (ParseOptionalSemicolon(parseSemicolon))
            {
                PopNodeStack();
                return ExpressionStatement(expression, comment);
            }
            
            IParseStatement result = null;
            
            // Assignment
            if (Kind.IsAssignmentOperator())
            {
                Token assignmentToken = Consume();

                // Get the value.
                var value = GetContainExpression();
                result = new Assignment(expression, assignmentToken, value, comment);
            }

            // Increment
            if (ParseOptional(TokenType.PlusPlus))
                result = new Increment(expression, false);
            
            // Decrement
            if (ParseOptional(TokenType.MinusMinus))
                result = new Increment(expression, true);
            
            // Statement found.
            if (result != null)
            {
                EndNode((Node)result);
                ParseSemicolon(parseSemicolon);
                return result;
            }
            // Default
            else
            {
                PopNodeStack();
                result = ExpressionStatement(expression, comment);
                ParseSemicolon(parseSemicolon);
                return result;
            }
        }

        Break ParseBreak()
        {
            StartNode();
            ParseExpected(TokenType.Break);
            var result = EndNode(new Break());
            ParseSemicolon();
            return result;
        }

        Continue ParseContinue()
        {
            StartNode();
            ParseExpected(TokenType.Continue);
            var result = EndNode(new Continue());
            ParseSemicolon();
            return result;
        }

        Return ParseReturn()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out Return ret)) return EndTokenCapture(ret);

            var returnToken = ParseExpected(TokenType.Return);
            IParseExpression expression = null;
            // Get the value being returned if the next token is not a semicolon.
            if (!Is(TokenType.Semicolon)) expression = GetContainExpression();
            // Parse the semicolon.
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

        Switch ParseSwitch()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out Switch @switch)) return EndTokenCapture(@switch);
            
            // Start the switch.
            ParseExpected(TokenType.Switch);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the switch's expression.
            var expression = GetContainExpression();

            ParseExpected(TokenType.Parentheses_Close);
            ParseExpected(TokenType.CurlyBracket_Open);

            // Get the statements.
            var statements = new List<IParseStatement>();
            while (!Is(TokenType.CurlyBracket_Close))
            {
                // Case
                if (ParseOptional(TokenType.Case, out var caseToken))
                {
                    StartNodeAtLast();
                    // Get the case's value.
                    var caseValue = GetContainExpression();
                    ParseExpected(TokenType.Colon);

                    // Add the case statement.
                    statements.Add(EndNode(new SwitchCase(caseToken, caseValue)));
                }
                // Default
                else if (ParseOptional(TokenType.Default, out var defaultToken))
                {
                    StartNodeAtLast();
                    ParseExpected(TokenType.Colon);

                    // Add the default statement.
                    statements.Add(EndNode(new SwitchCase(defaultToken)));
                }
                // Normal statement
                else statements.Add(ParseStatement());
            }

            ParseExpected(TokenType.CurlyBracket_Close);
            return EndTokenCapture(new Switch(expression, statements));
        }

        For ParseFor()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out For @for)) return EndTokenCapture(@for);

            ParseExpected(TokenType.For);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the initializer.
            IParseStatement initializer = null;
            Token initializerToken = ParseOptionalSemicolon();
            if (!initializerToken)
            {
                initializer = ParseStatement(false);
                initializerToken = ParseSemicolon();
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
            if (!Is(TokenType.Parentheses_Close))
                iterator = ParseStatement(false);

            // End the for parameters.
            ParseExpected(TokenType.Parentheses_Close);

            // Get the for's statement.
            var statement = ParseStatement();

            // Done
            return EndTokenCapture(new For(initializer, condition, iterator, statement, initializerToken));
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

        Foreach ParseForeach()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out Foreach node)) return EndTokenCapture(node);

            ParseExpected(TokenType.Foreach);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the variable.
            var type = ParseType();
            var identifier = ParseExpected(TokenType.Identifier);

            // Parse in.
            ParseExpected(TokenType.In);

            // Parse the value.
            var value = GetContainExpression();

            ParseExpected(TokenType.Parentheses_Close);

            // Get the foreach's statement.
            var statement = ParseStatement();

            // Done
            return EndTokenCapture(new Foreach(type, identifier, value, statement));
        }

        Delete ParseDelete()
        {
            StartNode();
            ParseExpected(TokenType.Delete);
            
            // Get the expression
            var expression = GetContainExpression();

            ParseExpected(TokenType.Semicolon);
            return EndNode(new Delete(expression));
        }

        IParseType ParseType()
        {
            StartNode();

            if (ParseOptional(TokenType.Void, out var @void))
                return EndNode(new ParseType(@void));

            // If we parse a parentheses, we can assume this is a lambda type.
            if (!ParseOptional(TokenType.Parentheses_Open))
            {
                // No parentheses found.
                // This is either a normal type or a lambda with a single parameter.

                // Get the type name.
                var identifier = ParseExpected(TokenType.Identifier, TokenType.Define);

                // No arrow, parse the type normally.
                var typeArgs = new List<IParseType>();

                // Get the type arguments.
                if (ParseOptional(TokenType.LessThan))
                {
                    do typeArgs.Add(ParseType());
                    while (ParseOptional(TokenType.Comma));

                    ParseExpected(TokenType.GreaterThan);
                }

                // Get the array indices
                int arrayCount = ParseTypeArray();
                
                IParseType result = EndNodeWithoutPopping(new ParseType(identifier, typeArgs, arrayCount));

                // Get pipe
                while (ParseOptional(TokenType.Pipe))
                {
                    IParseType right = ParseType();
                    result = EndNodeWithoutPopping(new PipeTypeContext(result, right));
                }

                // If we parse an arrow, this is a lambda type with a single parameter.
                if (!ParseOptional(TokenType.Arrow, out Token arrow))
                {
                    PopNodeStack();
                    return result;
                }
                // Lambda type
                else
                {
                    // Parse the lambda's return type.
                    var returnType = ParseType();
                    return EndNode(new LambdaType(result, returnType, arrow));
                }
            }
            else // This is a lambda with parenthesized parameters.
            {
                // Get the parameter types.
                var parameterTypes = ParseDelimitedList(TokenType.Parentheses_Close, () => Kind.IsStartOfType(), () => ParseType());

                // End the type list parentheses.
                ParseExpected(TokenType.Parentheses_Close);

                Token arrow = null;
                bool isLambda;

                // If an arrow is required, parse the expected arrow.
                if (parameterTypes.Count != 1)
                {
                    arrow = ParseExpected(TokenType.Arrow);
                    isLambda = true;
                }
                // Otherwise, parse the optional arrow.
                else isLambda = ParseOptional(TokenType.Arrow, out arrow);

                // Parse lambda type.
                if (isLambda)
                {
                    // Get the return type.
                    var returnType = ParseType();

                    // Done.
                    return EndNode(new LambdaType(parameterTypes, returnType, arrow));
                }
                else
                {
                    IParseType child = parameterTypes[0];

                    // Get the array indices
                    int arrayCount = ParseTypeArray();

                    // Done.
                    return EndNode(new GroupType(child, arrayCount));
                }
            }
        }

        int ParseTypeArray()
        {
            // Get the array indices
            int arrayCount = 0;
            while (ParseOptional(TokenType.SquareBracket_Open))
            {
                ParseExpected(TokenType.SquareBracket_Close);
                arrayCount++;
            }
            return arrayCount;
        }

        bool IsDeclaration(bool functionDeclaration) => Lookahead(() => {
            ParseAttributes();
            var typeParse = ParseType();
            return typeParse.LookaheadValid && (ParseExpected(TokenType.Identifier) && (
                // This is a declaration if the following token is:
                Is(TokenType.Semicolon) ||   // End declaration statement.
                Is(TokenType.Equal) ||       // Initial value.
                Is(TokenType.Exclamation) || // Extended collection marker.
                Is(TokenType.Number) ||      // Assigned workshop ID.
                Is(TokenType.Colon) ||       // Macro variable value.
                (functionDeclaration && Is(TokenType.Parentheses_Open)) || // Function parameter start.
                IsFinished                   // EOF was reached.
            ));
        });

        bool IsConstructor() => Lookahead(() => {
            ParseAttributes();
            return Is(TokenType.Identifier) && Is(TokenType.Parentheses_Open, 1);
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

        /// <summary>Determines if the current context is a formatted string.</summary>
        bool IsFormattedString() => Is(TokenType.LessThan) && (Is(TokenType.String, 1) || (Is(TokenType.At, 1) && Is(TokenType.String, 2)));

        /// <summary>Determines if this is a lambda operator.</summary>
        bool IsLambda() =>
            // Lambda operator without parameters. An error will be thrown later when the lambda is parsed.
            Is(TokenType.Arrow)
            // Search for arrow.
            || Lookahead(() => {
            // Lambda with parenthesized parameters.
            if (ParseOptional(TokenType.Parentheses_Open))
            {
                // Zero parameters if the next token is a closing parentheses.
                if (ParseOptional(TokenType.Parentheses_Close))
                    // This is a lambda if the following token is an arrow.
                    return ParseExpected(TokenType.Arrow);

                // Parse the parameters.
                do
                {
                    ParseType();
                    ParseOptional(TokenType.Identifier);
                }
                // Keep parsing while a comma is matched.
                while (ParseOptional(TokenType.Comma));

                // Close the lambda parameter group.
                ParseExpected(TokenType.Parentheses_Close);
                // This is a lambda if the following token is an arrow.
                return ParseExpected(TokenType.Arrow);
            }
            // Lambda with a single parameter.
            else
            {
                // A lambda without an opening parentheses has a single parameter.
                ParseExpected(TokenType.Identifier);
                // This is a lambda if the following token is an arrow.
                return ParseExpected(TokenType.Arrow);
            }
        });

        bool IsHook() => Lookahead(() => {
            bool parsedAny = false;
            do parsedAny = ParseExpected(TokenType.Identifier) || parsedAny;
            while (ParseOptional(TokenType.Dot));
            return parsedAny && Is(TokenType.Equal);
        });

        bool IsStartOfParameter() => Is(TokenType.Ref) || Kind.IsStartOfType();

        bool IsStartOfExpression() => Kind.IsStartOfExpression() || Kind.IsBinaryOperator();

        LambdaExpression ParseLambda()
        {
            StartNode();

            // Get the parameters.
            List<LambdaParameter> parameters;

            // Parenthesized parameters.
            if (ParseOptional(TokenType.Parentheses_Open))
            {
                parameters = ParseDelimitedList(TokenType.Parentheses_Close, () => Kind.IsStartOfType(), ParseLambdaParameter);

                // Close the parentheses.
                ParseExpected(TokenType.Parentheses_Close);
            }
            // Single parameter
            else
            {
                parameters = new List<LambdaParameter>();
                parameters.Add(new LambdaParameter(null, ParseExpected(TokenType.Identifier)));
            }

            // Arrow.
            var arrow = ParseExpected(TokenType.Arrow);
            
            // Get the statement.
            var statement = ParseStatement(false);

            // Done.
            return EndNode(new LambdaExpression(parameters, arrow, statement));
        }

        LambdaParameter ParseLambdaParameter()
        {
            StartNode();
            Token identifier;
            var type = ParseType();
            
            // If the parsed type is *definitely* a type, parse an expected identifier.
            if (type.DefinitelyType)
                identifier = ParseExpected(TokenType.Identifier);
            // Otherwise, parse an optional identifier.
            else if (!ParseOptional(TokenType.Identifier, out identifier))
            {
                // If no identifier is parsed, set the identifier to the type's identifier.
                identifier = type.GenericToken;
                // The type is actually the identifier, so set the type to null.
                type = null;
            }

            return EndNode(new LambdaParameter(type, identifier));
        }

        List<VariableDeclaration> ParseParameters()
        {
            var parameters = new List<VariableDeclaration>();
            if (IsStartOfParameter())
            {
                do parameters.Add(ParseDeclaration(false));
                while (ParseOptional(TokenType.Comma));
            }
            return parameters;
        }

        IDeclaration ParseVariableOrFunctionDeclaration()
        {
            StartNode();
            var attributes = ParseAttributes();
            var type = ParseType();
            
            // Get the identifier.
            var identifier = ParseExpected(TokenType.Identifier);
            
            // Function
            if (ParseOptional(TokenType.Parentheses_Open))
            {
                // Get the parameters.
                var parameters = ParseParameters();

                // End the parameter list.
                ParseExpected(TokenType.Parentheses_Close);

                // Macro
                if (ParseOptional(TokenType.Colon))
                {
                    // Get the macro's value.
                    var value = GetContainExpression();
                    ParseSemicolon();
                    return EndNode(new MacroFunctionContext(attributes, type, identifier, parameters, value));
                }
                // Normal function
                else
                {
                    // Get optional subroutine attributes.
                    Token globalvar = null, playervar = null, subroutine = null;

                    // This will parse either the globalvar or playervar tokens.
                    if (ParseOptional(TokenType.GlobalVar, out globalvar) || ParseOptional(TokenType.PlayerVar, out playervar))
                    {
                        // Get the subroutine name.
                        subroutine = ParseExpected(TokenType.String);
                    }
                    // Get the optional subroutine name.
                    else subroutine = ParseOptional(TokenType.String);

                    // Get the function's block.
                    Block block = ParseBlock();
                    return EndNode(new FunctionContext(attributes, type, identifier, parameters, block, globalvar, playervar, subroutine));
                }
            }
            // Variable macro
            else if (ParseOptional(TokenType.Colon))
            {
                // Get the value.
                var macroValue = GetContainExpression();
                ParseSemicolon();
                return EndNode(new MacroVarDeclaration(attributes, type, identifier, macroValue));
            }
            // Variable
            else
            {
                // Parse an optional variable ID or extended collection marker.
                Token id = null, ext = null;
                
                if (!ParseOptional(TokenType.Number, out id))
                    ParseOptional(TokenType.Exclamation, out ext);

                // Get the initial value.
                IParseExpression initialValue = null;
                if (ParseOptional(TokenType.Equal))
                    initialValue = GetContainExpression(); // Get the initial value.

                ParseSemicolon();
                return EndNode(new VariableDeclaration(attributes, type, identifier, initialValue, ext, id));
            }
        }

        VariableDeclaration ParseDeclaration(bool parseSemicolon)
        {
            StartNode();
            var attributes = ParseAttributes();
            var type = ParseType();
            var identifier = ParseExpected(TokenType.Identifier);

            // Parse an optional variable ID or extended collection marker.
            Token id = null, ext = null;
            
            if (!ParseOptional(TokenType.Number, out id))
                ParseOptional(TokenType.Exclamation, out ext);

            // Initial value
            IParseExpression initialValue = null;
            if (ParseOptional(TokenType.Equal))
                // Get the value.
                initialValue = GetContainExpression();
            
            ParseSemicolon(parseSemicolon);
            return EndNode(new VariableDeclaration(attributes, type, identifier, initialValue, ext, id));
        }

        ConstructorContext ParseConstructor()
        {
            var attributes = ParseAttributes();
            var identifier = ParseExpected(TokenType.Identifier);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the parameters.
            var parameters = ParseParameters();

            // End the parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            Token subroutineName = ParseOptional(TokenType.String);

            // Get the constructor's block.
            Block block = ParseBlock();

            return new ConstructorContext(attributes, identifier, parameters, subroutineName, block);
        }

        NewExpression ParseNew()
        {
            StartNode();
            ParseExpected(TokenType.New);

            // Parse the class identifier.
            var identifier = ParseExpected(TokenType.Identifier);
            
            // Start the parentheses.
            ParseExpected(TokenType.Parentheses_Open);

            // Parse the parameters.
            List<ParameterValue> parameterValues = ParseParameterValues();

            // End the parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            return EndNode(new NewExpression(identifier, parameterValues));
        }

        CreateArray ParseCreateArray()
        {
            StartNode();
            // Start the array creation.
            Token left = ParseExpected(TokenType.SquareBracket_Open);

            // Create the list that stores the parsed values.
            List<IParseExpression> values = new List<IParseExpression>();

            // If the next token is not a ], get the array values.
            if (!Is(TokenType.SquareBracket_Close))
                do values.Add(GetContainExpression());
                while (ParseOptional(TokenType.Comma));

            // End the array creation.
            Token right = ParseExpected(TokenType.SquareBracket_Close);

            return EndNode(new CreateArray(values, left, right));
        }

        ExpressionGroup ParseGroup()
        {
            StartNode();
            // Start the parentheses.
            var left = ParseExpected(TokenType.Parentheses_Open);
            
            // Get the expression.
            var expression = GetContainExpression();

            // End the parentheses.
            var right = ParseExpected(TokenType.Parentheses_Close);

            return EndNode(new ExpressionGroup(expression, left, right));
        }

        IParseExpression ParseTypeCast()
        {
            DocPos startPosition = Current.Range.Start;

            // Parse the opening angled bracket.
            ParseExpected(TokenType.LessThan);
            
            // Parse the type.
            var type = ParseType();

            // Parse the closing angled bracket.
            ParseExpected(TokenType.GreaterThan);

            PushOperator(new TypeCastInfo(type, startPosition));

            var value = GetExpressionWithArray();
            Operands.Push(value);
            return value;
        }

        StringExpression ParseFormattedString()
        {
            StartNode();
            ParseExpected(TokenType.LessThan);

            // Get the optional localized token.
            var localized = ParseOptional(TokenType.At);

            // Get the string token.
            var str = ParseExpected(TokenType.String);
            
            // Get the formats.
            var formats = new List<IParseExpression>();
            while (ParseOptional(TokenType.Comma))
                formats.Add(GetContainExpression(true));

            ParseExpected(TokenType.GreaterThan);
            return EndNode(new StringExpression(localized, str, formats));
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
                case TokenType.Disabled:
                    context.Rules.Add(ParseRule());
                    break;
                
                // Class
                case TokenType.Class:
                    context.Classes.Add(ParseClass());
                    break;
                
                // Enum
                case TokenType.Enum:
                    context.Enums.Add(ParseEnum());
                    break;

                // Import
                case TokenType.Import:
                    context.Imports.Add(ParseImport());
                    break;
                
				case TokenType.Operator:
					//TODO: Operators
					break;
                // Others
                default:
                    // Variable declaration
                    if (IsDeclaration(true))
                        context.Declarations.Add(ParseVariableOrFunctionDeclaration());
                    // Hook
                    else if (IsHook())
                        context.Hooks.Add(ParseHook());
                    // Unknown
                    else
                        Unexpected(true);
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

            Token disabled = ParseOptional(TokenType.Disabled);
            Token ruleToken = ParseExpected(TokenType.Rule);

            // Colon
            ParseExpected(TokenType.Colon);

            Token name = ParseExpected(TokenType.String);
            NumberExpression order = null;
            if (IsNumber()) order = ParseNumber();

            // Get the rule options.
            var settings = new List<RuleSetting>();
            while (ParseOptional(TokenType.Identifier, out Token settingIdentifier))
            {
                StartNodeAtLast();
                settings.Add(EndNode(new RuleSetting(settingIdentifier, ParseExpected(TokenType.Dot), ParseExpected(TokenType.Identifier))));
            }

            // Get the conditions
            List<IfCondition> conditions = new List<IfCondition>();
            while (TryGetIfStatement(out var condition)) conditions.Add(condition);

            // Get the block.
            var statement = ParseStatement();

            return EndTokenCapture(new RuleContext(ruleToken, name, disabled, order, settings, conditions, statement));
        }

        /// <summary>Parses a class.</summary>
        ClassContext ParseClass()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out ClassContext @class)) return EndTokenCapture(@class);

            ParseExpected(TokenType.Class);
            var identifier = ParseExpected(TokenType.Identifier);

            // Get the types being inherited.
            var inheriting = new List<Token>();
            if (ParseOptional(TokenType.Colon, out Token inheritToken))
                do inheriting.Add(ParseExpected(TokenType.Identifier));
                while (ParseOptional(TokenType.Comma));

            // Start the class group.
            ParseExpected(TokenType.CurlyBracket_Open);

            ClassContext context = new ClassContext(identifier, inheritToken, inheriting);

            // Get the class elements.
            while (!Is(TokenType.CurlyBracket_Close) && !IsFinished)
                if (IsDeclaration(true))
                    context.Declarations.Add(ParseVariableOrFunctionDeclaration());
                else if (IsConstructor())
                    context.Constructors.Add(ParseConstructor());
                else
                {
                    // TODO: error recovery
                    break;
                }
            
            // End the class group.
            ParseExpected(TokenType.CurlyBracket_Close);

            return EndTokenCapture(context);
        }

        EnumContext ParseEnum()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out EnumContext @enum)) return EndTokenCapture(@enum);

            ParseExpected(TokenType.Enum);
            var identifier = ParseExpected(TokenType.Identifier);

            // Start the value group.
            ParseExpected(TokenType.CurlyBracket_Open);

            // Get the values
            var values = new List<EnumValue>();
            if (!Is(TokenType.CurlyBracket_Close))
                do
                {
                    // Get the value identifier.
                    StartNode();
                    var valueIdentifier = ParseExpected(TokenType.Identifier);
                    IParseExpression value = null;

                    // Get the enum's value.
                    if (ParseOptional(TokenType.Equal))
                        value = GetContainExpression();
                    
                    // Add the value to the list.
                    values.Add(EndNode(new EnumValue(valueIdentifier, value)));
                }
                while (ParseOptional(TokenType.Comma));

            // End the value group.
            ParseExpected(TokenType.CurlyBracket_Close);

            return new EnumContext(identifier, values);
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
                else if (ParseOptional(TokenType.Ref, out token)) tokens.Ref = token;
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

        Import ParseImport()
        {
            ParseExpected(TokenType.Import);
            var fileToken = ParseExpected(TokenType.String);

            // Parse optional 'as'.
            Token asIdentifier = null;
            if (ParseOptional(TokenType.As, out Token @as))
                asIdentifier = ParseExpected(TokenType.Identifier);

            ParseExpected(TokenType.Semicolon);
            return new Import(fileToken, @as, asIdentifier);
        }

        Hook ParseHook()
        {
            var variableExpression = GetContainExpression();
            ParseExpected(TokenType.Equal);
            var variableValue = GetContainExpression();
            ParseSemicolon();
            return new Hook(variableExpression, variableValue);
        }

        public Identifier MakeIdentifier(Token identifier, List<ArrayIndex> indices) => new Identifier(identifier, indices);
        MissingElement MissingElement() => new MissingElement(CurrentOrLast.Range);
        IParseStatement ExpressionStatement(IParseExpression expression, Token actionComment)
        {
            if (expression is IParseStatement statement) return statement;
            return new ExpressionStatement(expression, actionComment);
        }
    }
}
