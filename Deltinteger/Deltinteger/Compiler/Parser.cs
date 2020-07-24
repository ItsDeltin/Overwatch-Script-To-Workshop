using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.Parser
{
    public class Parser
    {
        public Lexer Lexer { get; }

        public ParserRule Root = new ParserRule();
        public ParserRule DeclareRule = new ParserRule();
        public ParserRule Statement = new ParserRule();
        public ParserRule Expression = new ParserRule();
        public ParserRule Define = new ParserRule();
        public ParserRule CodeType = new ParserRule();
        
        public Parser(Lexer lexer)
        {
            Lexer = lexer;

            // Types
            CodeType.Init(seq(
                req(alt("Expected define or identifier", TokenType.Define, TokenType.Identifier)),
                // Generics
                opt(seq(
                    req(TokenType.LessThan),
                    CodeType,
                    rep(seq(TokenType.Comma, CodeType)),
                    TokenType.GreaterThan
                )),
                // Array
                rep(seq(req(TokenType.SquareBracket_Open), TokenType.SquareBracket_Close))
            ));

            // Root of the file.
            Root.Init(rep(DeclareRule));

            // Rule declaration.
            DeclareRule.Init(seq(
                TokenType.Rule, TokenType.Colon, TokenType.String, // rule: ""
                rep(seq(TokenType.Identifier, TokenType.Dot, TokenType.Identifier)), // Rule options
                rep(seq(TokenType.If, TokenType.Parentheses_Open, Expression, TokenType.Parentheses_Close)), // Conditions
                Statement
            ));

            // Expressions.
            Expression.Init(alt("Invalid expression term '{0}'",
                TokenType.String,
                TokenType.Number,
                TokenType.True,
                TokenType.False
            ));

            // Statements
            Statement.Init(alt("Invalid statement term '{0}'",
                // Block
                seq(
                    req(TokenType.CurlyBracket_Open),
                    rep(Statement),
                    TokenType.CurlyBracket_Close
                ),
                // Break
                seq(req(TokenType.Break), TokenType.Semicolon),
                // Continue
                seq(req(TokenType.Continue), TokenType.Semicolon),
                // Define
                seq(req(Define), TokenType.Semicolon),
                // Single semicolon
                TokenType.Semicolon
            ));
            
            // Define
            Define.Init(seq(
                CodeType,
                TokenType.Identifier,
                opt(alt("Expected number or !",
                    TokenType.Number,
                    TokenType.Exclamation
                )),
                opt(seq(
                    TokenType.Equals,
                    Expression
                ))
            ));
        }

        public void Parse(TreeWalker rule)
        {
            ParseTree root = new ParseTree(Lexer);
            rule.Walk(root);
            root.Debug();
        }

        /// <summary>A sequence of rules or tokens.</summary>
        /// <param name="parts">The rules in order.</param>
        /// <returns>The created sequence.</returns>
        private static Sequence seq(params TreeWalker[] parts) => new Sequence(parts);
        /// <summary>Repeats the tree walker.</summary>
        /// <param name="part">The rule that will be repeated.</param>
        /// <returns>The created repeater.</returns>
        private static Repeat0 rep(TreeWalker part) => new Repeat0(part);
        /// <summary>An optional rule.</summary>
        /// <param name="walker">The optional rule.</param>
        /// <returns>The created tree walker.</returns>
        private static Optional opt(TreeWalker walker) => new Optional(walker);
        /// <summary>One of any of the specified options.</summary>
        /// <param name="alias">The message that is displayed when none is found.</param>
        /// <param name="options">The valid rules.</param>
        /// <returns>The created tree walker.</returns>
        private static Alt alt(string alias, params TreeWalker[] options) => new Alt(alias, options);
        /// <summary>Marks the rule as required. If this rule is not found in a sequence, the sequence is canceled.</summary>
        /// <param name="walker">The rule that will be marked as required.</param>
        /// <returns>Returns 'walker'.</returns>
        private static TreeWalker req(TreeWalker walker)
        {
            walker.Required = true;
            return walker;
        }
    }

    public class ParseTree
    {
        public int Position { get; private set; }
        public Lexer Lexer { get; }
        public List<ParserDiagnostic> Diagnostics { get; } = new List<ParserDiagnostic>();
        public bool Accepted { get; private set; }
        public bool ReachedEnd => Position == Lexer.Tokens.Count;
        public bool WasAdvanced { get; private set; }
        public bool ContinueEvenWithNoAdvance { get; set; }
        private readonly List<ParseTree> _children = new List<ParseTree>();
        private readonly ParseTree _parent;

        public ParseTree(Lexer lexer)
        {
            Lexer = lexer;
        }
        private ParseTree(ParseTree parent)
        {
            _parent = parent;
            parent._children.Add(this);
            Position = parent.Position;
            Lexer = parent.Lexer;
        }

        public void Advance()
        {
            WasAdvanced = true;
            if (Position == Lexer.Tokens.Count) throw new Exception("No more tokens to advance.");
            Position++;
        }

        public void Moosh(ParseTree parseTree)
        {
            if (!_children.Remove(parseTree)) throw new Exception("Mooshing parse tree that is not a child.");
            if (parseTree.Position > Position) WasAdvanced = true;
            Position = parseTree.Position;
            Diagnostics.AddRange(parseTree.Diagnostics);
            _children.AddRange(parseTree._children);
            Accept();
        }

        public ParseTree Child() => new ParseTree(this);

        /// <summary>Gets the current token.</summary>
        public Token CurrentToken() => Lexer.Tokens[Position];

        /// <summary>Gets the last token in the file.</summary>
        public Token LastToken() => Lexer.Tokens[Lexer.Tokens.Count - 1];

        /// <summary>The diagnostics that will be displayed if this tree is chosen.</summary>
        /// <param name="message">The message that will be displayed.</param>
        /// <param name="range">The range of the diagnostic.</param>
        public void Diagnostic(string message, DocRange range) => Diagnostics.Add(new ParserDiagnostic(message, range));

        /// <summary>Accepts the current tree.</summary>
        public void Accept()
        {
            Accepted = true;
            // if (_parent != null)
            // {
            //     _parent.Position = Position;
            //     _parent.WasAdvanced = true;
            // }
        }

        public void Skip(ParseTree parseTree)
        {
            if (Position >= parseTree.Position) throw new Exception("Skipping to same or lower.");
            Position = parseTree.Position;
            WasAdvanced = true;
        }

        public void Debug(int ind = 0)
        {
            foreach (var diag in Diagnostics)
                Console.WriteLine(new string(' ', ind * 4) + diag.Message + ": " + diag.Range);
            foreach (var child in _children)
                if (child.Accepted)
                {
                    child.Debug(ind + 1);
                    return;
                }
        }

        public override string ToString() => "[" + nameof(Accepted) + ": " + Accepted +
            ", " + nameof(_children) + ": " + _children.Count +
            ", " + nameof(Diagnostics) + ": " + Diagnostics.Count + "]";
    }

    public class ParserRule : TreeWalker
    {
        private TreeWalker _walker;

        public ParserRule() {}

        public void Init(TreeWalker walker)
        {
            _walker = walker;
        }

        public override void Walk(ParseTree parseTree)
        {
            _walker.Walk(parseTree);
        }
    }

    public abstract class TreeWalker
    {
        public bool Required { get; set; }
        
        public static implicit operator TreeWalker(TokenType tokenType) => new TokenWalker(tokenType);
        public abstract void Walk(ParseTree parseTree);
        public virtual void EOF(ParseTree parseTree) {}
    }
    class TokenWalker : TreeWalker
    {
        private readonly TokenType _tokenType;

        public TokenWalker(TokenType type)
        {
            _tokenType = type;
        }

        public override void Walk(ParseTree parseTree)
        {
            if (parseTree.CurrentToken().TokenType == _tokenType)
            {
                parseTree.Advance();
                parseTree.Accept();
            }
            else
                parseTree.Diagnostic("Syntax error, '" + _tokenType.Name() + "' expected", parseTree.CurrentToken().Range);
        }

        public override void EOF(ParseTree parseTree)
            => parseTree.Diagnostic("Syntax error, '" + _tokenType.Name() + "' expected", parseTree.LastToken().Range);
        
        public override string ToString() => _tokenType.ToString();
    }

    class Sequence : TreeWalker
    {
        private readonly TreeWalker[] _children;

        public Sequence(params TreeWalker[] children)
        {
            _children = children;
        }

        public override void Walk(ParseTree parseTree)
        {
            ParseTree sequenceRoot = parseTree.Child();

            for (int i = 0; i < _children.Length;)
            {
                bool broke = false;
                for (int l = i; l < _children.Length; l++)
                {                    
                    ParseTree childTree = sequenceRoot.Child();
                    _children[l].Walk(childTree);
                    
                    sequenceRoot.Moosh(childTree);

                    if (childTree.WasAdvanced || childTree.ContinueEvenWithNoAdvance)
                    {
                        i = l + 1;
                        if (sequenceRoot.ReachedEnd) break;
                        else broke = true;
                        break;
                    }
                    else if (_children[i].Required) break;
                }
                if (sequenceRoot.ReachedEnd)
                {
                    for (; i < _children.Length; i++)
                        _children[i].EOF(sequenceRoot);
                    break;
                }
                if (!broke)
                    break;
            }

            if (sequenceRoot.WasAdvanced)
                parseTree.Moosh(sequenceRoot);
        }

        public override string ToString() => "[" + string.Join(", ", _children.Select(c => c.ToString())) + "]";
    }
    class Alt : TreeWalker
    {
        private readonly TreeWalker[] _options;
        private readonly string _alias;

        public Alt(params TreeWalker[] options)
        {
            _options = options;
        }
        public Alt(string alias, params TreeWalker[] options)
        {
            _alias = alias;
            _options = options;
        }

        public override void Walk(ParseTree parseTree)
        {
            bool anyFound = false;

            ParseTree[] optionTrees = new ParseTree[_options.Length];
            for (int i = 0; i < _options.Length; i++)
            {
                optionTrees[i] = parseTree.Child();
                _options[i].Walk(optionTrees[i]);
                if (optionTrees[i].Accepted)
                {
                    parseTree.Accept();
                    anyFound = true;
                }
            }

            // Syntax error if none was found.
            if (!anyFound)
            {
                if (_alias != null)
                    parseTree.Diagnostic(string.Format(_alias, parseTree.CurrentToken().TokenType.Name()), parseTree.CurrentToken().Range);
                else
                    // TODO
                    throw new NotImplementedException();
            }
            else
            {
                foreach (ParseTree optionTree in optionTrees)
                    if (optionTree.Accepted)
                    {
                        parseTree.Moosh(optionTree);
                        break;
                    }
            }
        }

        public override string ToString() => string.Join(" | ", _options.Select(op => op.ToString()));
    }
    class Repeat0 : TreeWalker
    {
        private readonly TreeWalker _rule;

        public Repeat0(TreeWalker rule)
        {
            _rule = rule;
        }

        public override void Walk(ParseTree parseTree)
        {
            parseTree.ContinueEvenWithNoAdvance = true;

            ParseTree last;
            do
            {
                last = parseTree.Child();
                _rule.Walk(last);
                if (last.Accepted && last.WasAdvanced) parseTree.Moosh(last);
            }
            while (last.Accepted && last.WasAdvanced && !last.ReachedEnd);
        }

        public override string ToString() => "rep(" + _rule.ToString() + ")";
    }
    class Optional : TreeWalker
    {
        private readonly TreeWalker _walker;

        public Optional(TreeWalker walker)
        {
            _walker = walker;
        }
        
        public override void Walk(ParseTree parseTree)
        {
            var walkTree = parseTree.Child();
            _walker.Walk(walkTree);
            if (walkTree.Accepted && walkTree.WasAdvanced) parseTree.Moosh(walkTree);
        }

        public override string ToString() => "?(" + _walker.ToString() + ")?";
    }

    public class ParserDiagnostic
    {
        public string Message { get; }
        public DocRange Range { get; }

        public ParserDiagnostic(string message, DocRange range)
        {
            Message = message;
            Range = range;
        }

        public override string ToString() => Message;
    }
}