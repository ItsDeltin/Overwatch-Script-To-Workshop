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
        public Diagnostics _diagnostics { get; }
        public string file { get; }

        public BuildAstVisitor(string file, Diagnostics diagnostics)
        {
            this.file = file;
            _diagnostics = diagnostics;
        }

        public override Node VisitRuleset(DeltinScriptParser.RulesetContext context)
        {
            return new RulesetNode(context, this);
        }

        public override Node VisitImport_file(DeltinScriptParser.Import_fileContext context)
        {
            return new ImportNode(context, this);
        }

        public override Node VisitRule_define(DeltinScriptParser.Rule_defineContext context)
        {
            return new RuleDefineNode(context, this);
        }

        public override Node VisitDefine(DeltinScriptParser.DefineContext context)
        {
            return new DefineNode(context, this);
        }

        public override Node VisitInclass_define(DeltinScriptParser.Inclass_defineContext context)
        {
            return new InclassDefineNode(context, this);
        }

        public override Node VisitUseVar(DeltinScriptParser.UseVarContext context)
        {
            return new UseVarNode(context, this);
        }

        public override Node VisitUser_method(DeltinScriptParser.User_methodContext context)
        {
            return new UserMethodNode(context, this);
        }

        public override Node VisitOw_rule(DeltinScriptParser.Ow_ruleContext context)
        {
            return new RuleNode(context, this);
        }

        public override Node VisitBlock(DeltinScriptParser.BlockContext context)
        {
            Node[] statements = new Node[context.statement().Length];
            for (int i = 0; i < statements.Length; i++)
                statements[i] = VisitStatement(context.statement()[i]);
            
            return new BlockNode(statements, new Location(file, Range.GetRange(context)));
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

                node = new OperationNode(left, operation, right, new Location(file, Range.GetRange(context)));
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

                node = new ValueInArrayNode(value, index, new Location(file, Range.GetRange(context)));
            }

            // Seperator
            else if (context.SEPERATOR() != null && context.expr().Length >= 2)
            {
                node = new ExpressionTreeNode(context, this);
            }
            
            // Not
            else if (context.ChildCount == 2
            && context.GetChild(0).GetText() == "!"
            && context.GetChild(1) is DeltinScriptParser.ExprContext)
            {
                Node value = Visit(context.GetChild(1));
                node = new NotNode(value, new Location(file, Range.GetRange(context)));
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
                node = new TernaryConditionalNode(condition, consequent, alternative, new Location(file, Range.GetRange(context)));
            }

            // This
            else if (context.THIS() != null)
            {
                node = new ThisNode(new Location(file, Range.GetRange(context)));
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
            return new NumberNode(value, new Location(file, Range.GetRange(context)));
        }

        // "Hello <0>! Waiting game..."
        public override Node VisitString(DeltinScriptParser.StringContext context)
        {
            string value = context.STRINGLITERAL().GetText().Trim('"');
            return new StringNode(value, null, new Location(file, Range.GetRange(context)));
        }

        // <"hello <0>! Waiting game...", EventPlayer()>
        public override Node VisitFormatted_string(DeltinScriptParser.Formatted_stringContext context)
        {
            string value = context.@string().GetText().Trim('"');
            Node[] format = new Node[context.expr().Length];
            for (int i = 0; i < format.Length; i++)
                format[i] = VisitExpr(context.expr()[i]);
            return new StringNode(value, format, new Location(file, Range.GetRange(context)));
        }

        // Method()
        public override Node VisitMethod(DeltinScriptParser.MethodContext context)
        {
            string methodName = context.PART().GetText();

            Node[] parameters = new Node[context.call_parameters()?.expr().Length ?? 0];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Visit(context.call_parameters().expr()[i]);

            Range nameRange = Range.GetRange(context.PART().Symbol);
            Range parameterRange = Range.GetRange(context.LEFT_PAREN().Symbol, context.RIGHT_PAREN().Symbol);

            return new MethodNode(methodName, parameters, nameRange, parameterRange, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitVariable(DeltinScriptParser.VariableContext context)
        {
            return new VariableNode(context, this);
        }

        // ( expr )
        public override Node VisitExprgroup(DeltinScriptParser.ExprgroupContext context)
        {
            return Visit(context.expr());
        }

        public override Node VisitTrue(DeltinScriptParser.TrueContext context)
        {
            return new BooleanNode(true, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitFalse(DeltinScriptParser.FalseContext context)
        {
            return new BooleanNode(false, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitNull(DeltinScriptParser.NullContext context)
        {
            return new NullNode(new Location(file, Range.GetRange(context)));
        }

        public override Node VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string[] split = context.GetText().Split('.');
            string type = split[0];
            string value = split[1];
            return new EnumNode(type, value, new Location(file, Range.GetRange(context)));
        }
        
        public override Node VisitCreatearray(DeltinScriptParser.CreatearrayContext context)
        {
            Node[] values = new Node[context.expr().Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = VisitExpr(context.expr()[i]);

            return new CreateArrayNode(values, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitCreate_object(DeltinScriptParser.Create_objectContext context)
        {
            return new CreateObjectNode(context, this);
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
            return new VarSetNode(context, this);
        }

        public override Node VisitFor(DeltinScriptParser.ForContext context)
        {
            BlockNode block = (BlockNode)VisitBlock(context.block());

            VarSetNode varSet = null;
            if (context.varset() != null)
                varSet = (VarSetNode)VisitVarset(context.varset());

            DefineNode defineNode = null;
            if (context.define() != null)
                defineNode = (DefineNode)VisitDefine(context.define());

            Node expression = null;
            if (context.expr() != null)
                expression = VisitExpr(context.expr());

            VarSetNode statement = null;
            if (context.forEndStatement() != null)
                statement = (VarSetNode)VisitVarset(context.forEndStatement().varset());
            
            return new ForNode(varSet, defineNode, expression, statement, block, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitForeach(DeltinScriptParser.ForeachContext context)
        {
            Node array = Visit(context.expr());

            string name = context.PART().GetText();

            BlockNode block = (BlockNode)VisitBlock(context.block());

            int repeaters = 1;
            if (context.number() != null)
                repeaters = int.Parse(context.number().GetText());
            
            return new ForEachNode(name, array, block, repeaters, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitWhile(DeltinScriptParser.WhileContext context)
        {
            BlockNode block = (BlockNode)VisitBlock(context.block());
            Node expression = VisitExpr(context.expr());

            return new WhileNode(expression, block, new Location(file, Range.GetRange(context)));
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

            return new IfNode(ifData, elseIfData, elseBlock, new Location(file, Range.GetRange(context)));
        }

        public override Node VisitReturn(DeltinScriptParser.ReturnContext context)
        {
            Node returnValue = null;
            if (context.expr() != null)
                returnValue = VisitExpr(context.expr());

            return new ReturnNode(returnValue, new Location(file, Range.GetRange(context)));
        }
        #endregion

        public override Node VisitType_define(DeltinScriptParser.Type_defineContext context)
        {
            return new TypeDefineNode(context, this);
        }

        public override Node VisitConstructor(DeltinScriptParser.ConstructorContext context)
        {
            return new ConstructorNode(context, this);
        }
    }

    public abstract class Node
    {
        public Location Location { get; private set; }

        public Range[] SubRanges { get; set; }

        public Element RelatedElement { get; set; }

        public ScopeGroup RelatedScopeGroup { get; set; }

        public Node(Location location, params Range[] subRanges)
        {
            Location = location;
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
            if (Location.range.IsInside(caretPos))
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

    public class ImportNode : Node
    {
        public string File { get; }

        public ImportNode(DeltinScriptParser.Import_fileContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            File = context.STRINGLITERAL().GetText().Trim('"');
        }

        override public Node[] Children()
        {
            return null;
        }
    }
    
    public class TypeDefineNode : Node
    {
        public TypeKind TypeKind { get; }
        public string Name { get; }
        public InclassDefineNode[] DefinedVars { get; }
        public ConstructorNode[] Constructors { get; }
        public UserMethodNode[] Methods { get; }

        public TypeDefineNode(DeltinScriptParser.Type_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            /*
            if (context.CLASS() != null)
            {
                visitor._diagnostics.Error("Classes are not yet supported, use struct instead.", Range.GetRange(context.CLASS()));
                TypeKind = TypeKind.Class;
            }
            */

            if (context.STRUCT() != null)
                TypeKind = TypeKind.Struct;
            
            else throw new Exception();

            Name = context.name.Text;

            DefinedVars = new InclassDefineNode[context.inclass_define().Length];
            for (int i = 0; i < DefinedVars.Length; i++)
                DefinedVars[i] = (InclassDefineNode)visitor.VisitInclass_define(context.inclass_define(i));
            
            Constructors = new ConstructorNode[context.constructor().Length];
            for (int i = 0; i < Constructors.Length; i++)
                Constructors[i] = (ConstructorNode)visitor.VisitConstructor(context.constructor(i));
            
            Methods = new UserMethodNode[context.user_method().Length];
            for (int i = 0; i < Methods.Length; i++)
                Methods[i] = (UserMethodNode)visitor.VisitUser_method(context.user_method(i));
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(DefinedVars, Constructors, Methods);
        }
    }

    public class ConstructorNode : Node
    {
        public AccessLevel AccessLevel { get; }
        public ParameterDefineNode[] Parameters { get; }
        public BlockNode BlockNode { get; }
        public string Name { get; }

        public ConstructorNode(DeltinScriptParser.ConstructorContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Name = context.PART().GetText();

            Parameters = new ParameterDefineNode[context.setParameters().parameter_define().Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = new ParameterDefineNode(context.setParameters().parameter_define(i), visitor);

            AccessLevel = AccessLevel.Private;
            if (context.accessor() != null)
                AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), context.accessor().GetText(), true);
            
            BlockNode = (BlockNode)visitor.VisitBlock(context.block());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(BlockNode);
        }
    }

    public class RulesetNode : Node
    {
        public ImportNode[] Imports { get; }
        public Variable UseGlobalVar { get; }
        public Variable UsePlayerVar { get; }
        public RuleNode[] Rules { get; }
        public RuleDefineNode[] DefinedVars { get; }
        public UserMethodNode[] UserMethods { get; }
        public TypeDefineNode[] DefinedTypes { get; }

        public RulesetNode(DeltinScriptParser.RulesetContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Imports = new ImportNode[context.import_file().Length];
            for (int i = 0; i < Imports.Length; i++)
                Imports[i] = new ImportNode(context.import_file(i), visitor);

            Rules = new RuleNode[context.ow_rule().Length];
            for (int i = 0; i < Rules.Length; i++)
                Rules[i] = (RuleNode)visitor.VisitOw_rule(context.ow_rule()[i]);

            Variable useGlobalVar;
            Variable usePlayerVar;
            Enum.TryParse<Variable>(context.useGlobalVar()?.PART().GetText(), out useGlobalVar);
            Enum.TryParse<Variable>(context.usePlayerVar()?.PART().GetText(), out usePlayerVar);
            UseGlobalVar = useGlobalVar;
            UsePlayerVar = usePlayerVar;

            DefinedVars = new RuleDefineNode[context.rule_define().Length];
            for (int i = 0; i < DefinedVars.Length; i++)
                DefinedVars[i] = (RuleDefineNode)visitor.VisitRule_define(context.rule_define(i));

            UserMethods = new UserMethodNode[context.user_method().Length];
            for (int i = 0; i < UserMethods.Length; i++)
                UserMethods[i] = (UserMethodNode)visitor.VisitUser_method(context.user_method(i));
            
            DefinedTypes = new TypeDefineNode[context.type_define().Length];
            for (int i = 0; i < DefinedTypes.Length; i++)
                DefinedTypes[i] = (TypeDefineNode)visitor.VisitType_define(context.type_define(i));
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Imports, Rules, DefinedVars, UserMethods, DefinedTypes);
        }
    }

    public interface IDefine
    {
        string VariableName { get; }
        string Type { get; }
        Node Value { get; }
    }

    public class DefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public UseVarNode UseVar { get; }
        public Node Value { get; }

        public DefineNode(DeltinScriptParser.DefineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            
            if (context.expr() != null)
                Value = visitor.VisitExpr(context.expr());

            if (context.useVar() != null)
                UseVar = (UseVarNode)visitor.VisitUseVar(context.useVar());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(UseVar, Value);
        }
    }

    public class RuleDefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get; }
        public UseVarNode UseVar { get; }
        public bool IsGlobal { get; }

        public RuleDefineNode(DeltinScriptParser.Rule_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            if (context.expr() != null)
                Value = visitor.Visit(context.expr());
            if (context.useVar() != null)
                UseVar = (UseVarNode)visitor.Visit(context.useVar());
            IsGlobal = context.GLOBAL() != null;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value, UseVar);
        }
    }

    public class InclassDefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get; }
        public AccessLevel AccessLevel { get; }

        public InclassDefineNode(DeltinScriptParser.Inclass_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            if (context.expr() != null)
                Value = visitor.Visit(context.expr());
            if (context.accessor() != null)
                AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), context.accessor().GetText(), true);
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class ParameterDefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get { throw new NotImplementedException(); } }

        public ParameterDefineNode(DeltinScriptParser.Parameter_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
        }

        public override Node[] Children()
        {
            return null;
        }

        public static ParameterBase[] GetParameters(ParsingData parser, ParameterDefineNode[] defineNodes)
        {
            ParameterBase[] parameters = new ParameterBase[defineNodes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                EnumData enumData = null;
                DefinedType type = null;
                if (defineNodes[i].Type != null)
                {
                    enumData = EnumData.GetEnum(defineNodes[i].Type);
                    type = parser.GetDefinedType(defineNodes[i].Type, null);

                    if (enumData == null && type == null)
                        throw SyntaxErrorException.NonexistentType(defineNodes[i].Type, defineNodes[i].Location);
                }

                if (enumData != null)
                    parameters[i] = new EnumParameter(defineNodes[i].VariableName, enumData.Type);
                
                else if (type != null)
                    parameters[i] = new TypeParameter(defineNodes[i].VariableName, type);

                else parameters[i] = new Parameter(defineNodes[i].VariableName, Elements.ValueType.Any, null);
            }
            return parameters;
        }
    }

    public class UseVarNode : Node
    {
        public Variable Variable { get; }
        public int[] Index { get; }

        public UseVarNode(DeltinScriptParser.UseVarContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            if (!Enum.TryParse<Variable>(context.PART().GetText(), out Variable variable))
            {
                visitor._diagnostics.Error("Expected letter.", new Location(visitor.file, Range.GetRange(context)));
                variable = Variable.A;
            }
            Variable = variable;
            
            int index = -1;
            if (context.number() != null)
                if (!int.TryParse(context.number().GetText(), out index))
                    index = -1;
            if (index != -1)
                Index = new int[] {index};
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

        public RuleNode(DeltinScriptParser.Ow_ruleContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Name = context.STRINGLITERAL().GetText().Trim('"');
            Block = (BlockNode)visitor.VisitBlock(context.block());

            Conditions = new Node[context.rule_if().Length];
            Range[] conditionRanges      = new Range          [context.rule_if().Length];

            for (int i = 0; i < context.rule_if().Length; i++)
            {
                if (context.rule_if(i).expr() != null)
                    Conditions[i] = visitor.VisitExpr(context.rule_if(i).expr());

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

                Range totalRange = Range.GetRange(ruleOption.PART(0).Symbol, ruleOption.PART(1).Symbol);
                
                switch (option)
                {
                    case "Event":
                        if (eventRange != null)
                            visitor._diagnostics.Error("Event already set.", new Location(visitor.file, totalRange));
                        
                        if (!Enum.TryParse<RuleEvent>(value, out eventType))
                            visitor._diagnostics.Error($"{value} is not a valid Event type.", new Location(visitor.file, valueRange));
                        
                        eventRange = Range.GetRange(ruleOption);
                        break;
                    
                    case "Team":
                        if (teamRange != null)
                            visitor._diagnostics.Error("Team already set.", new Location(visitor.file, totalRange));

                        if (!Enum.TryParse<Team>(value, out team))
                            visitor._diagnostics.Error($"{value} is not a valid Team type.", new Location(visitor.file, valueRange));
                        
                        teamRange = Range.GetRange(ruleOption);
                        break;

                    case "Player":
                        if (eventRange != null)
                            visitor._diagnostics.Error("Player already set.", new Location(visitor.file, totalRange));

                        if (!Enum.TryParse<PlayerSelector>(value, out player))
                            visitor._diagnostics.Error($"{value} is not a valid Player type.", new Location(visitor.file, valueRange));
                        
                        playerRange = Range.GetRange(ruleOption);
                        break;
                    
                    default:
                        visitor._diagnostics.Error($"{option} is not a valid rule option.", new Location(visitor.file, optionRange));
                        break;
                }
            }
            Event = eventType;
            Team = team;
            Player = player;

            SubRanges = ArrayBuilder<Range>.Build(eventRange, teamRange, playerRange, conditionRanges);
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
        public ParameterDefineNode[] Parameters { get; }
        public BlockNode Block { get; }
        public bool IsRecursive { get; }
        public string Documentation { get; }
        public AccessLevel AccessLevel { get; }
        
        public UserMethodNode(DeltinScriptParser.User_methodContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Name = context.PART().GetText();

            Parameters = new ParameterDefineNode[context.setParameters().parameter_define().Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = new ParameterDefineNode(context.setParameters().parameter_define(i), visitor);

            Block = (BlockNode)visitor.VisitBlock(context.block());
            IsRecursive = context.RECURSIVE() != null;
            Documentation = string.Join("\n\r", context.DOCUMENTATION().Select(doc => doc.GetText().TrimEnd().TrimStart('#', ' ')));

            AccessLevel = AccessLevel.Private;
            if (context.accessor() != null)
                AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), context.accessor().GetText(), true);

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

        public OperationNode(Node left, string operation, Node right, Location location) : base(location)
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

        public BlockNode(Node[] statements, Location location) : base(location) 
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

        public MethodNode(string name, Node[] parameters, Range nameRange, Range parameterRange, Location location) : base(location, nameRange, parameterRange)
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
        public Node[] Index { get; private set; }

        public VariableNode(DeltinScriptParser.VariableContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Name = context.PART().GetText();

            Index = new Node[context.array()?.expr().Length ?? 0];
            for (int i = 0; i < Index.Length; i++)
                Index[i] = visitor.VisitExpr(context.array().expr(i));
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Index);
        }
    }

    public class ExpressionTreeNode : Node
    {
        public Node[] Tree { get; }
        public ExpressionTreeNode(DeltinScriptParser.ExprContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Tree = new Node[context.expr().Length];
            for (int i = 0; i < Tree.Length; i++)
                Tree[i] = visitor.VisitExpr(context.expr(i));
        }

        public override Node[] Children()
        {
            return Tree;
        }
    }

    public class NumberNode : Node
    {
        public double Value;

        public NumberNode(double value, Location location) : base(location)
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

        public StringNode(string value, Node[] format, Location location) : base(location)
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

        public BooleanNode(bool value, Location location) : base(location)
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

        public NotNode(Node value, Location location) : base(location)
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
        public NullNode(Location location) : base(location) {}

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

        public EnumNode(string type, string value, Location location) : base(location)
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

        public ValueInArrayNode(Node value, Node index, Location location) : base(location)
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
        public CreateArrayNode(Node[] values, Location location) : base(location)
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
        public TernaryConditionalNode(Node condition, Node consequent, Node alternative, Location location) : base(location)
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

    public class CreateObjectNode : Node
    {
        public string TypeName { get; }
        public Node[] Parameters { get; }

        public CreateObjectNode(DeltinScriptParser.Create_objectContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            TypeName = context.type.Text;
            
            Parameters = new Node[context.call_parameters()?.expr().Length ?? 0];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = visitor.VisitExpr(context.call_parameters().expr()[i]);
        }

        public override Node[] Children()
        {
            return Parameters;
        }
    }

    public class ThisNode : Node
    {
        public ThisNode(Location location) : base(location)
        {
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class VarSetNode : Node
    {
        public Node Variable { get; private set; }
        public Node[] Index { get; private set; }
        public string Operation { get; private set; }
        public Node Value { get; private set; }

        public VarSetNode(DeltinScriptParser.VarsetContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, Range.GetRange(context)))
        {
            Variable = visitor.VisitExpr(context.var);
            
            Node[] index = new Node[context.array()?.expr().Length ?? 0];
            for (int i = 0; i < index.Length; i++)
                index[i] = visitor.VisitExpr(context.array().expr(i));

            if (context.val != null)
                Value = visitor.VisitExpr(context.val);

            Operation = context.statement_operation()?.GetText();
            if (Operation == null)
            {
                if (context.INCREMENT() != null)
                    Operation = "++";
                else if (context.DECREMENT() != null)
                    Operation = "--";
            }
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Variable, Index, Value);
        }
    }

    public class ForEachNode : Node
    {
        public string VariableName { get; }
        public Node Array { get; }
        public BlockNode Block { get; }
        public int Repeaters { get; }

        public ForEachNode(string variableName, Node array, BlockNode block, int repeaters, Location location) : base(location)
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
        public DefineNode DefineNode { get; private set; }
        public Node Expression { get; private set; }
        public VarSetNode Statement { get; private set; }
        public BlockNode Block { get; private set; }

        public ForNode(VarSetNode varSetNode, DefineNode defineNode, Node expression, VarSetNode statement, BlockNode block, Location location) : base(location)
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

        public WhileNode(Node expression, BlockNode block, Location location) : base(location)
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

        public IfNode(IfData ifData, IfData[] elseIfData, BlockNode elseBlock, Location location) : base(location)
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

        public ReturnNode(Node value, Location location) : base (location)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }
}