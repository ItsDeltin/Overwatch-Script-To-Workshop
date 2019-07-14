using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class BuildAstVisitor : DeltinScriptBaseVisitor<Node>
    {
        List<Diagnostic> _diagnostics;

        public BuildAstVisitor(List<Diagnostic> diagnostics)
        {
            _diagnostics = diagnostics;
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
            
            return new RulesetNode(rules, useGlobalVar, usePlayerVar, definedVars, userMethods, Range.GetRange(context));
        }

        public override Node VisitVardefine(DeltinScriptParser.VardefineContext context)
        {
            string variableName = context.PART().GetText();
            bool isGlobal = context.GLOBAL() != null;

            UseVarNode useVar = null;
            if (context.useVar() != null)
                useVar = (UseVarNode)VisitUseVar(context.useVar());

            return new DefinedNode(isGlobal, variableName, useVar, Range.GetRange(context));
        }

        public override Node VisitDefine(DeltinScriptParser.DefineContext context)
        {
            string variableName = context.PART().GetText();
            
            Node value = null;
            if (context.expr() != null)
                value = VisitExpr(context.expr());

            UseVarNode useVar = null;
            if (context.useVar() != null)
                useVar = (UseVarNode)VisitUseVar(context.useVar());

            return new ScopedDefineNode(variableName, value, useVar, Range.GetRange(context));
        }

        public override Node VisitUseVar(DeltinScriptParser.UseVarContext context)
        {
            if (!Enum.TryParse<Variable>(context.PART().GetText(), out Variable variable))
            {
                _diagnostics.Add(new Diagnostic("Expected letter.", Range.GetRange(context)) { severity = Diagnostic.Error });
                return null;
            }
            
            int index = -1;
            if (context.number() != null)
                if (!int.TryParse(context.number().GetText(), out index))
                    index = -1;

            return new UseVarNode(variable, index, Range.GetRange(context));
        }

        public override Node VisitUser_method(DeltinScriptParser.User_methodContext context)
        {
            string name = context.PART(0).GetText();

            string[] parameters = new string[context.PART().Length - 1];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = context.PART(i + 1).GetText();

            BlockNode block = (BlockNode)VisitBlock(context.block());

            bool isRecursive = context.RECURSIVE() != null;

            string documentation = string.Join("\n\r", context.DOCUMENTATION().Select(doc => doc.GetText().TrimEnd().TrimStart('#', ' ')));

            return new UserMethodNode(name, parameters, block, isRecursive, documentation, Range.GetRange(context));
        }

        public override Node VisitOw_rule(DeltinScriptParser.Ow_ruleContext context)
        {
            string name = context.STRINGLITERAL().GetText().Trim('"');
            BlockNode block = (BlockNode)VisitBlock(context.block());

            Node[] conditions = new Node[context.rule_if().Length];
            Range[] conditionRanges      = new Range          [context.rule_if().Length];

            for (int i = 0; i < context.rule_if().Length; i++)
            {
                if (context.rule_if(i).expr() != null)
                    conditions[i] = VisitExpr(context.rule_if(i).expr());


                //conditionRanges[i] = Range.GetRange(context.rule_if(i));
                // Get the range between the ().
                conditionRanges[i] = Range.GetRange(
                    context.rule_if(i).LEFT_PAREN().Symbol, 
                    context.rule_if(i).RIGHT_PAREN().Symbol
                );
            }

            RuleEvent eventType = RuleEvent.OngoingGlobal;
            Team team = Team.All;
            PlayerSelector player = PlayerSelector.All;

            Range eventRange = null;
            Range teamRange = null;
            Range playerRange = null;
            foreach(var ruleOption in context.@enum())
            {
                string option = ruleOption.PART(0).GetText();
                Range optionRange = Range.GetRange(ruleOption.PART(0).Symbol);

                string value = ruleOption.PART(1)?.GetText();
                Range valueRange = null;
                if (value != null)
                    valueRange = Range.GetRange(ruleOption.PART(1).Symbol);
                
                switch (option)
                {
                    case "Event":
                        if (!Enum.TryParse<RuleEvent>(value, out eventType))
                            _diagnostics.Add(new Diagnostic($"{value} is not a valid Event type.", valueRange));
                        eventRange = Range.GetRange(ruleOption);
                        break;
                    
                    case "Team":
                        if (!Enum.TryParse<Team>(value, out team))
                            _diagnostics.Add(new Diagnostic($"{value} is not a valid Team type.", valueRange));
                        teamRange = Range.GetRange(ruleOption);
                        break;

                    case "Player":
                        if (!Enum.TryParse<PlayerSelector>(value, out player))
                            _diagnostics.Add(new Diagnostic($"{value} is not a valid Player type.", valueRange));
                        playerRange = Range.GetRange(ruleOption);
                        break;
                    
                    default:
                        _diagnostics.Add(new Diagnostic($"{option} is not a valid rule option.", optionRange));
                        break;
                }
            }

            return new RuleNode(name, eventType, team, player, conditions, block, eventRange, teamRange, playerRange, conditionRanges, Range.GetRange(context));
        }

        public override Node VisitBlock(DeltinScriptParser.BlockContext context)
        {
            Node[] statements = new Node[context.statement().Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = VisitStatement(context.statement()[i]);
            
            return new BlockNode(statements, Range.GetRange(context));
        }

        public override Node VisitExpr(DeltinScriptParser.ExprContext context)
        {
            Node node;
            
            // Operations
            if (context.ChildCount == 3 && Constants.AllOperations.Contains(context.GetChild(1).GetText()))
            {
                Node left = Visit(context.GetChild(0));
                string operation = context.GetChild(1).GetText();
                Node right = Visit(context.GetChild(2));


                node = new OperationNode(left, operation, right, Range.GetRange(context));
            }

            // Getting values in arrays
            else if (context.ChildCount == 4
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "["
            && context.GetChild(2) is DeltinScriptParser.ExprContext
            && context.GetChild(3).GetText() == "]")
            {
                Node value = Visit(context.GetChild(0));
                Node index = Visit(context.GetChild(2));

                node = new ValueInArrayNode(value, index, Range.GetRange(context));
            }

            // Seperator
            else if (context.ChildCount == 3
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "."
            && context.GetChild(2) is DeltinScriptParser.VariableContext)
            {
                string name = context.GetChild(2).GetText();
                Node target = Visit(context.GetChild(0));

                node = new VariableNode(name, target, Range.GetRange(context));
            }
            
            // Not
            else if (context.ChildCount == 2
            && context.GetChild(0).GetText() == "!"
            && context.GetChild(1) is DeltinScriptParser.ExprContext)
            {
                Node value = Visit(context.GetChild(1));
                node = new NotNode(value, Range.GetRange(context));
            }

            // Ternary Condition
            else if (context.ChildCount == 5
            && context.GetChild(0) is DeltinScriptParser.ExprContext
            && context.GetChild(1).GetText() == "?"
            && context.GetChild(2) is DeltinScriptParser.ExprContext
            && context.GetChild(3).GetText() == ":"
            && context.GetChild(4) is DeltinScriptParser.ExprContext)
            {
                Node condition = VisitExpr(context.expr(0));
                Node consequent = VisitExpr(context.expr(1));
                Node alternative = VisitExpr(context.expr(2));
                node = new TernaryConditionalNode(condition, consequent, alternative, Range.GetRange(context));
            }

            else
            {
                return Visit(context.GetChild(0));
            }

            return node;
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
            string value = context.STRINGLITERAL().GetText().Trim('"');
            return new StringNode(value, null, Range.GetRange(context));
        }

        // <"hello <0>! Waiting game...", EventPlayer()>
        public override Node VisitFormatted_string(DeltinScriptParser.Formatted_stringContext context)
        {
            string value = context.@string().GetText().Trim('"');
            Node[] format = new Node[context.expr().Length];
            for (int i = 0; i < format.Length; i++)
                format[i] = VisitExpr(context.expr()[i]);
            return new StringNode(value, format, Range.GetRange(context));
        }

        // Method()
        public override Node VisitMethod(DeltinScriptParser.MethodContext context)
        {
            string methodName = context.PART().GetText();

            // TODO check null check spots in [].
            Node[] parameters = new Node[context.parameters()?.expr().Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Visit(context.parameters().expr()[i]);

            Range nameRange = Range.GetRange(context.PART().Symbol);
            Range parameterRange = Range.GetRange(context.LEFT_PAREN().Symbol, context.RIGHT_PAREN().Symbol);

            return new MethodNode(methodName, parameters, nameRange, parameterRange, Range.GetRange(context));
        }

        public override Node VisitVariable(DeltinScriptParser.VariableContext context)
        {
            string name = context.PART().GetText();
            return new VariableNode(name, null, Range.GetRange(context));
        }

        // ( expr )
        public override Node VisitExprgroup(DeltinScriptParser.ExprgroupContext context)
        {
            return Visit(context.expr());
        }

        public override Node VisitTrue(DeltinScriptParser.TrueContext context)
        {
            return new BooleanNode(true, Range.GetRange(context));
        }

        public override Node VisitFalse(DeltinScriptParser.FalseContext context)
        {
            return new BooleanNode(false, Range.GetRange(context));
        }

        public override Node VisitNull(DeltinScriptParser.NullContext context)
        {
            return new NullNode(Range.GetRange(context));
        }

        public override Node VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string[] split = context.GetText().Split('.');
            string type = split[0];
            string value = split[1];
            return new EnumNode(type, value, Range.GetRange(context));
        }
        
        public override Node VisitCreatearray(DeltinScriptParser.CreatearrayContext context)
        {
            Node[] values = new Node[context.expr().Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = VisitExpr(context.expr()[i]);

            return new CreateArrayNode(values, Range.GetRange(context));
        }
        #endregion
        
        public override Node VisitStatement(DeltinScriptParser.StatementContext context)
        {
            Node node = null;

            if (node == null)
            {
                return Visit(context.GetChild(0));
            }
            else
            {
                return node;
            }
        }

        #region Statements
        public override Node VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            Node target = context.expr().Length == 2 ? Visit(context.expr()[0]) : null;
            string variable = context.PART().GetText();
            
            Node index = null;
            if (context.array() != null)
                index = Visit(context.array().expr());

            Node value = context.expr().Length > 0 ? Visit(context.expr().Last()) : null;

            string operation = context.statement_operation()?.GetText();
            if (operation == null)
            {
                if (context.INCREMENT() != null)
                    operation = "++";
                else if (context.DECREMENT() != null)
                    operation = "--";
            }

            return new VarSetNode(target, variable, index, operation, value, Range.GetRange(context));
        }

        public override Node VisitFor(DeltinScriptParser.ForContext context)
        {
            BlockNode block = (BlockNode)VisitBlock(context.block());

            VarSetNode varSet = null;
            if (context.varset() != null)
                varSet = (VarSetNode)VisitVarset(context.varset());

            ScopedDefineNode defineNode = null;
            if (context.define() != null)
                defineNode = (ScopedDefineNode)VisitDefine(context.define());

            Node expression = null;
            if (context.expr() != null)
                expression = VisitExpr(context.expr());

            VarSetNode statement = null;
            if (context.forEndStatement() != null)
                statement = (VarSetNode)VisitVarset(context.forEndStatement().varset());
            
            return new ForNode(varSet, defineNode, expression, statement, block, Range.GetRange(context));
        }

        public override Node VisitForeach(DeltinScriptParser.ForeachContext context)
        {
            Node array = Visit(context.expr());

            string name = context.PART().GetText();

            BlockNode block = (BlockNode)VisitBlock(context.block());

            int repeaters = 1;
            if (context.number() != null)
                repeaters = int.Parse(context.number().GetText());
            
            return new ForEachNode(name, array, block, repeaters, Range.GetRange(context));
        }

        public override Node VisitWhile(DeltinScriptParser.WhileContext context)
        {
            BlockNode block = (BlockNode)VisitBlock(context.block());
            Node expression = VisitExpr(context.expr());

            return new WhileNode(expression, block, Range.GetRange(context));
        }

        public override Node VisitIf(DeltinScriptParser.IfContext context)
        {
            // Get the if data
            IfData ifData = new IfData
            (
                VisitExpr(context.expr()),
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
                        VisitExpr(context.else_if()[i].expr()),
                        (BlockNode)VisitBlock(context.else_if()[i].block())
                    );
            }
            
            // Get the else block
            BlockNode elseBlock = null;
            if (context.@else() != null)
                elseBlock = (BlockNode)VisitBlock(context.@else().block());

            return new IfNode(ifData, elseIfData, elseBlock, Range.GetRange(context));
        }

        public override Node VisitReturn(DeltinScriptParser.ReturnContext context)
        {
            Node returnValue = null;
            if (context.expr() != null)
                returnValue = VisitExpr(context.expr());

            return new ReturnNode(returnValue, Range.GetRange(context));
        }
        #endregion
    }

    public abstract class Node
    {
        public Range Range { get; private set; }

        public Range[] SubRanges { get; private set; }

        public Element RelatedElement { get; set; }

        public ScopeGroup RelatedScopeGroup { get; set; }

        public Node(Range range, params Range[] subRanges)
        {
            Range = range;
            SubRanges = subRanges;
        }

        public abstract Node[] Children();

        public Node[] SelectedNode(Pos caretPos)
        {
            List<Node> nodes = new List<Node>();
            SelectedNode(caretPos, nodes);
            return nodes.ToArray();
        }

        private void SelectedNode(Pos caretPos, List<Node> nodes)
        {
            if (Range.IsInside(caretPos))
                nodes.Insert(0, this);

            var children = Children();
            if (children != null)
                foreach(var child in children)
                    if (child != null)
                        child.SelectedNode(caretPos, nodes);
        }

        public int[] SubrangesSelected(Pos caretPos)
        {
            if (SubRanges != null)
            {
                List<int> inside = new List<int>();
                for (int i = 0; i < SubRanges.Length; i++)
                    if (SubRanges[i]?.IsInside(caretPos) ?? false)
                        inside.Add(i);
                return inside.ToArray();
            }
            return new int[0];
        }
    }

    public class RulesetNode : Node
    {
        public Variable UseGlobalVar { get; }
        public Variable UsePlayerVar { get; }
        public RuleNode[] Rules { get; }
        public DefinedNode[] DefinedVars { get; }
        public UserMethodNode[] UserMethods { get; }

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

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Rules, DefinedVars, UserMethods);
        }
    }

    public class DefinedNode : Node
    {
        public string VariableName { get; }
        public UseVarNode UseVar { get; }
        public bool IsGlobal { get; }

        public DefinedNode(bool isGlobal, string variableName, UseVarNode useVar, Range range) : base (range)
        {
            IsGlobal = isGlobal;
            VariableName = variableName;
            UseVar = useVar;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(UseVar);
        }
    }

    public class ScopedDefineNode : Node
    {
        public string VariableName { get; }
        public UseVarNode UseVar { get; }
        public Node Value { get; }

        public ScopedDefineNode(string variableName, Node value, UseVarNode useVar, Range range) : base (range)
        {
            VariableName = variableName;
            Value = value;
            UseVar = useVar;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(UseVar, Value);
        }
    }

    public class UseVarNode : Node
    {
        public Variable Variable { get; }
        public int Index { get; }
        public bool UsesIndex { get; }
        public UseVarNode(Variable variable, int index, Range range) : base (range)
        {
            Variable = variable;
            Index = index;
            UsesIndex = Index != -1;
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class RuleNode : Node
    {
        public string Name { get; private set; }
        public RuleEvent Event { get; private set; }
        public Team Team { get; private set; }
        public PlayerSelector Player { get; private set; }
        public Node[] Conditions { get; private set; }
        public BlockNode Block { get; private set; }

        public RuleNode(string name, RuleEvent eventType, Team team, PlayerSelector player, Node[] conditions, BlockNode block, 
            Range eventRange, Range teamRange, Range playerRange, Range[] conditionRanges, Range range) : base(range,
            new Range[] { eventRange, teamRange, playerRange}.Concat(conditionRanges).ToArray())
        {
            Name = name;

            Event = eventType;
            Team = team;
            Player = player;
            
            Conditions = conditions;
            Block = block;
        }

        public bool IsEventOptionSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(0);
        }

        public bool IsTeamOptionSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(1);
        }

        public bool IsPlayerOptionSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(2);
        }

        public bool IsIfSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Any(v => v > 2);
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Conditions, Block);
        }
    }

    public class UserMethodNode : Node
    {
        public string Name { get; }
        public string[] Parameters { get; }
        public BlockNode Block { get; }
        public bool IsRecursive { get; }
        public string Documentation { get; }
        
        public UserMethodNode(string name, string[] parameters, BlockNode block, bool isRecursive, string documentation, Range range) : base(range)
        {
            Name = name;
            Parameters = parameters;
            Block = block;
            IsRecursive = isRecursive;
            Documentation = documentation;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Block);
        }
    }

    public class OperationNode : Node
    {
        public Node Left { get; }
        public string Operation { get; }
        public Node Right { get; }

        public OperationNode(Node left, string operation, Node right, Range range) : base(range)
        {
            Left = left;
            Operation = operation;
            Right = right;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Left, Right);
        }
    }

    public class BlockNode : Node
    {
        public Node[] Statements;

        public BlockNode(Node[] statements, Range range) : base(range) 
        {
            Statements = statements;
        }

        public override Node[] Children()
        {
            return Statements;
        }
    }

    public class MethodNode : Node
    {
        public string Name { get; private set; }
        public Node[] Parameters { get; private set; }

        public MethodNode(string name, Node[] parameters, Range nameRange, Range parameterRange, Range range) : base(range, nameRange, parameterRange)
        {
            Name = name;
            Parameters = parameters;
        }

        public bool IsNameSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(0);
        }

        public bool IsParametersSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(1);
        }

        public override Node[] Children()
        {
            return Parameters;
        }
    }

    public class VariableNode : Node
    {
        public string Name { get; private set; }
        public Node Target { get; private set; }

        public VariableNode(string name, Node target, Range range) : base(range)
        {
            Name = name;
            Target = target;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Target);
        }
    }

    public class NumberNode : Node
    {
        public double Value;

        public NumberNode(double value, Range range) : base(range)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class StringNode : Node
    {
        public string Value { get; private set; }
        public Node[] Format { get; private set; }

        public StringNode(string value, Node[] format, Range range) : base (range)
        {
            Value = value;
            Format = format;
        }

        public override Node[] Children()
        {
            return Format;
        }
    }

    public class BooleanNode : Node
    {
        public bool Value { get; private set; }

        public BooleanNode(bool value, Range range) : base (range)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class NotNode : Node
    {
        public Node Value;

        public NotNode(Node value, Range range) : base(range)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class NullNode : Node
    {
        public NullNode(Range range) : base(range) {}

        public override Node[] Children()
        {
            return null;
        }
    }

    public class EnumNode : Node
    {
        public string Type { get; private set; }
        public string Value { get; private set; }
        public EnumMember EnumMember { get; private set; }

        public EnumNode(string type, string value, Range range) : base(range)
        {
            Type = type;
            Value = value;
            EnumMember = EnumData.GetEnumValue(type, value);
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class ValueInArrayNode : Node
    {
        public Node Value { get; private set; }
        public Node Index { get; private set; }

        public ValueInArrayNode(Node value, Node index, Range range) : base(range)
        {
            Value = value;
            Index = index;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value, Index);
        }
    }

    public class CreateArrayNode : Node
    {
        public Node[] Values { get; private set; }
        public CreateArrayNode(Node[] values, Range range) : base(range)
        {
            Values = values;
        }

        public override Node[] Children()
        {
            return Values;
        }
    }

    public class TernaryConditionalNode : Node
    {
        public Node Condition { get; private set; }
        public Node Consequent { get; private set; }
        public Node Alternative { get; private set; }
        public TernaryConditionalNode(Node condition, Node consequent, Node alternative, Range range) : base(range)
        {
            Condition = condition;
            Consequent = consequent;
            Alternative = alternative;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Condition, Consequent, Alternative);
        }
    }

    public class VarSetNode : Node
    {
        public Node Target { get; private set; }
        public string Variable { get; private set; }
        public Node Index { get; private set; }
        public string Operation { get; private set; }
        public Node Value { get; private set; }

        public VarSetNode(Node target, string variable, Node index, string operation, Node value, Range range) : base(range)
        {
            Target = target;
            Variable = variable;
            Index = index;
            Operation = operation;
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Target, Index, Value);
        }
    }

    public class ForEachNode : Node
    {
        public string VariableName { get; }
        public Node Array { get; }
        public BlockNode Block { get; }
        public int Repeaters { get; }

        public ForEachNode(string variableName, Node array, BlockNode block, int repeaters, Range range) : base(range)
        {
            VariableName = variableName;
            Array = array;
            Block = block;
            Repeaters = repeaters;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Array, Block);
        }
    }

    public class ForNode : Node
    {
        public VarSetNode VarSetNode { get; private set; }
        public ScopedDefineNode DefineNode { get; private set; }
        public Node Expression { get; private set; }
        public VarSetNode Statement { get; private set; }
        public BlockNode Block { get; private set; }

        public ForNode(VarSetNode varSetNode, ScopedDefineNode defineNode, Node expression, VarSetNode statement, BlockNode block, Range range) : base(range)
        {
            VarSetNode = varSetNode;
            DefineNode = defineNode;
            Expression = expression;
            Statement = statement;
            Block = block;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(VarSetNode, DefineNode, Expression, Statement, Block);
        }
    }

    public class WhileNode : Node
    {
        public Node Expression { get; private set; }
        public BlockNode Block { get; private set; }

        public WhileNode(Node expression, BlockNode block, Range range) : base(range)
        {
            Expression = expression;
            Block = block;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Expression, Block);
        }
    }

    public class IfNode : Node
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

        public override Node[] Children()
        {
            List<Node> children = new List<Node>();
            children.Add(IfData.Expression);
            children.Add(IfData.Block);
            
            foreach(IfData elseIf in ElseIfData)
            {
                children.Add(elseIf.Expression);
                children.Add(elseIf.Block);
            }

            if (ElseBlock != null)
                children.Add(ElseBlock);

            return ArrayBuilder<Node>.Build(IfData.Expression, IfData.Block, ElseBlock, ElseIfData.Select(ei => ei.Block).ToArray(), ElseIfData.Select(ei => ei.Expression).ToArray());
        }
    }

    public class IfData
    {
        public Node Expression { get; private set; }
        public BlockNode Block { get; private set; }

        public IfData(Node expression, BlockNode block)
        {
            Expression = expression;
            Block = block;
        }
    }

    public class ReturnNode : Node
    {
        public Node Value { get; private set; }

        public ReturnNode(Node value, Range range) : base (range)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class Pos : IComparable<Pos>
    {
        public int line { get; set; }
        public int character { get; set; }

        public Pos(int line, int character)
        {
            this.line = line;
            this.character = character;
        }

        public Pos() {}

        public override string ToString()
        {
            return line + ", " + character;
        }

        public Range ToRange()
        {
            return new Range(this, this);
        }

        public int CompareTo(Pos other)
        {
            if (other == null || this.line < other.line || (this.line == other.line && this.character < other.character))
                return -1;
            
            if (this.line == other.line && this.character == other.character)
                return 0;
            
            if (this.line > other.line || (this.line == other.line && this.character > other.character))
                return 1;

            throw new Exception();
        }

        #region Operators
        public static bool operator <(Pos p1, Pos p2)  => p1.CompareTo(p2) <  0;
        public static bool operator >(Pos p1, Pos p2)  => p1.CompareTo(p2) >  0;
        public static bool operator <=(Pos p1, Pos p2) => p1.CompareTo(p2) <= 0;
        public static bool operator >=(Pos p1, Pos p2) => p1.CompareTo(p2) >= 0;
        #endregion
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
            if (context.stop == null)
            {
                Pos pos = new Pos(context.start.Line, context.start.Column);
                return new Range(pos, pos);
            }

            if (context.start.Line == context.stop.Line &&
                context.start.Column == context.stop.Column)
            {
                return new Range
                (
                    new Pos(context.start.Line - 1, context.start.Column),
                    new Pos(context.stop.Line - 1, context.stop.Column + context.GetText().Length)
                );
            }
            else
            {
                return new Range
                (
                    new Pos(context.start.Line - 1, context.start.Column),
                    new Pos(context.stop.Line - 1, context.stop.Column + 1)
                );
            }
        }

        public static Range GetRange(IToken token)
        {
            return new Range(new Pos(token.Line - 1, token.Column), new Pos(token.Line - 1, token.Column + token.Text.Length));
        }

        public static Range GetRange(IToken start, IToken stop)
        {
            return new Range(new Pos(start.Line - 1, start.Column + 1), new Pos(stop.Line - 1, stop.Column));
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