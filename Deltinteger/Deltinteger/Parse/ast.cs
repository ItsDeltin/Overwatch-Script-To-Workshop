using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Deltin.Deltinteger.Checker;

namespace Deltin.Deltinteger.Parse
{
    public class BuildAstVisitor : DeltinScriptBaseVisitor<Node>
    {
        Pos _caretPos;
        public Node SelectedNode { get; private set; }

        public BuildAstVisitor(Pos caretPos = null)
        {
            _caretPos = caretPos;
        }

        public override Node VisitRuleset(DeltinScriptParser.RulesetContext context)
        {
            RuleNode[] rules = new RuleNode[context.ow_rule().Length];
            for (int i = 0; i < rules.Length; i++)
                rules[i] = (RuleNode)VisitOw_rule(context.ow_rule()[i]);
            
            var node = new RulesetNode(rules, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitOw_rule(DeltinScriptParser.Ow_ruleContext context)
        {
            string name = context.STRINGLITERAL().GetText().Trim('"');
            BlockNode block = (BlockNode)VisitBlock(context.block());

            var node = new RuleNode(name, block, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitBlock(DeltinScriptParser.BlockContext context)
        {
            IStatementNode[] statements = new IStatementNode[context.statement().Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = (IStatementNode)VisitStatement(context.statement()[i]);
            
            var node = new BlockNode(statements, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitExpr(DeltinScriptParser.ExprContext context)
        {
            switch (context.GetChild(0).GetType().Name)
            {
                case nameof(DeltinScriptParser.NumberContext):
                case nameof(DeltinScriptParser.StringContext):
                case nameof(DeltinScriptParser.Formatted_stringContext):
                case nameof(DeltinScriptParser.MethodContext):
                case nameof(DeltinScriptParser.VariableContext):
                    return Visit(context.GetChild(0));
            }

            throw new Exception($"Can't translate expression {context.GetText()} of type {context.GetType().Name}");
        }
        
        #region Expressions
        // -123.456
        public override Node VisitNumber(DeltinScriptParser.NumberContext context)
        {
            double value = double.Parse(context.GetText());
            return new NumberNode(value, Range.GetRange(context));
        }

        // "Hello <0>! Waiting game..."
        public override Node VisitString(DeltinScriptParser.StringContext context)
        {
            string value = context.STRINGLITERAL().GetText();
            return new StringNode(value, null, Range.GetRange(context));
        }

        // <"hello <0>! Waiting game...", EventPlayer()>
        public override Node VisitFormatted_string(DeltinScriptParser.Formatted_stringContext context)
        {
            string value = context.@string().GetText();
            IExpressionNode[] format = new IExpressionNode[context.expr().Length];
            for (int i = 0; i < format.Length; i++)
                format[i] = (IExpressionNode)VisitExpr(context.expr()[i]);
            return new StringNode(value, format, Range.GetRange(context));
        }

        // Method()
        public override Node VisitMethod(DeltinScriptParser.MethodContext context)
        {
            string methodName = context.PART().GetText();

            // TODO check null check spots in [].
            IExpressionNode[] parameters = new IExpressionNode[context.parameters()?.expr()?.Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = (IExpressionNode)Visit(context.parameters().expr()[i]);

            Node node = new MethodNode(methodName, parameters, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitVariable(DeltinScriptParser.VariableContext context)
        {
            string name = context.PART().GetText();
            Node node = new VariableNode(name, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        // ( expr )
        public override Node VisitExprgroup(DeltinScriptParser.ExprgroupContext context)
        {
            return Visit(context.GetChild(0));
        }
        #endregion
        
        private void CheckRange(Node node)
        {
            // If the caret position is inside the node
            // and the node's range is less than the currently selected node.
            if ((node.Range.IsInside(_caretPos)) && (SelectedNode == null || node.Range < SelectedNode.Range))
            {
                SelectedNode = node;
            }
        }
    }

    public abstract class Node
    {
        public Range Range { get; private set; }

        public Node(Range range)
        {
            Range = range;
        }
    }

    public class RulesetNode : Node
    {
        public RuleNode[] Rules;

        public RulesetNode(RuleNode[] rules, Range range) : base(range)
        {
            Rules = rules;
        }
    }

    public class RuleNode : Node
    {
        public string Name { get; private set; }
        public BlockNode Block { get; private set; }

        public RuleNode(string name, BlockNode block, Range range) : base(range) 
        {
            Name = name;
            Block = block;
        }
    }

    public interface IExpressionNode {}

    public interface IStatementNode {}

    public interface INamedNode 
    {
        string Name { get; }
    }

    public class BlockNode : Node
    {
        public IStatementNode[] Statements;

        public BlockNode(IStatementNode[] statements, Range range) : base(range) 
        {
            Statements = statements;
        }
    }

    public class MethodNode : Node, IExpressionNode, IStatementNode, INamedNode
    {
        public string Name { get; private set; }
        public IExpressionNode[] Parameters { get; private set; }

        public MethodNode(string name, IExpressionNode[] parameters, Range range) : base(range)
        {
            Name = name;
            Parameters = parameters;
        }
    }

    public class VariableNode : Node, IExpressionNode, INamedNode
    {
        public string Name { get; private set; }

        public VariableNode(string name, Range range) : base(range)
        {
            Name = name;
        }
    }

    public class VarSetNode : Node, IStatementNode
    {
        public VariableNode Variable { get; private set; }
        public IExpressionNode Player { get; private set; }
        public IExpressionNode Index { get; private set; }
        public IExpressionNode Value { get; private set; }

        public VarSetNode(VariableNode variable, IExpressionNode player, IExpressionNode index, IExpressionNode value, Range range) : base(range)
        {
            Variable = variable;
            Player = player;
            Index = index;
            Value = value;
        }
    }

    public class NumberNode : Node, IExpressionNode
    {
        public double Value;

        public NumberNode(double value, Range range) : base(range)
        {

        }
    }

    public class StringNode : Node, IExpressionNode
    {
        public string Value { get; private set; }
        public IExpressionNode[] Format { get; private set; }

        public StringNode(string value, IExpressionNode[] format, Range range) : base (range)
        {
            Value = value;
            Format = format;
        }
    }

    public class Pos
    {
        public int line { get; private set; }
        public int character { get; private set; }

        public Pos(int line, int character)
        {
            this.line = line;
            this.character = character;
        }
    }

    public class Range : IComparable<Range>
    {
        public Pos start { get; private set; }
        public Pos end { get; private set; }

        public Range(Pos start, Pos end)
        {
            this.start = start;
            this.end = end;
        }

        public static Range GetRange(IToken start, IToken end)
        {
            return new Range(new Pos(start.Line, start.Column), new Pos(end.Line, end.Column));
        }
        public static Range GetRange(ParserRuleContext context)
        {
            if (context.start.Line == context.stop.Line &&
                context.start.Column == context.stop.Column)
                return new Range(new Pos(context.start.Line, context.start.Column), new Pos(context.stop.Line, context.stop.Column + context.GetText().Length));
            return GetRange(context.Start, context.Stop);
        }
        public bool IsInside(Pos pos)
        {
            return (start.line < pos.line || (start.line == pos.line && pos.character >= start.character))
                && (end.line > pos.line || (end.line == pos.line && pos.character <= end.character));
        }

        public int CompareTo(Range other)
        {
            // Return -1 if 'this' is less than 'other'.
            // Return 0 if 'this' is equal to 'other'.
            // Return 1 if 'this' is greater than 'other'.

            // This is greater if other is null.
            if (other == null)
                return 1;

            // Get the number of lines 'start' and 'stop' contain.
            int thisLineDif = this.end.line - this.start.line; 
            int otherLineDif = other.end.line - other.start.line;

            // If 'this' has less lines than 'other', return less than.
            if (thisLineDif < otherLineDif)
                return -1;

            // If 'this' has more lines than 'other', return greater than.
            if (thisLineDif > otherLineDif)
                return 1;
            
            // If the amount of lines are equal, compare by character offset.
            if (thisLineDif == otherLineDif)
            {
                int thisCharDif = this.end.character - this.start.character;
                int otherCharDif = other.end.character - other.end.character;

                // Return less-than.
                if (thisCharDif < otherCharDif)
                    return -1;
                
                // Return equal.
                if (thisCharDif == otherCharDif)
                    return 0;

                // Return greater-than.
                if (thisCharDif > otherCharDif)
                    return 1;
            }

            // This isn't possible.
            throw new Exception();
        }

        #region Operators
        public static bool operator <(Range r1, Range r2)  => r1.CompareTo(r2) <  0;
        public static bool operator >(Range r1, Range r2)  => r1.CompareTo(r2) >  0;
        public static bool operator <=(Range r1, Range r2) => r1.CompareTo(r2) <= 0;
        public static bool operator >=(Range r1, Range r2) => r1.CompareTo(r2) >= 0;
        #endregion
    }
}