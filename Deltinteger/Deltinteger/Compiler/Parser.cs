using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parser
{
    public class Parser
    {
        public Lexer Lexer { get; }

        public ParserRule Root = new ParserRule();
        public ParserRule DeclareRule = new ParserRule();
        public ParserRule Statement = new ParserRule();
        public ParserRule Expression = new ParserRule();
        
        public Parser(Lexer lexer)
        {
            Lexer = lexer;

            // Root of the file.
            Root.Init(rep(DeclareRule));

            // Rule declaration.
            DeclareRule.Init(seq(
                TokenType.Rule, TokenType.Colon, TokenType.String, // rule: ""
                rep(seq(TokenType.Identifier, TokenType.Dot, TokenType.Identifier)), // Rule options
                rep(seq(TokenType.If, TokenType.Parentheses_Open, Expression, TokenType.Parentheses_Close)) // Conditions
            ));

            // Expressions.
            Expression.Init(alt("Invalid expression term '{0}'",
                TokenType.String,
                TokenType.Number,
                TokenType.True,
                TokenType.False
            ));
        }

        public void Parse(TreeWalker rule)
        {
            ParseTree root = new ParseTree(Lexer);
            rule.Walk(root);
            root.Debug();
        }

        private static Sequence seq(params TreeWalker[] parts) => new Sequence(parts);
        private static Repeat0 rep(TreeWalker part) => new Repeat0(part);
        // private static Alt alt(params TreeWalker[] options) => new Alt(options);
        private static Alt alt(string alias, params TreeWalker[] options) => new Alt(alias, options);
    }

    public class ParseTree
    {
        public int Position { get; private set; }
        public Lexer Lexer { get; }
        public List<ParserDiagnostic> Diagnostics { get; } = new List<ParserDiagnostic>();
        public bool Accepted { get; private set; }
        public bool ReachedEnd => Position == Lexer.Tokens.Count;
        public bool WasAdvanced { get; private set; }
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
            Position = parseTree.Position;
            WasAdvanced = true;
            Diagnostics.AddRange(parseTree.Diagnostics);
            _children.AddRange(parseTree._children);
            Accept();
        }

        public ParseTree Child() => new ParseTree(this);

        /// <summary>Gets the current token.</summary>
        public Token CurrentToken() => Lexer.Tokens[Position];

        /// <summary>The diagnostics that will be displayed if this tree is chosen.</summary>
        /// <param name="message">The message that will be displayed.</param>
        /// <param name="range">The range of the diagnostic.</param>
        public void Diagnostic(string message, DocRange range) => Diagnostics.Add(new ParserDiagnostic(message, range));

        /// <summary>Accepts the current tree.</summary>
        public void Accept()
        {
            Accepted = true;
            if (_parent != null) _parent.Position = Position;
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
        public static implicit operator TreeWalker(TokenType tokenType) => new TokenWalker(tokenType);
        public abstract void Walk(ParseTree parseTree);
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
                parseTree.Diagnostic("Syntax error, '" + _tokenType + "' expected", parseTree.CurrentToken().Range);
        }
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
            foreach (TreeWalker child in _children)
            {
                child.Walk(parseTree);
                if (parseTree.ReachedEnd) return;
            }
        }
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

            foreach (TreeWalker option in _options)
            {
                ParseTree optionTree = parseTree.Child();
                option.Walk(optionTree);
                if (optionTree.Accepted)
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
                // parseTree.Accept();
            }
        }
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
            ParseTree last;
            do
            {
                last = parseTree.Child();
                _rule.Walk(last);
                if (last.Accepted && last.WasAdvanced) parseTree.Moosh(last);
            }
            while (last.Accepted && last.WasAdvanced && !last.ReachedEnd);
        }
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