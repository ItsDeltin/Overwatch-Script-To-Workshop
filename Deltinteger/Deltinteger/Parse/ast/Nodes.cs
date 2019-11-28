using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public abstract class Node : INode
    {
        public Location Location { get; }

        public DocRange[] SubRanges { get; set; }

        public ScopeGroup RelatedScopeGroup { get; set; }

        public Node(Location location, params DocRange[] subRanges)
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

    public class ImportNode : Node, IImportNode
    {
        public string File { get; }

        public ImportNode(DeltinScriptParser.Import_fileContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            File = context.STRINGLITERAL().GetText().Trim('"');
        }

        override public Node[] Children()
        {
            return null;
        }
    }

    public class ImportObjectNode : Node, IImportNode
    {
        public string Name { get; }
        public string File { get; }

        public ImportObjectNode(DeltinScriptParser.Import_objectContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Name = context.name.Text;
            File = context.file.Text.Trim('"');
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
        public UserMethodBase[] Methods { get; }

        public TypeDefineNode(DeltinScriptParser.Type_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            if (context.STRUCT() != null)
                TypeKind = TypeKind.Struct;
            else if (context.CLASS() != null)
                TypeKind = TypeKind.Class;
            else throw new Exception();

            Name = context.name.Text;

            DefinedVars = new InclassDefineNode[context.inclass_define().Length];
            for (int i = 0; i < DefinedVars.Length; i++)
                DefinedVars[i] = (InclassDefineNode)visitor.VisitInclass_define(context.inclass_define(i));
            
            Constructors = new ConstructorNode[context.constructor().Length];
            for (int i = 0; i < Constructors.Length; i++)
                Constructors[i] = (ConstructorNode)visitor.VisitConstructor(context.constructor(i));
            
            Methods = new UserMethodBase[context.user_method().Length + context.macro().Length];
            for (int i = 0; i < context.user_method().Length; i++)
                Methods[i] = (UserMethodBase)visitor.VisitUser_method(context.user_method(i));

            for (int i = 0; i < context.macro().Length; i++)
                Methods[context.user_method().Length + i] = (UserMethodBase)visitor.VisitMacro(context.macro(i));
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

        public ConstructorNode(DeltinScriptParser.ConstructorContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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
        public ImportObjectNode[] ObjectImports { get; }
        public RuleNode[] Rules { get; }
        public RuleDefineNode[] DefinedVars { get; }
        public UserMethodBase[] UserMethods { get; }
        public TypeDefineNode[] DefinedTypes { get; }
        public ReservedNode ReservedGlobal { get; }
        public ReservedNode ReservedPlayer { get; }
        public int[] ReservedGlobalIDs { get; }
        public int[] ReservedPlayerIDs { get; }

        public RulesetNode(DeltinScriptParser.RulesetContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            // Get imports
            Imports = new ImportNode[context.import_file().Length];
            for (int i = 0; i < Imports.Length; i++)
                Imports[i] = new ImportNode(context.import_file(i), visitor);

            ObjectImports = new ImportObjectNode[context.import_object().Length];
            for (int i = 0; i < ObjectImports.Length; i++)
                ObjectImports[i] = new ImportObjectNode(context.import_object(i), visitor);

            // Get rules
            Rules = new RuleNode[context.ow_rule().Length];
            for (int i = 0; i < Rules.Length; i++)
                Rules[i] = (RuleNode)visitor.VisitOw_rule(context.ow_rule()[i]);

            // Get defined variables
            DefinedVars = new RuleDefineNode[context.rule_define().Length];
            var reservedGlobalIDs = new List<int>();
            var reservedPlayerIDs = new List<int>();
            for (int i = 0; i < DefinedVars.Length; i++)
            {
                DefinedVars[i] = new RuleDefineNode(context.rule_define(i), visitor);
                if (DefinedVars[i].OverrideID != -1)
                    (DefinedVars[i].IsGlobal ? reservedGlobalIDs : reservedPlayerIDs).Add(DefinedVars[i].OverrideID);
            }
            ReservedGlobalIDs = reservedGlobalIDs.ToArray();
            ReservedPlayerIDs = reservedPlayerIDs.ToArray();

            // Get user methods
            UserMethods = new UserMethodBase[context.user_method().Length + context.macro().Length];
            for (int i = 0; i < context.user_method().Length; i++)
                UserMethods[i] = (UserMethodNode)visitor.VisitUser_method(context.user_method(i));
            
            // Get macros
            for (int i = 0; i < context.macro().Length; i++)
                UserMethods[i + context.user_method().Length] = new MacroNode(context.macro(i), visitor);
            
            // Get types
            DefinedTypes = new TypeDefineNode[context.type_define().Length];
            for (int i = 0; i < DefinedTypes.Length; i++)
                DefinedTypes[i] = (TypeDefineNode)visitor.VisitType_define(context.type_define(i));
            
            // Get reserved global variables
            if (context.reserved_global()?.reserved_list() != null)
                ReservedGlobal = new ReservedNode(context.reserved_global().reserved_list(), visitor);
            
            // Get reserved player variables
            if (context.reserved_player()?.reserved_list() != null)
                ReservedPlayer = new ReservedNode(context.reserved_player().reserved_list(), visitor);
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(ReservedGlobal, ReservedPlayer, Imports, ObjectImports, Rules, DefinedVars, UserMethods, DefinedTypes);
        }
    }

    public class ReservedNode : Node
    {
        public string[] ReservedNames { get; }
        public int[] ReservedIDs { get; }

        public ReservedNode(DeltinScriptParser.Reserved_listContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            ReservedNames = context.PART().Select(p => p.GetText()).ToArray();
            ReservedIDs = context.NUMBER().Select(n => int.Parse(n.GetText())).ToArray();
        }

        public override Node[] Children()
        {
            return null;
        }
    }
    
    public class DefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get; }
        public bool Extended { get; }

        public DefineNode(DeltinScriptParser.DefineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            Extended = context.NOT() != null;
            
            if (context.expr() != null)
                Value = visitor.VisitExpr(context.expr());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class RuleDefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get; }
        public bool Extended { get; }
        public bool IsGlobal { get; }
        public int OverrideID { get; } = -1;

        public RuleDefineNode(DeltinScriptParser.Rule_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            if (context.expr() != null)
                Value = visitor.Visit(context.expr());
            Extended = context.NOT() != null;
            IsGlobal = context.GLOBAL() != null;

            if (context.id != null)
                OverrideID = int.Parse(context.id.GetText());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class InclassDefineNode : Node, IDefine
    {
        public string VariableName { get; }
        public string Type { get; }
        public Node Value { get; }
        public bool Extended { get; } = false;
        public AccessLevel AccessLevel { get; }

        public InclassDefineNode(DeltinScriptParser.Inclass_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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
        public bool Extended { get; }

        public ParameterDefineNode(DeltinScriptParser.Parameter_defineContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            VariableName = context.name.Text;
            Type = context.type?.Text;
            Extended = Extended = context.NOT() != null;
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

                parameters[i].Extended = defineNodes[i].Extended;
            }
            return parameters;
        }
    }

    public class RuleNode : Node
    {
        public string Name { get; }
        public RuleEvent Event { get; }
        public Team Team { get; }
        public PlayerSelector Player { get; }
        public Node[] Conditions { get; }
        public BlockNode Block { get; }
        public bool Disabled { get; }

        public RuleNode(DeltinScriptParser.Ow_ruleContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Name = context.STRINGLITERAL().GetText().Trim('"');
            Block = (BlockNode)visitor.VisitBlock(context.block());
            Disabled = context.DISABLED() != null;

            Conditions = new Node[context.rule_if().Length];
            DocRange[] conditionRanges      = new DocRange          [context.rule_if().Length];

            for (int i = 0; i < context.rule_if().Length; i++)
            {
                if (context.rule_if(i).expr() != null)
                    Conditions[i] = visitor.VisitExpr(context.rule_if(i).expr());

                // Get the range between the ().
                conditionRanges[i] = DocRange.GetRange(
                    context.rule_if(i).LEFT_PAREN().Symbol, 
                    context.rule_if(i).RIGHT_PAREN().Symbol
                );
            }

            RuleEvent eventType = RuleEvent.OngoingGlobal;
            Team team = Team.All;
            PlayerSelector player = PlayerSelector.All;

            DocRange eventRange = null;
            DocRange teamRange = null;
            DocRange playerRange = null;
            foreach(var ruleOption in context.@enum())
            {
                string option = ruleOption.PART(0).GetText();
                DocRange optionRange = DocRange.GetRange(ruleOption.PART(0).Symbol);

                string value = ruleOption.PART(1)?.GetText();
                DocRange valueRange = null;
                if (value != null)
                    valueRange = DocRange.GetRange(ruleOption.PART(1).Symbol);

                DocRange totalRange;
                if (ruleOption.PART(1) != null) totalRange = DocRange.GetRange(ruleOption.PART(0).Symbol, ruleOption.PART(1).Symbol);
                else totalRange = DocRange.GetRange(ruleOption.PART(0));
                
                switch (option)
                {
                    case "Event":
                        if (eventRange != null)
                            visitor._diagnostics.Error("Event already set.", new Location(visitor.file, totalRange));
                        
                        if (!Enum.TryParse<RuleEvent>(value, out eventType))
                            visitor._diagnostics.Error($"{value} is not a valid Event type.", new Location(visitor.file, valueRange));
                        
                        eventRange = DocRange.GetRange(ruleOption);
                        break;
                    
                    case "Team":
                        if (teamRange != null)
                            visitor._diagnostics.Error("Team already set.", new Location(visitor.file, totalRange));

                        if (!Enum.TryParse<Team>(value, out team))
                            visitor._diagnostics.Error($"{value} is not a valid Team type.", new Location(visitor.file, valueRange));
                        
                        teamRange = DocRange.GetRange(ruleOption);
                        break;

                    case "Player":
                        if (playerRange != null)
                            visitor._diagnostics.Error("Player already set.", new Location(visitor.file, totalRange));

                        if (!Enum.TryParse<PlayerSelector>(value, out player))
                            visitor._diagnostics.Error($"{value} is not a valid Player type.", new Location(visitor.file, valueRange));
                        
                        playerRange = DocRange.GetRange(ruleOption);
                        break;
                    
                    default:
                        visitor._diagnostics.Error($"{option} is not a valid rule option.", new Location(visitor.file, optionRange));
                        break;
                }
            }
            Event = eventType;
            Team = team;
            Player = player;

            SubRanges = ArrayBuilder<DocRange>.Build(eventRange, teamRange, playerRange, conditionRanges);
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

    public abstract class UserMethodBase : Node
    {
        public string Name { get; protected set; }
        public string Documentation { get; protected set; }
        public ParameterDefineNode[] Parameters { get; protected set; }
        public AccessLevel AccessLevel { get; protected set; }

        public UserMethodBase(Location location) : base(location)
        {
        }

        protected string GetDocumentation(ITerminalNode[] nodes)
        {
            return string.Join("\n\r", nodes.Select(doc => doc.GetText().TrimEnd().TrimStart('#', ' ')));
        }

        override public Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Parameters, GetChildren());
        }

        abstract protected Node[] GetChildren();
    }

    public class UserMethodNode : UserMethodBase, IBlockContainer
    {
        public string Type { get; }
        public BlockNode Block { get; }
        public bool IsRecursive { get; }
        private Location errorRange { get; }
        
        public UserMethodNode(DeltinScriptParser.User_methodContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            errorRange = new Location(visitor.file, DocRange.GetRange(context.name));
            Name = context.name.Text;
            Type = context.type?.Text;

            Parameters = new ParameterDefineNode[context.setParameters().parameter_define().Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = new ParameterDefineNode(context.setParameters().parameter_define(i), visitor);

            Block = (BlockNode)visitor.VisitBlock(context.block());
            IsRecursive = context.RECURSIVE() != null;
            Documentation = GetDocumentation(context.DOCUMENTATION());

            AccessLevel = AccessLevel.Private;
            if (context.accessor() != null)
                AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), context.accessor().GetText(), true);
        }

        override protected Node[] GetChildren()
        {
            return ArrayBuilder<Node>.Build(Block);
        }
    
        public PathInfo[] Paths()
        {
            return new PathInfo[] {new PathInfo(Block, errorRange, false)};
        }
    }

    public class MacroNode : UserMethodBase
    {
        public Node Expression { get; }

        public MacroNode(DeltinScriptParser.MacroContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Name = context.name.Text;
            Documentation = GetDocumentation(context.DOCUMENTATION());

            Parameters = new ParameterDefineNode[context.setParameters().parameter_define().Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = new ParameterDefineNode(context.setParameters().parameter_define(i), visitor);
            
            AccessLevel = AccessLevel.Private;
            if (context.accessor() != null)
                AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), context.accessor().GetText(), true);
            
            Expression = visitor.VisitExpr(context.expr());
        }

        override protected Node[] GetChildren()
        {
            return ArrayBuilder<Node>.Build(Expression);
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
        public Node[] Statements { get; }

        public BlockNode(Node[] statements, Location location) : base(location) 
        {
            Statements = statements;
        }

        public override Node[] Children()
        {
            return Statements;
        }
    }

    public class MethodNode : Node, ICallableNode
    {
        public string Name { get; }
        public Node[] Parameters { get; }
        public PickyParameter[] PickyParameters { get; }

        public IMethod Method { get; set; }

        public MethodNode(DeltinScriptParser.MethodContext context, BuildAstVisitor visitor)
            : base(
                new Location(visitor.file, DocRange.GetRange(context)),
                DocRange.GetRange(context.PART().Symbol),
                DocRange.GetRange(context.LEFT_PAREN().Symbol, context.RIGHT_PAREN().Symbol)
            )
        {
            Name = context.PART().GetText();

            if (context.call_parameters() != null)
            {
                Parameters = new Node[context.call_parameters().expr().Length];
                for (int i = 0; i < Parameters.Length; i++)
                    Parameters[i] = visitor.Visit(context.call_parameters().expr()[i]);
            }
            else if (context.picky_parameters() != null)
            {
                PickyParameters = new PickyParameter[context.picky_parameters().picky_parameter().Length];
                for (int i = 0; i < PickyParameters.Length; i++)
                    PickyParameters[i] = new PickyParameter(context.picky_parameters().picky_parameter(i), visitor);
            }
            else
                Parameters = new Node[0];
        }

        public bool IsNameSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(0);
        }

        public bool IsParametersSelected(Pos caretPos)
        {
            return SubrangesSelected(caretPos).Contains(1);
        }

        public bool UsingNormalParameters()
        {
            return Parameters != null;
        }

        public Node[] GetParameters()
        {
            return UsingNormalParameters() ? Parameters : PickyParameters;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Parameters, PickyParameters);
        }
    }

    public class PickyParameter : Node
    {
        public string Name { get; }
        public Node Expression { get; }

        public PickyParameter(DeltinScriptParser.Picky_parameterContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Name = context.PART()?.GetText();
            if (context.expr() != null)
                Expression = visitor.Visit(context.expr());
        }

        public override Node[] Children()
        {
            return new Node[] { Expression };
        }
    }

    public class VariableNode : Node
    {
        public string Name { get; }
        public Node[] Index { get; }

        public VariableNode(DeltinScriptParser.VariableContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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
        public ExpressionTreeNode(DeltinScriptParser.ExprContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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

    public class NumberNode : Node, IConstantSupport
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

        public object GetValue()
        {
            return Value;
        }
    }

    public class StringNode : Node, IConstantSupport
    {
        public string Value { get; }
        public Node[] Format { get; }
        public bool Localized { get; }

        public StringNode(string value, Node[] format, bool localized, Location location) : base(location)
        {
            Value = value;
            Format = format;
            Localized = localized;
        }

        public override Node[] Children()
        {
            return Format;
        }

        public object GetValue()
        {
            return Value;
        }
    }

    public class BooleanNode : Node, IConstantSupport
    {
        public bool Value { get; }

        public BooleanNode(bool value, Location location) : base(location)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return null;
        }

        public object GetValue()
        {
            return Value;
        }
    }

    public class NotNode : Node
    {
        public Node Value { get; }

        public NotNode(Node value, Location location) : base(location)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class InvertNode : Node
    {
        public Node Value { get; }

        public InvertNode(Node value, Location location) : base(location)
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
        public string Type { get; }
        public string Value { get; }
        public EnumMember EnumMember { get; }

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
        public Node Value { get; }
        public Node Index { get; }

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
        public Node[] Values { get; }
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
        public Node Condition { get; }
        public Node Consequent { get; }
        public Node Alternative { get; }
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

    public class CreateObjectNode : Node, ICallableNode
    {
        public string TypeName { get; }
        public Node[] Parameters { get; }

        public CreateObjectNode(DeltinScriptParser.Create_objectContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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

    public class RootNode : Node
    {
        public RootNode(Location location) : base (location)
        {
        }

        public override Node[] Children()
        {
            return null;
        }
    }

    public class VarSetNode : Node
    {
        public Node Variable { get; }
        public Node[] Index { get; }
        public string Operation { get; }
        public Node Value { get; }

        public VarSetNode(DeltinScriptParser.VarsetContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
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

    public class ForEachNode : Node, IBlockContainer
    {
        public ParameterDefineNode Variable { get; }
        public Node Array { get; }
        public BlockNode Block { get; }
        public int Repeaters { get; }
        private Location errorRange { get; }

        public ForEachNode(DeltinScriptParser.ForeachContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            errorRange = new Location(visitor.file, DocRange.GetRange(context.FOREACH()));
            Array = visitor.Visit(context.expr());
            Variable = new ParameterDefineNode(context.parameter_define(), visitor);
            Block = (BlockNode)visitor.VisitBlock(context.block());

            Repeaters = 1;
            if (context.number() != null)
                Repeaters = int.Parse(context.number().GetText());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Variable, Array, Block);
        }
    
        public PathInfo[] Paths()
        {
            return new PathInfo[] {new PathInfo(Block, errorRange, false)};
        }
    }

    public class ForNode : Node, IBlockContainer
    {
        public VarSetNode VarSetNode { get; }
        public DefineNode DefineNode { get; }
        public Node Expression { get; }
        public VarSetNode Statement { get; }
        public BlockNode Block { get; }
        private Location errorRange { get; }

        public ForNode(DeltinScriptParser.ForContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            errorRange = new Location(visitor.file, DocRange.GetRange(context.FOR()));
            Block = (BlockNode)visitor.VisitBlock(context.block());

            if (context.varset() != null)
                VarSetNode = (VarSetNode)visitor.VisitVarset(context.varset());

            if (context.define() != null)
                DefineNode = (DefineNode)visitor.VisitDefine(context.define());

            if (context.expr() != null)
                Expression = visitor.VisitExpr(context.expr());

            if (context.forEndStatement() != null)
                Statement = (VarSetNode)visitor.VisitVarset(context.forEndStatement().varset());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(VarSetNode, DefineNode, Expression, Statement, Block);
        }
    
        public PathInfo[] Paths()
        {
            return new PathInfo[] {new PathInfo(Block, errorRange, false)};
        }
    }

    public class WhileNode : Node, IBlockContainer
    {
        public Node Expression { get; }
        public BlockNode Block { get; }
        private Location errorRange { get; }

        public WhileNode(DeltinScriptParser.WhileContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Expression = visitor.VisitExpr(context.expr());
            Block = (BlockNode)visitor.VisitBlock(context.block());
            errorRange = new Location(visitor.file, DocRange.GetRange(context.WHILE()));
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Expression, Block);
        }
    
        public PathInfo[] Paths()
        {
            return new PathInfo[] {new PathInfo(Block, errorRange, false)};
        }
    }

    public class IfNode : Node, IBlockContainer
    {
        public IfData IfData { get; }
        public IfData[] ElseIfData { get; }
        public BlockNode ElseBlock { get; }

        private List<PathInfo> paths { get; } = new List<PathInfo>();

        public IfNode(DeltinScriptParser.IfContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            // Get the if data
            IfData = new IfData
            (
                visitor.VisitExpr(context.expr()),
                (BlockNode)visitor.VisitBlock(context.block())
            );
            paths.Add(new PathInfo(IfData.Block, new Location(visitor.file, DocRange.GetRange(context.IF())), false));

            // Get the else-if data
            ElseIfData = null;
            if (context.else_if() != null)
            {
                ElseIfData = new IfData[context.else_if().Length];
                for (int i = 0; i < context.else_if().Length; i++)
                {
                    ElseIfData[i] = new IfData
                    (
                        visitor.VisitExpr(context.else_if(i).expr()),
                        (BlockNode)visitor.VisitBlock(context.else_if(i).block())
                    );
                    paths.Add(
                        new PathInfo(
                            ElseIfData[i].Block,
                            new Location(
                                visitor.file, 
                                DocRange.GetRange(
                                    context.else_if(i).ELSE(),
                                    context.else_if(i).IF()
                                )
                            ),
                            false
                        )
                    );
                }
            }
            
            // Get the else block
            ElseBlock = null;
            if (context.@else() != null)
            {
                ElseBlock = (BlockNode)visitor.VisitBlock(context.@else().block());
                paths.Add(new PathInfo(ElseBlock, new Location(visitor.file, DocRange.GetRange(context.@else().ELSE())), true));
            }
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

        public PathInfo[] Paths()
        {
            return paths.ToArray();
        }
    }

    public class IfData
    {
        public Node Expression { get; }
        public BlockNode Block { get; }

        public IfData(Node expression, BlockNode block)
        {
            Expression = expression;
            Block = block;
        }
    }

    public class ReturnNode : Node
    {
        public Node Value { get; }

        public ReturnNode(Node value, Location location) : base (location)
        {
            Value = value;
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Value);
        }
    }

    public class DeleteNode : Node
    {
        public Node Delete { get; } 

        public DeleteNode(DeltinScriptParser.DeleteContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Delete = visitor.VisitExpr(context.expr());
        }

        public override Node[] Children()
        {
            return ArrayBuilder<Node>.Build(Delete);
        }
    }

    public class TypeConvertNode : Node
    {
        public string Type { get; }
        public Node Expression { get; }

        public TypeConvertNode(DeltinScriptParser.TypeconvertContext context, BuildAstVisitor visitor) : base(new Location(visitor.file, DocRange.GetRange(context)))
        {
            Type = context.PART().GetText();
            Expression = visitor.VisitExpr(context.expr());
        }

        override public Node[] Children()
        {
            return new Node[] { Expression };
        }
    }
}