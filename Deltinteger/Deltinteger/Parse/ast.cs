using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Deltin.Deltinteger.Checker;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class BuildAstVisitor : DeltinScriptBaseVisitor<Node>
    {
        Pos _caretPos;
        public List<Node> SelectedNode { get; private set; } = new List<Node>();

        public BuildAstVisitor(Pos caretPos)
        {
            _caretPos = caretPos;
        }

        public override Node VisitRuleset(DeltinScriptParser.RulesetContext context)
        {
            RuleNode[] rules = new RuleNode[context.ow_rule().Length];
            for (int i = 0; i < rules.Length; i++)
                rules[i] = (RuleNode)VisitOw_rule(context.ow_rule()[i]);

            Variable useGlobalVar;
            Variable usePlayerVar;
            Enum.TryParse<Variable>(context.useGlobalVar()?.PART().GetText(), out useGlobalVar);
            Enum.TryParse<Variable>(context.usePlayerVar()?.PART().GetText(), out usePlayerVar);

            DefinedNode[] definedVars = new DefinedNode[context.vardefine().Length];
            for (int i = 0; i < definedVars.Length; i++)
                definedVars[i] = (DefinedNode)VisitVardefine(context.vardefine()[i]);

            UserMethodNode[] userMethods = new UserMethodNode[context.user_method().Length];
            for (int i = 0; i < userMethods.Length; i++)
                userMethods[i] = (UserMethodNode)VisitUser_method(context.user_method()[i]);
            
            Node node = new RulesetNode(rules, useGlobalVar, usePlayerVar, definedVars, userMethods, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitVardefine(DeltinScriptParser.VardefineContext context)
        {
            string variableName = context.PART(0).GetText();
            bool isGlobal = context.GLOBAL() != null;

            Variable? useVar = null;
            if (Enum.TryParse<Variable>(context.PART().ElementAtOrDefault(1)?.GetText(), out Variable setUseVar))
                useVar = setUseVar;
            
            int? useIndex = null;
            if (int.TryParse(context.number()?.GetText(), out int setUseIndex))
                useIndex = setUseIndex;

            Node node = new DefinedNode(isGlobal, variableName, useVar, useIndex, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitUser_method(DeltinScriptParser.User_methodContext context)
        {
            string name = context.PART(0).GetText();

            string[] parameters = new string[context.PART().Length - 1];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = context.PART(i + 1).GetText();

            BlockNode block = (BlockNode)VisitBlock(context.block());

            Node node = new UserMethodNode(name, parameters, block, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitOw_rule(DeltinScriptParser.Ow_ruleContext context)
        {
            string name = context.STRINGLITERAL().GetText().Trim('"');
            BlockNode block = (BlockNode)VisitBlock(context.block());

            IExpressionNode[] conditions = new IExpressionNode[context.rule_if()?.expr().Length ?? 0];
            for (int i = 0; i < conditions.Length; i++)
                conditions[i] = (IExpressionNode)VisitExpr(context.rule_if().expr()[i]);

            var node = new RuleNode(name, conditions, block, Range.GetRange(context));
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
            if (context.exception != null)
                return null;
                
            Node node;

            // Operations
            if (context.ChildCount == 3 && Constants.AllOperations.Contains(context.GetChild(1).GetText()))
            {
                IExpressionNode left = (IExpressionNode)Visit(context.GetChild(0));
                string operation = context.GetChild(1).GetText();
                IExpressionNode right = (IExpressionNode)Visit(context.GetChild(2));

                node = new OperationNode(left, operation, right, Range.GetRange(context));
            }

            // Getting values in arrays
            else if (context.ChildCount == 4
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "["
            && context.GetChild(2) is DeltinScriptParser.ExprContext
            && context.GetChild(3).GetText() == "]")
            {
                IExpressionNode value = (IExpressionNode)Visit(context.GetChild(0));
                IExpressionNode index = (IExpressionNode)Visit(context.GetChild(2));

                node = new ValueInArrayNode(value, index, Range.GetRange(context));
            }

            // Seperator
            else if (context.ChildCount == 3
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "."
            && context.GetChild(2) is DeltinScriptParser.VariableContext)
            {
                string name = context.GetChild(2).GetText();
                IExpressionNode target = (IExpressionNode)Visit(context.GetChild(0));

                node = new VariableNode(name, target, Range.GetRange(context));
            }

            else
            {
                return Visit(context.GetChild(0));
            }

            CheckRange(node);
            return node;
        }
        
        #region Expressions
        // -123.456
        public override Node VisitNumber(DeltinScriptParser.NumberContext context)
        {
            if (context.exception != null)
                return null;

            double value = double.Parse(context.GetText());
            Node node = new NumberNode(value, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        // "Hello <0>! Waiting game..."
        public override Node VisitString(DeltinScriptParser.StringContext context)
        {
            if (context.exception != null)
                return null;

            string value = context.STRINGLITERAL().GetText().Trim('"');
            Node node = new StringNode(value, null, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        // <"hello <0>! Waiting game...", EventPlayer()>
        public override Node VisitFormatted_string(DeltinScriptParser.Formatted_stringContext context)
        {
            if (context.exception != null)
                return null;
                
            string value = context.@string().GetText().Trim('"');
            IExpressionNode[] format = new IExpressionNode[context.expr().Length];
            for (int i = 0; i < format.Length; i++)
                format[i] = (IExpressionNode)VisitExpr(context.expr()[i]);
            Node node = new StringNode(value, format, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        // Method()
        public override Node VisitMethod(DeltinScriptParser.MethodContext context)
        {
            if (context.exception != null)
                return null;
            
            string methodName = context.PART().GetText();

            // TODO check null check spots in [].
            IExpressionNode[] parameters = new IExpressionNode[context.parameters()?.expr().Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = (IExpressionNode)Visit(context.parameters().expr()[i]);

            Node node = new MethodNode(methodName, parameters, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitVariable(DeltinScriptParser.VariableContext context)
        {
            if (context.exception != null)
                return null;

            string name = context.PART().GetText();
            Node node = new VariableNode(name, null, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        // ( expr )
        public override Node VisitExprgroup(DeltinScriptParser.ExprgroupContext context)
        {
            return Visit(context.GetChild(0));
        }

        public override Node VisitTrue(DeltinScriptParser.TrueContext context)
        {
            Node node = new BooleanNode(true, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitFalse(DeltinScriptParser.FalseContext context)
        {
            Node node = new BooleanNode(false, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitNot(DeltinScriptParser.NotContext context)
        {
            IExpressionNode value = (IExpressionNode)Visit(context.GetChild(0));
            Node node = new NotNode(value, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitNull(DeltinScriptParser.NullContext context)
        {
            Node node = new NullNode(Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string[] split = context.GetText().Split('.');
            string type = split[0];
            string value = split[1];
            Node node = new EnumNode(type, value, Range.GetRange(context));
            CheckRange(node);
            return node;
        }
        
        public override Node VisitCreatearray(DeltinScriptParser.CreatearrayContext context)
        {
            IExpressionNode[] values = new IExpressionNode[context.expr().Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = (IExpressionNode)VisitExpr(context.expr()[i]);

            Node node = new CreateArrayNode(values, Range.GetRange(context));
            CheckRange(node);
            return node;
        }
        #endregion
        
        public override Node VisitStatement(DeltinScriptParser.StatementContext context)
        {
            if (context.exception != null)
                return null;
            
            Node node = null;

            if (node == null)
            {
                if (context.GetChild(0) != null)
                    return Visit(context.GetChild(0));
                else
                    return null;
            }
            else
            {
                CheckRange(node);
                return node;
            }
        }

        #region Statements
        public override Node VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            if (context.exception != null)
                return null;

            IExpressionNode target = context.expr().Length == 2 ? (IExpressionNode)Visit(context.expr()[0]) : null;
            string variable = context.PART().GetText();
            IExpressionNode index = (IExpressionNode)Visit(context.array().expr());
            string operation = context.statement_operation().GetText();
            IExpressionNode value = (IExpressionNode)Visit(context.expr().Last());

            Node node = new VarSetNode(target, variable, index, operation, value, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitFor(DeltinScriptParser.ForContext context)
        {
            if (context.exception != null)
                return null;
            
            IExpressionNode array = (IExpressionNode)Visit(context.expr());
            string variable = context.PART().GetText();
            BlockNode block = (BlockNode)VisitBlock(context.block());

            Node node = new ForEachNode(variable, array, block, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitIf(DeltinScriptParser.IfContext context)
        {
            if (context.exception != null)
                return null;
            
            // Get the if data
            IfData ifData = new IfData
            (
                (IExpressionNode)VisitExpr(context.expr()),
                (BlockNode)VisitBlock(context.block())
            );

            // Get the else-if data
            IfData[] elseIfData = null;
            if (context.else_if() != null)
            {
                elseIfData = new IfData[context.else_if().Length];
                for (int i = 0; i < context.else_if().Length; i++)
                    elseIfData[i] = new IfData
                    (
                        (IExpressionNode)VisitExpr(context.else_if()[i].expr()),
                        (BlockNode)VisitBlock(context.else_if()[i].block())
                    );
            }
            
            // Get the else block
            BlockNode elseBlock = null;
            if (context.@else() != null)
                elseBlock = (BlockNode)VisitBlock(context.@else().block());

            Node node = new IfNode(ifData, elseIfData, elseBlock, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitReturn(DeltinScriptParser.ReturnContext context)
        {
            if (context.exception != null)
                return null;
            
            IExpressionNode returnValue = null;
            if (context.expr() != null)
                returnValue = (IExpressionNode)VisitExpr(context.expr());

            Node node = new ReturnNode(returnValue, Range.GetRange(context));
            CheckRange(node);
            return node;
        }

        public override Node VisitDefine(DeltinScriptParser.DefineContext context)
        {
            if (context.exception != null)
                return null;

            string variableName = context.PART().GetText();
            IExpressionNode value = (IExpressionNode)VisitExpr(context.expr());

            Node node = new ScopedDefineNode(variableName, value, Range.GetRange(context));
            CheckRange(node);
            return node;
        }
        #endregion

        private void CheckRange(Node node)
        {
            if (_caretPos == null)
                return;

            // If the caret position is inside the node
            // and the node's range is less than the currently selected node.
            if ((node.Range.IsInside(_caretPos)) /*&& (SelectedNode == null || node.Range < SelectedNode.Range)*/)
            {
                //SelectedNode = node;
                SelectedNode.Add(node);
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
        public RuleNode[] Rules { get; private set; }
        public Variable UseGlobalVar { get; private set; }
        public Variable UsePlayerVar { get; private set; }
        public DefinedNode[] DefinedVars { get; private set; }
        public UserMethodNode[] UserMethods { get; private set; }

        public RulesetNode(
            RuleNode[] rules, 
            Variable useGlobalVar, 
            Variable usePlayerVar, 
            DefinedNode[] definedVars, 
            UserMethodNode[] userMethods, 
            Range range) : base(range)
        {
            Rules = rules;
            UseGlobalVar = useGlobalVar;
            UsePlayerVar = usePlayerVar;
            DefinedVars = definedVars;
            UserMethods = userMethods;
        }
    }

    public class DefinedNode : Node
    {
        public bool IsGlobal { get; private set; }
        public string VariableName { get; private set; }
        public Variable? UseVar { get; private set; }
        public int? UseIndex { get; private set; }

        public DefinedNode(bool isGlobal, string variableName, Variable? useVar, int? useIndex, Range range) : base (range)
        {
            IsGlobal = isGlobal;
            VariableName = variableName;
            UseVar = useVar;
            UseIndex = useIndex;
        }
    }

    public class RuleNode : Node
    {
        public string Name { get; private set; }
        public IExpressionNode[] Conditions { get; private set; }
        public BlockNode Block { get; private set; }

        public RuleNode(string name, IExpressionNode[] conditions, BlockNode block, Range range) : base(range)
        {
            Name = name;
            Conditions = conditions;
            Block = block;
        }
    }

    public class UserMethodNode : Node, INamedNode
    {
        public string Name { get; private set; }
        public string[] Parameters { get; private set; }
        public BlockNode Block { get; private set; }
        
        public UserMethodNode(string name, string[] parameters, BlockNode block, Range range) : base(range)
        {
            Name = name;
            Parameters = parameters;
            Block = block;
        }
    }

    // TODO maybe remove IExpressionNode and IStatementNode if empty interfaces is a bad coding practice?
    public interface IExpressionNode {}
    public interface IStatementNode {}

    public interface INamedNode 
    {
        string Name { get; }
    }

    public class OperationNode : Node, IExpressionNode
    {
        public IExpressionNode Left { get; }
        public string Operation { get; }
        public IExpressionNode Right { get; }

        public OperationNode(IExpressionNode left, string operation, IExpressionNode right, Range range) : base(range)
        {
            Left = left;
            Operation = operation;
            Right = right;
        }
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
        public IExpressionNode Target { get; private set; }

        public VariableNode(string name, IExpressionNode target, Range range) : base(range)
        {
            Name = name;
            Target = target;
        }
    }

    public class NumberNode : Node, IExpressionNode
    {
        public double Value;

        public NumberNode(double value, Range range) : base(range)
        {
            Value = value;
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

    public class BooleanNode : Node, IExpressionNode
    {
        public bool Value { get; private set; }

        public BooleanNode(bool value, Range range) : base (range)
        {
            Value = value;
        }
    }

    public class NotNode : Node, IExpressionNode
    {
        public IExpressionNode Value;

        public NotNode(IExpressionNode value, Range range) : base(range)
        {
            Value = value;
        }
    }

    public class NullNode : Node, IExpressionNode
    {
        public NullNode(Range range) : base(range) {}
    }

    public class EnumNode : Node, IExpressionNode
    {
        public string Type { get; private set; }
        public string Value { get; private set; }

        public EnumNode(string type, string value, Range range) : base(range)
        {
            Type = type;
            Value = value;
        }
    }

    public class ValueInArrayNode : Node, IExpressionNode
    {
        public IExpressionNode Value { get; private set; }
        public IExpressionNode Index { get; private set; }

        public ValueInArrayNode(IExpressionNode value, IExpressionNode index, Range range) : base(range)
        {
            Value = value;
            Index = index;
        }
    }

    public class CreateArrayNode : Node, IExpressionNode
    {
        public IExpressionNode[] Values { get; private set; }
        public CreateArrayNode(IExpressionNode[] values, Range range) : base(range)
        {
            Values = values;
        }
    }

    public class VarSetNode : Node, IStatementNode
    {
        public IExpressionNode Target { get; private set; }
        public string Variable { get; private set; }
        public IExpressionNode Index { get; private set; }
        public string Operation { get; private set; }
        public IExpressionNode Value { get; private set; }

        public VarSetNode(IExpressionNode target, string variable, IExpressionNode index, string operation, IExpressionNode value, Range range) : base(range)
        {
            Target = target;
            Variable = variable;
            Index = index;
            Operation = operation;
            Value = value;
        }
    }

    public class ForEachNode : Node, IStatementNode
    {
        public IExpressionNode Array { get; private set; }
        public string Variable { get; private set; }
        public BlockNode Block { get; private set; }

        public ForEachNode(string variable, IExpressionNode array, BlockNode block, Range range) : base(range)
        {
            Array = array;
            Variable = variable;
            Block = block;
        }
    }

    public class IfNode : Node, IStatementNode
    {
        public IfData IfData { get; private set; }
        public IfData[] ElseIfData { get; private set; }
        public BlockNode ElseBlock { get; private set; }

        public IfNode(IfData ifData, IfData[] elseIfData, BlockNode elseBlock, Range range) : base(range)
        {
            IfData = ifData;
            ElseIfData = elseIfData;
            ElseBlock = elseBlock;
        }
    }

    public class IfData
    {
        public IExpressionNode Expression { get; private set; }
        public BlockNode Block { get; private set; }

        public IfData(IExpressionNode expression, BlockNode block)
        {
            Expression = expression;
            Block = block;
        }
    }

    public class ReturnNode : Node, IStatementNode
    {
        public IExpressionNode Value { get; private set; }

        public ReturnNode(IExpressionNode value, Range range) : base (range)
        {
            Value = value;
        }
    }

    public class ScopedDefineNode : Node, IStatementNode
    {
        public string VariableName { get; private set; }
        public IExpressionNode Value { get; private set; }

        public ScopedDefineNode(string variableName, IExpressionNode value, Range range) : base (range)
        {
            VariableName = variableName;
            Value = value;
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

        public static Range GetRange(ParserRuleContext context)
        {
            if (context.start.Line == context.stop.Line &&
                context.start.Column == context.stop.Column)
            {
                return new Range
                (
                    new Pos(context.start.Line, context.start.Column), 
                    new Pos(context.stop.Line, context.stop.Column + context.GetText().Length)
                );
            }
            else
            {
                return new Range
                (
                    new Pos(context.start.Line, context.start.Column), 
                    new Pos(context.stop.Line, context.stop.Column)
                );
            }
        }

        public static Range GetRange(IToken token)
        {
            return new Range(new Pos(token.Line, token.StartIndex), new Pos(token.Line, token.StopIndex));
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
    
        public Range LanguageServerOffset()
        {
            return new Range(new Pos(start.line - 1, start.character), new Pos(end.line - 1, end.character));
        }
    }
}