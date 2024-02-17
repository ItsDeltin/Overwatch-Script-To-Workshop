using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse.Operators;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Compiler.Parse.Lexing;
using Deltin.Deltinteger.Compiler.File;
using MediatR;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Parser
    {
        Lexer Lexer { get; }
        int Token { get; set; }
        Token Current => Lexer.ScanTokenAt(Token);
        Token CurrentOrLast => Lexer.ScanTokenAtOrLast(Token);
        Token Previous => Lexer.ScanTokenAt(Token - 1);
        TokenType Kind => Current?.TokenType ?? TokenType.EOF;
        bool IsFinished => Kind == TokenType.EOF;

        public Stack<TokenCapture> TokenCaptureStack { get; } = new Stack<TokenCapture>();
        public Stack<int> TokenRangeStart { get; } = new Stack<int>();
        public Stack<bool> TernaryCheck { get; } = new Stack<bool>();
        public Stack<bool> StringCheck { get; } = new Stack<bool>();
        public List<TokenCapture> NodeCaptures { get; } = new List<TokenCapture>();
        public List<IParserError> Errors { get; } = new List<IParserError>();

        private readonly VersionInstance _content;
        private readonly ParserSettings _parserSettings;
        private readonly IncrementalParse? _increment;

        readonly OperatorStack<IParseExpression> _operatorStack = new(new OstwOperandFactory());
        readonly OperatorStack<IVanillaExpression> _vanillaOperatorStack = new(new VanillaOperandFactory());

        private int LookaheadDepth = 0;

        public Parser(VersionInstance content, ParserSettings parserSettings, IncrementalParse? increment)
        {
            Lexer = new(parserSettings, increment?.IncrementalLexer, content);
            _content = content;
            _parserSettings = parserSettings;
            _increment = increment;
            StringCheck.Push(false);
        }

        Token Consume()
        {
            if (!IsFinished)
            {
                Token++;

                if (LookaheadDepth == 0)
                    Lexer.ProgressTo(Token);

                return Lexer.ScanTokenAt(Token - 1);
            }
            return null;
        }

        void StartTokenCapture()
        {
            if (LookaheadDepth == 0)
                TokenCaptureStack.Push(new TokenCapture(Token));
        }

        T EndTokenCapture<T>(T node) where T : Node
        {
            if (LookaheadDepth == 0)
            {
                var capture = TokenCaptureStack.Pop();
                capture.Finish(Token, node);
                node.Range = TokenAtOrEnd(capture.StartToken).Range + Previous.Range;
                if (capture.IsValid) NodeCaptures.Add(capture);
            }
            return node;
        }

        void PopEndTokenCapture(object node)
        {
            if (LookaheadDepth == 0)
            {
                var capture = TokenCaptureStack.Pop();
                // todo: Shouldn't this be T - 1?
                capture.Finish(Token, node);
                if (capture.IsValid)
                    NodeCaptures.Add(capture);
            }
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

        T EndNode<T>(T node) where T : INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = new DocRange(
                    Lexer.GetTokenAtOrEnd(TokenRangeStart.Pop()).Range.Start,
                    Lexer.GetTokenAtOrEnd(Token - 1).Range.End);
            return node;
        }

        T EndNodeWithoutPopping<T>(T node) where T : INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = new DocRange(Lexer.ScanTokenAt(TokenRangeStart.Peek()).Range.Start, Previous.Range.End);
            return node;
        }

        public T EndNodeFrom<T>(T node, DocPos start) where T : INodeRange
        {
            if (LookaheadDepth == 0)
                node.Range = start + TokenAtOrEnd(Token - 1).Range.End;
            return node;
        }

        void PopNodeStack()
        {
            if (LookaheadDepth == 0)
                TokenRangeStart.Pop();
        }

        T Node<T>(Func<T> func) where T : INodeRange
        {
            StartNode();
            T r = func();
            return EndNode(r);
        }

        T CaptureRange<T>(Func<CaptureRangeHelper, T> func) => func(new CaptureRangeHelper(this, Token));

        readonly record struct CaptureRangeHelper(Parser Parser, int StartToken)
        {
            public readonly DocRange GetRange() => new(
                start: Parser.Lexer.GetTokenAtOrEnd(StartToken).Range.Start,
                end: Parser.Lexer.GetTokenAtOrEnd(Math.Max(Parser.Token - 1, StartToken)).Range.End);
        }

        /// <summary>Gets an existing parsing tree at the current position.</summary>
        /// <param name="node">The resulting node.</param>
        /// <typeparam name="T">The expected node type.</typeparam>
        /// <returns>A boolean determining whether a compatible node was found.</returns>
        bool GetIncrementalNode<T>(out T node)
        {
            // If the last syntax tree was not provided or the current token is inside the change range, return null.
            if (_increment is null || (Token >= _increment.Value.ChangeStartToken() && !Lexer.IsLexCompleted()))
            {
                node = default;
                return false;
            }

            // Iterate through each cached node.
            foreach (var capture in _increment.Value.NodeCaptures)
            {
                bool surpassesIncrementalChange = Token > _increment.Value.ChangeStartToken();
                int delta = surpassesIncrementalChange ? Lexer.GetTokenDelta() : 0;

                // If the node's type is equal to the expected node type
                // and the cached node's start token index is equal to the current token's index (adjusted for token difference if the current token is past the change position) 
                if (capture.Node.GetType() == typeof(T) &&
                    // The capture's end token (capture.startToken + capture.Length) preceeds (<) the token where the change starts (Lexer.IncrementalChangeStart)
                    // OR the Lexer's increment is completed (Lexer.IsPushCompleted) and the capture's start token (capture.startToken) proceeds (>) the token where
                    // the change starts (Lexer.IncrementalChangeStart)
                    //
                    // This makes sure we do not reuse a changed incremental node.
                    (capture.StartToken + capture.Length < _increment.Value.ChangeStartToken() || (Lexer.IsLexCompleted() && capture.StartToken > Lexer.LexResyncedAt())) &&
                    // If this surpasses the incremental change range, make sure the push is completed.
                    (!surpassesIncrementalChange || Lexer.IsLexCompleted()) &&
                    // Make sure the incremental node's position matches the current Token (capture.StartToken + ... = Token).
                    // If Token surpasses the incremental change range (surpassesIncrementalChange), then offset by the change delta.
                    capture.StartToken + delta == Token)
                {
                    // Then return the node then advance by the number of tokens in the cached node.
                    node = (T)capture.Node;
                    Token += capture.Length;
                    return true;
                }
            }

            // No matching node was found.
            node = default;
            return false;
        }

        T GetIncrementalNode<T>(Func<T> factory)
        {
            StartTokenCapture();
            if (!GetIncrementalNode(out T node))
                node = factory();
            PopEndTokenCapture(node);
            return node;
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

        void AddError(string message) => AddError(IParserError.New(CurrentOrLast.Range, message));

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
            Lookahead(() =>
            {
                result = action.Invoke();
            });
            return result;
        }

        bool Is(TokenType type)
        {
            switch (type)
            {
                default: return Kind == type;
                case TokenType.Identifier: return Kind.IsIdentifier();
            }
        }

        bool Is(TokenType type, int lookahead)
        {
            TokenType currentTokenType = Lexer.ScanTokenAt(Token + lookahead)?.TokenType ?? TokenType.EOF;
            switch (type)
            {
                default: return type == currentTokenType;
                case TokenType.Identifier: return currentTokenType.IsIdentifier();
            }
        }

        TokenType NextNonDecorativeToken()
        {
            for (int i = 0; ; i++)
            {
                var current = Lexer.ScanTokenAt(Token + i);
                if (current == null)
                    return TokenType.EOF;
                else if (!current.TokenType.IsDecorative())
                    return current.TokenType;
            }
        }

        Token TokenAtOrEnd(int position) => Lexer.ScanTokenAtOrLast(position);

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
            foreach (var type in types)
                if (Is(type))
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

        bool ParseOptionalWithMetaComment(TokenType type, out Token result, out MetaComment comment)
        {
            int forwardToken = 0;
            for (; Is(TokenType.ActionComment, forwardToken); forwardToken++) { }
            if (Is(type, forwardToken))
            {
                comment = ParseMetaComment();
                result = Consume();
                return true;
            }
            result = null;
            comment = null;
            return false;
        }

        bool Parse(TokenType type, bool isExpected, out Token result)
        {
            if (isExpected)
                return result = ParseExpected(type);
            else
                return ParseOptional(type, out result);
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
        void PushOperator(IStackOperator<IParseExpression> op) => _operatorStack.PushOperator(op);

        enum BinaryRhsMode
        {
            Default,
            Dot,
            Squiggle
        }

        bool TryParseBinaryOperator<T>(out IStackOperator<T> stackOperator, out BinaryRhsMode binaryRhsMode)
        {
            foreach (var op in CStyleOperator.BinaryOperators)
            {
                // Do not parse '>' if we are in a formatted string and the following token is not an identifier.
                if (op.Text == ">" && StringCheck.Peek() && !Lexer.ScanTokenAt(Token + 1).TokenType.IsStartOfExpression())
                    continue;

                // Do not parse right-hand ternary if the top operator is not a left-hand ternary operator.
                if (op.Type == CStyleOperatorType.TernaryRight && !TernaryCheck.Peek())
                    continue;

                if (ParseOptional(op.OstwTokenType, out Token token))
                {
                    if (op.Type == CStyleOperatorType.TernaryLeft)
                        TernaryCheck.Push(true);
                    else if (op.Type == CStyleOperatorType.TernaryRight)
                        TernaryCheck.Pop();

                    stackOperator = op.AsStackOperator<T>(token);
                    binaryRhsMode = op == CStyleOperator.Dot ? BinaryRhsMode.Dot :
                        op == CStyleOperator.Squiggle ? BinaryRhsMode.Squiggle : BinaryRhsMode.Default;
                    return true;
                }
            }

            stackOperator = null;
            binaryRhsMode = BinaryRhsMode.Default;
            return false;
        }

        IStackOperator<IParseExpression> ParseUnaryOperator()
        {
            // Operator !expr
            if (ParseOptional(TokenType.Exclamation, out var not))
                return CStyleOperator.Not.AsStackOperator<IParseExpression>(not);

            // Operator -expr
            else if (ParseOptional(TokenType.Subtract, out var inverse))
                return CStyleOperator.Inv.AsStackOperator<IParseExpression>(inverse);

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
        public void GetExpressionWithArray()
        {
            IParseExpression expression;
            switch (Kind)
            {
                // Negative number or unary operator
                case TokenType.Subtract:
                    if (IsNumber())
                    {
                        expression = ParseNumber();
                        break;
                    }
                    goto case TokenType.Exclamation;

                // Unary operator
                case TokenType.Exclamation:
                    var op = ParseUnaryOperator();
                    PushOperator(op);
                    GetExpressionWithArray();
                    return;

                // Type cast
                case TokenType.LessThan:
                    // Make sure this is actually a type cast and not an operator.
                    if (IsTypeCast())
                        ParseTypeCast();
                    goto default;

                default:
                    expression = GetSubExpression();
                    break;
            }
            _operatorStack.PushOperand(expression);
            GetArrayAndInvokes();
        }

        public void GetArrayAndInvokes()
        {
            while (true)
            {
                // Get the array index.
                if (ParseOptional(TokenType.SquareBracket_Open, out var open))
                {
                    IParseExpression index = GetContainExpression();
                    // End the closing square bracket.
                    var close = ParseExpected(TokenType.SquareBracket_Close);
                    // Push operator
                    PushOperator(new IndexerStackOperator<IParseExpression>(open, index, close));
                }
                // Invoke
                else if (ParseOptional(TokenType.Parentheses_Open, out Token leftParentheses))
                {
                    // Parse parameters.
                    var values = ParseParameterValues();
                    // End the parentheses.
                    Token rightParentheses = ParseExpected(TokenType.Parentheses_Close) ?? CurrentOrLast;
                    // Update the expression.
                    PushOperator(new InvokeStackOperator(leftParentheses, rightParentheses, values));
                }
                // No more array indices or invocations.
                else break;
            }
        }

        IParseExpression GetSubExpression()
        {
            switch (Kind)
            {
                // Numbers
                case TokenType.Number: return ParseNumber();
                // Booleans
                case TokenType.True: return new BooleanExpression(Consume(), true);
                case TokenType.False: return new BooleanExpression(Consume(), false);
                // Strings
                case TokenType.String: return Node(() => new StringExpression(null, Consume()));
                // Localized strings
                case TokenType.At: return Node(() => new StringExpression(Consume(), ParseExpected(TokenType.String)));
                // Interpolated strings
                case TokenType.InterpolatedStringTail:
                case TokenType.InterpolatedStringMiddle:
                case TokenType.InterpolatedStringHead:
                    return ParseInterpolatedString();
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
                // Async
                case TokenType.Async: return ParseAsyncExpression();
                // Struct declaration
                case TokenType.CurlyBracket_Open: return ParseStructDeclaration();
                // Formatted string
                case TokenType.LessThan:
                    // Make sure that the following token is a string.
                    if (Is(TokenType.String, 1) || Is(TokenType.At, 1)) return ParseFormattedString();
                    goto default;
                // Json
                case TokenType.Import: return ParseJsonImport();
                // Other
                default:
                    // Check if this is a lambda before expression group or identifier.
                    if (IsLambda())
                        return ParseLambda();

                    // 'single' struct
                    else if (Is(TokenType.Single) && Is(TokenType.CurlyBracket_Open, 1))
                        return ParseStructDeclaration();

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
            GetExpressionWithArray();

            // Binary operator
            while (TryParseBinaryOperator<IParseExpression>(out var stackOperator, out var rhsMode))
            {
                PushOperator(stackOperator);
                if (rhsMode == BinaryRhsMode.Default)
                {
                    GetExpressionWithArray();
                }
                else
                {
                    _operatorStack.PushOperand(Identifier());
                    GetArrayAndInvokes();

                    if (rhsMode == BinaryRhsMode.Squiggle)
                        _operatorStack.PopAllOperators();
                }
            }

            _operatorStack.PopAllOperators();
        }

        /// <summary>Contains the operator stack and parses an expression.</summary>
        /// <returns>The resulting expression.</returns>
        IParseExpression GetContainExpression(bool stringCheck = false)
        {
            StringCheck.Push(stringCheck);
            TernaryCheck.Push(false);

            var result = _operatorStack.GetExpression(GetExpressionWithOperators);

            StringCheck.Pop();
            TernaryCheck.Pop();

            return result;
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

            // Parse generics
            var generics = new List<IParseType>();
            if (indices.Count == 0 && IsGenerics())
                generics = ParseGenerics();

            return EndNode(MakeIdentifier(identifier, indices, generics));
        }

        /// <summary>Parses the inner parameter values of a function.</summary>
        /// <returns></returns>
        List<ParameterValue> ParseParameterValues()
        {
            if (Is(TokenType.Parentheses_Close)) return new List<ParameterValue>();
            return ParseDelimitedList(TokenType.Parentheses_Close, IsStartOfExpression, () =>
            {
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
            return ParseDelimitedList(terminator, elementDeterminer, _ => parseElement(), null);
        }

        List<T> ParseDelimitedList<T>(TokenType terminator, Func<bool> elementDeterminer, Func<Token, T> parseElement, Func<Token, T> onMissing)
        {
            var values = new List<T>();

            Token lastComma = null;
            bool requireThisTime = false;
            while (true)
            {
                if (elementDeterminer())
                {
                    requireThisTime = false;
                    int s = Token;

                    T lastElement = parseElement(lastComma);
                    values.Add(lastElement);

                    if (ParseOptional(TokenType.Comma, out lastComma))
                    {
                        requireThisTime = true;
                        if (lastElement is IListComma listComma) listComma.NextComma = lastComma;
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
                else if (requireThisTime && onMissing is not null)
                {
                    values.Add(onMissing(lastComma));
                }
                lastComma = null;
                requireThisTime = false;

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


        List<T> ParseList<T>(TokenType terminator, Func<bool> isElement, Func<T> parseElement) => ParseList(() => Is(terminator), isElement, parseElement);

        List<T> ParseList<T>(Func<bool> isTerminator, Func<bool> isElement, Func<T> parseElement)
        {
            var elements = new List<T>();
            while (!isTerminator.Invoke())
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
            var statements = ParseList(() => Is(TokenType.CurlyBracket_Close) || (Is(TokenType.ActionComment) && Is(TokenType.CurlyBracket_Close, 1)), () => Kind.IsStartOfStatement(), () => ParseStatement());


            //End Comment
            MetaComment metaComment = ParseMetaComment();
            // Close block
            ParseExpected(TokenType.CurlyBracket_Close);

            // Create the block
            var result = new Block(statements, metaComment);

            EndTokenCapture(result);

            // Done
            return result;
        }

        IParseStatement ParseStatement(bool parseSemicolon = true)
        {
            MetaComment metaComment = ParseMetaComment();

            IParseStatement statement;

            switch (Kind)
            {
                // Block
                case TokenType.CurlyBracket_Open: statement = ParseBlock(); break;
                // Continue and break
                case TokenType.Break: statement = ParseBreak(); break;
                case TokenType.Continue: statement = ParseContinue(); break;
                // Return
                case TokenType.Return: statement = ParseReturn(); break;
                // If
                case TokenType.If: statement = ParseIf(); break;
                // Switch
                case TokenType.Switch: statement = ParseSwitch(); break;
                // Loops
                case TokenType.For: statement = ParseFor(); break;
                case TokenType.While: statement = ParseWhile(); break;
                case TokenType.Foreach: statement = ParseForeach(); break;
                // Delete
                case TokenType.Delete: statement = ParseDelete(); break;
                // Declaration and expression statements.
                default:
                    // Declaration
                    if (IsDeclaration(false))
                        statement = ParseDeclaration(parseSemicolon);
                    // Expression statement
                    else
                        statement = ParseExpressionStatement(parseSemicolon);
                    break;
            }

            statement.Comment = metaComment;
            return statement;
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

        // Parses a struct or statement without the block/struct conflict. Used in places where either an expression or statement is expected.
        IParseStatement ParseStructOrStatement() => IsStructDeclaration() ? ExpressionStatement(ParseStructDeclaration(), null) : ParseStatement(false);

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
            while (ParseOptionalWithMetaComment(TokenType.Else, out _, out MetaComment branchComment))
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

                    elifs.Add(new ElseIf(elifExpr, elifStatement, branchComment));
                }
                // Else
                else
                {
                    // Parse the else's block.
                    var elseStatement = ParseStatement();
                    els = new Else(elseStatement, branchComment);

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
            var statements = ParseList(TokenType.CurlyBracket_Close, () => Kind.IsStartOfStatement() || Is(TokenType.Case) || Is(TokenType.Default), () =>
            {
                // Case
                if (ParseOptional(TokenType.Case, out var caseToken))
                {
                    StartNodeAtLast();
                    // Get the case's value.
                    var caseValue = GetContainExpression();
                    ParseExpected(TokenType.Colon);

                    // Add the case statement.
                    return EndNode(new SwitchCase(caseToken, caseValue));
                }
                // Default
                else if (ParseOptional(TokenType.Default, out var defaultToken))
                {
                    StartNodeAtLast();
                    ParseExpected(TokenType.Colon);

                    // Add the default statement.
                    return EndNode(new SwitchCase(defaultToken));
                }
                // Normal statement
                else return ParseStatement();
            });

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
            var extended = ParseOptional(TokenType.Exclamation);

            // Parse in.
            ParseExpected(TokenType.In);

            // Parse the value.
            var value = GetContainExpression();

            ParseExpected(TokenType.Parentheses_Close);

            // Get the foreach's statement.
            var statement = ParseStatement();

            // Done
            return EndTokenCapture(new Foreach(type, identifier, value, statement, extended));
        }

        Delete ParseDelete()
        {
            StartNode();
            var delete = ParseExpected(TokenType.Delete);

            // Get the expression
            var expression = GetContainExpression();

            ParseExpected(TokenType.Semicolon);
            return EndNode(new Delete(delete, expression));
        }

        IParseType ParseType()
        {
            StartNode();

            if (ParseOptional(TokenType.Void, out var @void))
                return EndNode(new ParseType(@void));

            var const_ = ParseOptional(TokenType.Const);

            // If we parse a parentheses, we can assume this is a lambda type.
            if (!ParseOptional(TokenType.Parentheses_Open))
            {
                // No parentheses found.
                // This is either a normal type or a lambda with a single parameter.

                // Get the type name.
                var identifier = ParseExpected(TokenType.Identifier, TokenType.Define);
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
                if (!Parse(TokenType.Arrow, const_, out Token arrow))
                {
                    PopNodeStack();
                    return result;
                }
                // Lambda type
                else
                {
                    // Parse the lambda's return type.
                    var returnType = ParseType();
                    return EndNode(new LambdaType(result, const_, returnType, arrow));
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
                if (parameterTypes.Count != 1 || const_)
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
                    return EndNode(new LambdaType(parameterTypes, const_, returnType, arrow));
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

        bool IsDeclaration(bool functionDeclaration) => Lookahead(() =>
        {
            ParseMetaComment();
            ParseAttributes();
            var typeParse = ParseType();
            return typeParse.LookaheadValid && (ParseExpected(TokenType.Identifier) && (
                // This is a declaration if the following token is:
                Is(TokenType.Semicolon) ||   // End declaration statement.
                Is(TokenType.Equal) ||       // Initial value.
                Is(TokenType.Exclamation) || // Extended collection marker.
                Is(TokenType.Number) ||      // Assigned workshop ID.
                Is(TokenType.Colon) ||       // Macro variable value.
                Is(TokenType.CurlyBracket_Open) || // Target vanilla value
                (functionDeclaration && (Is(TokenType.Parentheses_Open) || Is(TokenType.LessThan))) || // Function parameter start.
                IsFinished                   // EOF was reached.
            ));
        });

        bool IsConstructor() => Lookahead(() =>
        {
            ParseAttributes();
            return Is(TokenType.Constructor);
        });

        /// <summary>Determines if the current context is a type cast.</summary>
        bool IsTypeCast()
        {
            if (!Is(TokenType.LessThan))
                return false;

            return Lookahead(() =>
            {
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
            || Lookahead(() =>
            {
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

        bool IsHook() => Lookahead(() =>
        {
            bool parsedAny = false;
            do parsedAny = ParseExpected(TokenType.Identifier) || parsedAny;
            while (ParseOptional(TokenType.Dot));
            return parsedAny && Is(TokenType.Equal);
        });

        bool IsStartOfParameter() => Is(TokenType.Ref) || Is(TokenType.In) || Kind.IsStartOfType();

        bool IsStartOfExpression() => Kind.IsStartOfExpression() || Kind.IsBinaryOperator();

        bool IsGenerics() => Lookahead(() =>
        {
            if (Kind != TokenType.LessThan) return false;

            int genericLevel = 0;
            while (Kind.IsPartOfTypeArgs())
            {
                if (Kind == TokenType.LessThan)
                    genericLevel++;
                else if (Kind == TokenType.GreaterThan)
                {
                    genericLevel--;
                    if (genericLevel == 0) return true;
                }
                if (genericLevel < 0) return false;
                Consume();
            }

            return false;
        });

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
            var statement = ParseStructOrStatement();

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
            var metaComment = ParseMetaComment();
            var attributes = ParseAttributes();
            var type = ParseType();

            // Get the identifier.
            var identifier = ParseExpected(TokenType.Identifier);

            // Get the type args.
            var typeArgs = ParseOptionalTypeArguments(out bool hasGenerics);

            // Function
            if (hasGenerics || Is(TokenType.Parentheses_Open))
            {
                ParseExpected(TokenType.Parentheses_Open);

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
                    return EndNode(new FunctionContext(attributes, type, identifier, typeArgs, parameters, value, metaComment));
                }
                // Normal function
                else
                {
                    // Get optional subroutine attributes.
                    Token globalvar = null, playervar = null;
                    FunctionSubroutineSyntax subroutine = null;

                    // This will parse either the globalvar or playervar tokens.
                    if (ParseOptional(TokenType.GlobalVar, out globalvar) || ParseOptional(TokenType.PlayerVar, out playervar))
                    {
                        // Get the subroutine name.
                        subroutine = ParseSubroutineName(true);
                    }
                    // Get the optional subroutine name.
                    else subroutine = ParseSubroutineName(false);

                    // Get the function's block.
                    Block block = ParseBlock();
                    return EndNode(new FunctionContext(attributes, type, identifier, typeArgs, parameters, block, globalvar, playervar, subroutine, metaComment));
                }
            }
            // Variable
            else
            {
                // Parse an optional variable ID or extended collection marker.
                Token id = null, ext = null, macro = null;
                TargetWorkshopVariable? target = null;

                if (!ParseOptional(TokenType.Number, out id))
                    if (!ParseOptional(TokenType.Exclamation, out ext))
                        target = ParseOptionalTargetWorkshopVariable();

                // Get the initial value.
                IParseExpression initialValue = null;
                if (ParseOptional(TokenType.Equal) || ParseOptional(TokenType.Colon, out macro))
                    initialValue = GetContainExpression(); // Get the initial value.

                ParseSemicolon();
                return EndNode(new VariableDeclaration(attributes, type, identifier, initialValue, ext, id, macro, metaComment, target));
            }
        }

        TargetWorkshopVariable? ParseOptionalTargetWorkshopVariable() => CaptureRange<TargetWorkshopVariable?>(r =>
        {
            // globalvar and playervar variables can target vanilla workshop variables.
            // Examples:
            //    globalvar Number myVar!"a";
            //    globalvar Number myVar!{"a"};
            //    globalvar Number myVar!{"a", "b"};
            //    globalvar Number myVar!"a"[0];
            //    globalvar Number myVar!{"a"[0]};
            //    globalvar Number myVar!{"a"[0], "b"[0]};

            // Parses a chain of [..] or [expr] following the target variable string.
            List<SpreadOrExpression> ParseVanillaTargetIndexer()
            {
                var indexer = new List<SpreadOrExpression>();

                while (ParseOptional(TokenType.SquareBracket_Open))
                {
                    // [..]
                    if (ParseOptional(TokenType.Spread, out var spread))
                    {
                        indexer.Add(new(spread, null));
                    }
                    // [expr]
                    else
                    {
                        indexer.Add(new(spread, GetContainExpression()));
                    }

                    ParseExpected(TokenType.SquareBracket_Close);
                }

                return indexer;
            }

            var targets = new List<StackTarget>();

            if (ParseOptional(TokenType.CurlyBracket_Open))
            {
                // Parse targets while there are strings.
                do
                {
                    if (ParseExpected(TokenType.String, out var target))
                    {
                        targets.Add(new(target, ParseVanillaTargetIndexer()));
                    }
                }
                while (ParseOptional(TokenType.Comma));
                ParseExpected(TokenType.CurlyBracket_Close);
            }
            else return null;
            return new TargetWorkshopVariable(r.GetRange(), targets);
        });

        FunctionSubroutineSyntax ParseSubroutineName(bool required)
        {
            Token name = required ? ParseExpected(TokenType.String) : ParseOptional(TokenType.String);
            Token target = null;

            if (name && ParseOptional(TokenType.Colon))
            {
                target = ParseExpected(TokenType.String);
            }

            return new(name, target);
        }

        VariableDeclaration ParseDeclaration(bool parseSemicolon)
        {
            StartNode();
            var metaComment = ParseMetaComment();
            var attributes = ParseAttributes();
            var type = ParseType();
            var identifier = ParseExpected(TokenType.Identifier);

            // Parse an optional variable ID or extended collection marker.
            Token id = null, ext = null, macro = null;
            TargetWorkshopVariable? target = null;
            if (!ParseOptional(TokenType.Number, out id))
                if (!ParseOptional(TokenType.Exclamation, out ext))
                    target = ParseOptionalTargetWorkshopVariable();

            // Initial value
            IParseExpression initialValue = null;
            if (ParseOptional(TokenType.Equal) || ParseOptional(TokenType.Colon, out macro))
                // Get the value.
                initialValue = GetContainExpression();

            ParseSemicolon(parseSemicolon);
            return EndNode(new VariableDeclaration(attributes, type, identifier, initialValue, ext, id, macro, metaComment, target));
        }

        ConstructorContext ParseConstructor()
        {
            var attributes = ParseAttributes();
            var constructor = ParseExpected(TokenType.Constructor);
            ParseExpected(TokenType.Parentheses_Open);

            // Get the parameters.
            var parameters = ParseParameters();

            // End the parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            Token subroutineName = ParseOptional(TokenType.String);

            // Get the constructor's block.
            Block block = ParseBlock();

            return new ConstructorContext(attributes, constructor, parameters, subroutineName, block);
        }

        NewExpression ParseNew()
        {
            StartNode();
            ParseExpected(TokenType.New);

            // Parse the class identifier.
            var type = ParseType();

            // Start the parentheses.
            ParseExpected(TokenType.Parentheses_Open);

            // Parse the parameters.
            List<ParameterValue> parameterValues = ParseParameterValues();

            // End the parentheses.
            ParseExpected(TokenType.Parentheses_Close);

            return EndNode(new NewExpression(type, parameterValues));
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

        void ParseTypeCast()
        {
            DocPos startPosition = Current.Range.Start;

            // Parse the opening angled bracket.
            ParseExpected(TokenType.LessThan);

            // Parse the type.
            var type = ParseType();

            // Parse the closing angled bracket.
            ParseExpected(TokenType.GreaterThan);

            PushOperator(new TypeCastStackOperator(this, type, startPosition));
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

        List<IParseType> ParseGenerics()
        {
            var generics = new List<IParseType>();

            if (ParseOptional(TokenType.LessThan))
            {
                generics = ParseDelimitedList(TokenType.GreaterThan, () => Lookahead(() => ParseType().LookaheadValid), ParseType);
                ParseExpected(TokenType.GreaterThan);
            }

            return generics;
        }

        List<TypeArgContext> ParseOptionalTypeArguments(out bool anyGenerics)
        {
            var generics = new List<TypeArgContext>();
            if (ParseOptional(TokenType.LessThan))
            {
                generics = ParseDelimitedList(
                    TokenType.GreaterThan,
                    () => Kind.IsIdentifier(),
                    () =>
                    {
                        Token single = ParseOptional(TokenType.Single);
                        Token identifier = ParseExpected(TokenType.Identifier);
                        return new TypeArgContext(identifier, single);
                    }
                );
                ParseExpected(TokenType.GreaterThan);
                anyGenerics = true;
            }
            else anyGenerics = false;
            return generics;
        }

        AsyncContext ParseAsyncExpression()
        {
            StartNode();
            var asyncToken = ParseExpected(TokenType.Async);
            var ignoreIfRunning = ParseOptional(TokenType.Exclamation);
            var expression = GetContainExpression();
            return EndNode(new AsyncContext(asyncToken, ignoreIfRunning, expression));
        }

        StructDeclarationContext ParseStructDeclaration()
        {
            // Get the incremental data
            StartTokenCapture();
            if (GetIncrementalNode(out StructDeclarationContext rule)) return EndTokenCapture(rule);

            // Inline single struct
            var single = ParseOptional(TokenType.Single);

            // Start the struct declaration.
            ParseExpected(TokenType.CurlyBracket_Open);

            // Parse the struct values.
            // Both of these are accepted:
            // {XYZ: Vector.Up, W: 0}
            // {Vector XYZ: Vector.Up, Number W: 0}
            var values = ParseDelimitedList(TokenType.CurlyBracket_Close, () => Lookahead(() => Kind == TokenType.Spread || ParseType().LookaheadValid), () =>
            {
                StartNode();
                var spread = ParseOptional(TokenType.Spread);
                if (spread)
                {
                    var spreadValue = GetContainExpression();
                    return EndNode(new StructDeclarationVariableContext(spread, spreadValue));
                }

                var typeOrIdentifier = ParseType(); // Parse the variable type.
                var identifier = ParseOptional(TokenType.Identifier); // Parse the identifier.
                ParseExpected(TokenType.Colon); // Parse the struct value seperator.
                var value = GetContainExpression(); // Parse the value.

                if (!identifier && typeOrIdentifier is ITypeContextHandler typeContextHandler)
                {
                    identifier = typeContextHandler.Identifier;
                    typeOrIdentifier = null;
                }

                return EndNode(new StructDeclarationVariableContext(typeOrIdentifier, identifier, value));
            });

            // End the struct declaration.
            var closingBracket = ParseExpected(TokenType.CurlyBracket_Close);

            return EndTokenCapture(new StructDeclarationContext(values, closingBracket, single));
        }

        bool IsStructDeclaration() => Lookahead(() =>
        {
            // 'single' token followed by opening curly bracket (to ensure this isn't a variable named 'single'.)
            if (ParseOptional(TokenType.Single) && ParseOptional(TokenType.CurlyBracket_Open))
                return true;

            // Start of struct '{'
            if (!ParseExpected(TokenType.CurlyBracket_Open))
                return false;

            if (ParseOptional(TokenType.Spread))
                return true;

            var typeOrIdentifier = ParseType();
            var identifier = ParseOptional(TokenType.Identifier);
            var colon = ParseExpected(TokenType.Colon);

            return ((typeOrIdentifier.LookaheadValid && identifier) || (typeOrIdentifier is ITypeContextHandler && !identifier)) && colon;
        });

        InterpolatedStringExpression ParseInterpolatedString()
        {
            StartNode();

            // Interpolated string with no values.
            if (ParseOptional(TokenType.InterpolatedStringHead, out Token existingHead))
                return EndNode(new InterpolatedStringExpression(existingHead, null));

            // Get the interpolated string tail.
            var tail = ParseExpected(TokenType.InterpolatedStringTail);

            // Determines if the string is single or double quotes.
            bool single = tail && ((tail.Flags & TokenFlags.StringSingleQuotes) == TokenFlags.StringSingleQuotes);

            // Get the interpolated value.
            var interpolatedValue = GetContainExpression();

            var parts = new List<InterpolatedStringPart>();

            // Continue string.
            Lexer.InContext(single ? LexerContextKind.InterpolatedStringSingle : LexerContextKind.InterpolatedStringDouble, () =>
            {
                while (true)
                {
                    // } {
                    if (ParseOptional(TokenType.InterpolatedStringMiddle, out Token middle))
                    {
                        parts.Add(new InterpolatedStringPart(interpolatedValue, middle));
                        interpolatedValue = Lexer.InContext(LexerContextKind.Normal, () => GetContainExpression());
                    }
                    // }"
                    else if (ParseOptional(TokenType.InterpolatedStringHead, out Token head) || ParseOptional(TokenType.String, out head))
                    {
                        parts.Add(new InterpolatedStringPart(interpolatedValue, head));
                        break;
                    }
                    else
                    {
                        AddError(new InterpolationMissingTerminator(tail.Range));
                        break;
                    }
                }
                return Unit.Value;
            });

            return EndNode(new InterpolatedStringExpression(tail, parts));
        }

        ImportJsonSyntax ParseJsonImport()
        {
            StartNode();
            ParseExpected(TokenType.Import);
            ParseExpected(TokenType.Parentheses_Open);
            var file = ParseExpected(TokenType.String);
            ParseExpected(TokenType.Parentheses_Close);

            return EndNode(new ImportJsonSyntax(file));
        }

        /// <summary>Parses the root of a file.</summary>
        public DocumentParseResult Parse()
        {
            var context = new RootContext();
            while (!IsFinished) ParseScriptRootElement(context);

            return new(context, Lexer.CurrentController.GetTokenList(), Errors, NodeCaptures, _content);
        }

        /// <summary>Parses a single import, rule, variable, class, etc.</summary>
        /// <returns>Determines whether an element was parsed.</returns>
        void ParseScriptRootElement(RootContext context)
        {
            switch (NextNonDecorativeToken())
            {
                // Class
                case TokenType.Class:
                case TokenType.Struct:
                    context.RootItems.Add(new(ParseClassOrStruct()));
                    return;
            }

            // Workshop variable or subroutine collection
            if (Is(TokenType.WorkshopVariables) || Is(TokenType.WorkshopVariablesEn) ||
                Is(TokenType.WorkshopSubroutines) || Is(TokenType.WorkshopSubroutinesEn))
            {
                context.RootItems.Add(new(ParseVanillaVariableCollection()));
                return;
            }

            // Vanilla settings
            if (Is(TokenType.WorkshopSettings) || Is(TokenType.WorkshopSettingsEn))
            {
                context.RootItems.Add(new(ParseVanillaLobbySettings()));
                return;
            }

            // Workshop rule
            if (IsVanillaRule())
            {
                context.RootItems.Add(new(ParseVanillaRule()));
                return;
            }

            // Return false if the EOF was reached.
            switch (Kind)
            {
                // Rule
                case TokenType.Rule:
                case TokenType.Disabled:
                    context.RootItems.Add(new(ParseRule()));
                    break;

                // Enum
                case TokenType.Enum:
                    context.RootItems.Add(new(ParseEnum()));
                    break;

                // Import
                case TokenType.Import:
                    context.RootItems.Add(new(ParseImport()));
                    break;

                case TokenType.Type:
                    context.RootItems.Add(new(ParseTypeAlias()));
                    break;

                case TokenType.GlobalVar:
                    if (Is(TokenType.CurlyBracket_Open, 1))
                    {
                        context.GlobalvarReservations.AddRange(ParseVariableReservation());
                    }
                    else goto default;
                    break;
                case TokenType.PlayerVar:
                    if (Is(TokenType.CurlyBracket_Open, 1))
                    {
                        context.PlayervarReservations.AddRange(ParseVariableReservation());
                    }
                    else goto default;
                    break;

                // Others
                default:
                    // Variable declaration
                    if (IsDeclaration(true))
                        context.RootItems.Add(new(ParseVariableOrFunctionDeclaration()));
                    // Hook
                    else if (IsHook())
                        context.Hooks.Add(ParseHook());
                    // Unknown
                    else
                        Unexpected(true);
                    break;
            }
        }


        List<Token> ParseVariableReservation()
        {
            if (Is(TokenType.GlobalVar))
            {
                ParseExpected(TokenType.GlobalVar);
            }
            else
            {
                ParseExpected(TokenType.PlayerVar);
            }
            ParseExpected(TokenType.CurlyBracket_Open);
            var variables = ParseDelimitedList(TokenType.CurlyBracket_Close, () => true, () => ParseOptional(TokenType.String) ?? ParseOptional(TokenType.Number));
            ParseExpected(TokenType.CurlyBracket_Close);
            return variables;

            //TODO
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

        /// <summary>Parses a type alias. </summary>
        TypeAliasContext ParseTypeAlias()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out TypeAliasContext type)) return EndTokenCapture(type);

            ParseExpected(TokenType.Type);
            Token nameToken = ParseExpected(TokenType.Identifier);
            ParseExpected(TokenType.Equal);
            var parseType = ParseType();
            ParseExpected(TokenType.Semicolon);

            return EndTokenCapture(new TypeAliasContext(nameToken, parseType));

        }

        /// <summary>Parses a class.</summary>
        ClassContext ParseClassOrStruct()
        {
            StartTokenCapture();
            if (GetIncrementalNode(out ClassContext @class)) return EndTokenCapture(@class);

            var doc = ParseMetaComment();

            var single = ParseOptional(TokenType.Single);

            var declareToken = ParseExpected(TokenType.Class, TokenType.Struct);

            var identifier = ParseExpected(TokenType.Identifier);

            // Get the type parameters.
            var generics = ParseOptionalTypeArguments(out _);

            // Get the types being inherited.
            var inheriting = new List<IParseType>();
            if (ParseOptional(TokenType.Colon, out Token inheritToken))
                do inheriting.Add(ParseType());
                while (ParseOptional(TokenType.Comma));

            // Start the class group.
            ParseExpected(TokenType.CurlyBracket_Open);

            ClassContext context = new ClassContext(declareToken, identifier, generics, inheritToken, inheriting, single, doc);

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
                else if (ParseOptional(TokenType.In, out token)) tokens.In = token;
                else if (ParseOptional(TokenType.Persist, out token)) tokens.Persist = token;
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
            condition.Comment = ParseMetaComment();

            if (condition.Comment != null)
            {
                if (!ParseExpected(TokenType.If, out condition.If))
                {
                    condition = null;
                    return false;
                }
            }
            else if (!ParseOptional(TokenType.If, out condition.If))
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

        bool IsVanillaRule() => Lookahead(() =>
        {
            ParseOptional(TokenType.Disabled);
            return ParseExpected(TokenType.Rule, TokenType.WorkshopRule) && ParseExpected(TokenType.Parentheses_Open);
        });

        VanillaRule ParseVanillaRule() => GetIncrementalNode<VanillaRule>(() =>
        {
            var disabled = ParseOptional(TokenType.Disabled);
            var keyword = ParseExpected(TokenType.Rule, TokenType.WorkshopRule);

            // Parentheses containing name.
            ParseExpected(TokenType.Parentheses_Open);
            var name = ParseExpected(TokenType.String);
            ParseExpected(TokenType.Parentheses_Close);

            // Inner data
            var begin = ParseExpected(TokenType.CurlyBracket_Open);

            var ruleContent = new List<VanillaRuleContent>();
            while (TryParseVanillaRuleContent(out var nextElement))
                ruleContent.Add(nextElement);

            ParseExpected(TokenType.CurlyBracket_Close);
            var end = Previous;

            return new(disabled, keyword, name, begin, ruleContent.ToArray(), end);
        });

        bool TryParseVanillaRuleContent(out VanillaRuleContent nextElement)
        {
            nextElement = Lexer.InVanillaWorkshopContext(() => Kind switch
            {
                TokenType.WorkshopEvent => ParseVanillaRuleContent(Kind),
                TokenType.WorkshopConditions => ParseVanillaRuleContent(Kind),
                TokenType.WorkshopActions => ParseVanillaRuleContent(Kind),
                _ => default
            });
            return nextElement != null;
        }

        VanillaRuleContent ParseVanillaRuleContent(TokenType initiatingTokenType) => CaptureRange(r =>
        {
            var groupToken = ParseExpected(initiatingTokenType);
            ParseExpected(TokenType.CurlyBracket_Open);

            List<CommentedVanillaExpression> items = new();
            while (Kind.IsWorkshopExpression() || Is(TokenType.Semicolon) || Is(TokenType.DisabledWorkshopItem))
            {
                var comment = ParseOptional(TokenType.String);
                var disabled = ParseOptional(TokenType.DisabledWorkshopItem);
                var expression = ParseVanillaExpression();
                var semicolon = ParseExpected(TokenType.Semicolon);
                items.Add(new(comment, disabled, expression, semicolon));
            }

            ParseExpected(TokenType.CurlyBracket_Close);
            return new VanillaRuleContent(r.GetRange(), groupToken, items.ToArray());
        });

        IVanillaExpression ParseVanillaExpression()
        {
            var expr = _vanillaOperatorStack.GetExpression(() =>
            {
                TernaryCheck.Push(false);
                ParseVanillaOperandToStack();

                while (true)
                {
                    // Binary operators
                    if (TryParseBinaryOperator<IVanillaExpression>(out var op, out _))
                    {
                        _vanillaOperatorStack.PushOperator(op);
                        ParseVanillaOperandToStack();
                    }
                    // Array indexer
                    else if (ParseOptional(TokenType.SquareBracket_Open, out var open))
                    {
                        var indexer = ParseVanillaExpression();
                        var close = ParseExpected(TokenType.SquareBracket_Close);
                        _vanillaOperatorStack.PushOperator(
                            new IndexerStackOperator<IVanillaExpression>(open, indexer, close));

                    }
                    else break;
                }
                TernaryCheck.Pop();
            });
            // Assignment operators
            if (TokenExtensions.Assignment_Tokens.Contains(Kind))
            {
                var token = Consume();
                var rhs = ParseVanillaExpression();
                expr = new VanillaAssignmentExpression(expr, token, rhs);
            }

            return expr;
        }

        void ParseVanillaOperandToStack()
        {
            // Check for NOT operator
            if (ParseOptional(TokenType.Exclamation, out var not))
            {
                _vanillaOperatorStack.PushOperator(
                    CStyleOperator.Not.AsStackOperator<IVanillaExpression>(not)
                );
                ParseVanillaOperandToStack();
            }
            else
                _vanillaOperatorStack.PushOperand(ParseVanillaExpressionTerm());
        }

        IVanillaExpression ParseVanillaExpressionTerm() => CaptureRange(r =>
        {
            IVanillaExpression expression;
            // Numbers
            if (IsNumber())
            {
                expression = ParseNumber();
            }
            // Strings
            else if (Is(TokenType.String))
            {
                expression = new VanillaStringExpression(Consume());
            }
            // Identifiers, constants, and workshop functions
            else if (Is(TokenType.WorkshopSymbol) || Is(TokenType.WorkshopConstant))
            {
                expression = new VanillaSymbolExpression(Consume());
            }
            // Expression contained in parentheses.
            else if (Is(TokenType.Parentheses_Open))
            {
                Consume();
                var innerExpression = ParseVanillaExpression();
                var rightParentheses = ParseExpected(TokenType.Parentheses_Close);
                expression = new ParenthesizedVanillaExpression(r.GetRange(), innerExpression);
            }
            // Team sugar
            else if (Is(TokenType.AllTeams) || Is(TokenType.Team1) || Is(TokenType.Team2))
            {
                expression = new VanillaTeamSugarExpression(Consume());
            }
            // Unknown
            else
            {
                Consume();
                AddError(new InvalidExpressionTerm(CurrentOrLast));
                return new MissingVanillaExpression(r.GetRange());
            }

            // Invoke
            if (ParseOptional(TokenType.Parentheses_Open, out var leftParentheses))
            {
                var arguments = ParseDelimitedList(
                    TokenType.Parentheses_Close,
                    () => Kind.IsWorkshopExpression(),
                    previousComma => new VanillaInvokeParameter(previousComma, ParseVanillaExpression()),
                    previousComma =>
                    {
                        AddError(new UnexpectedToken(CurrentOrLast));
                        return new VanillaInvokeParameter(previousComma, new MissingVanillaExpression(CurrentOrLast));
                    });

                // End parameter list
                var rightParentheses = ParseExpected(TokenType.Parentheses_Close);

                expression = new VanillaInvokeExpression(r.GetRange(), expression, arguments, leftParentheses, rightParentheses);
            }

            return expression;
        });

        VanillaVariableCollection ParseVanillaVariableCollection() => CaptureRange(r =>
        {
            var openingToken = ParseExpected(
                TokenType.WorkshopVariablesEn,
                TokenType.WorkshopVariables,
                TokenType.WorkshopSubroutinesEn,
                TokenType.WorkshopSubroutines
            );
            return Lexer.InVanillaWorkshopContext(() =>
            {
                ParseExpected(TokenType.CurlyBracket_Open);

                var items = new List<GroupOrName>();
                while (true)
                {
                    // New group
                    if (ParseOptional(TokenType.WorkshopSymbol, out var groupToken))
                    {
                        ParseExpected(TokenType.Colon);
                        items.Add(new(new VariableGroup(groupToken)));
                    }
                    // Assigned variable in current group
                    else if (ParseOptional(TokenType.Number, out var varId))
                    {
                        ParseExpected(TokenType.Colon);
                        var name = ParseExpected(TokenType.WorkshopSymbol, TokenType.WorkshopConstant);
                        items.Add(new(new VariableName(varId, name)));
                    }
                    else break;
                }

                ParseExpected(TokenType.CurlyBracket_Close);
                return new VanillaVariableCollection(openingToken, r.GetRange(), items);
            });
        });

        VanillaSettingsSyntax ParseVanillaLobbySettings()
        {
            var openingToken = ParseExpected(TokenType.WorkshopSettings, TokenType.WorkshopSettingsEn);
            return Lexer.InSettingsContext(() =>
            {
                var group = ParseListOfSettings();
                return new VanillaSettingsSyntax(openingToken, group);
            });
        }

        VanillaSettingsGroupSyntax ParseListOfSettings() => CaptureRange(r =>
        {
            var openingBracket = ParseExpected(TokenType.CurlyBracket_Open);

            var settings = new List<VanillaSettingSyntax>();

            while (Is(TokenType.WorkshopSymbol) || Is(TokenType.WorkshopConstant) || Is(TokenType.DisabledWorkshopItem))
            {
                var disabled = ParseOptional(TokenType.DisabledWorkshopItem);
                var settingToken = Consume();

                // Extensions will not have a value.
                IVanillaSettingValueSyntax settingValue = null;

                Token tokenAfterColon = null;

                // Key/value pair
                if (ParseOptional(TokenType.Colon, out var colon))
                {
                    tokenAfterColon = Current;
                    // Keyword
                    if (Is(TokenType.WorkshopConstant) || Is(TokenType.WorkshopSymbol))
                    {
                        settingValue = new SymbolSettingSyntax(Consume());
                    }
                    // Number
                    else if (ParseOptional(TokenType.Number, out var numberToken))
                    {
                        // Percent sign
                        var percentSign = ParseOptional(TokenType.Modulo);
                        settingValue = new NumberSettingSyntax(numberToken, percentSign);
                    }
                    // String
                    else if (ParseOptional(TokenType.String, out var stringToken))
                    {
                        settingValue = new StringSettingSyntax(stringToken);
                    }
                    // Error
                    else AddError("Expected symbol, number, or string");
                }
                // Sublist
                else if (Is(TokenType.CurlyBracket_Open))
                {
                    settingValue = ParseListOfSettings();
                }
                // Map ID thing (ex: Workshop Island Night 972777519512064579)
                else if (ParseOptional(TokenType.Number))
                {
                    // ...not sure what to do with this
                }

                settings.Add(new(disabled, settingToken, colon, tokenAfterColon, settingValue));
            }

            ParseExpected(TokenType.CurlyBracket_Close);

            return new VanillaSettingsGroupSyntax(r.GetRange(), openingBracket, settings.ToArray());
        });

        Identifier MakeIdentifier(Token identifier, List<ArrayIndex> indices, List<IParseType> generics) => new Identifier(identifier, indices, generics);
        MetaComment ParseMetaComment()
        {
            if (!Is(TokenType.ActionComment))
                return null;

            StartNode();
            List<Token> comments = new List<Token>();
            while (ParseOptional(TokenType.ActionComment, out Token comment)) comments.Add(comment);
            return EndNode(new MetaComment(comments));
        }

        MissingElement MissingElement() => new MissingElement(CurrentOrLast.Range);
        IParseStatement ExpressionStatement(IParseExpression expression, Token actionComment)
        {
            if (expression is IParseStatement statement) return statement;
            return new ExpressionStatement(expression, actionComment);
        }
    }
}
