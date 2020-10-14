using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse;

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    public class RootContext
    {
        public List<Import> Imports { get; } = new List<Import>();
        public List<RuleContext> Rules { get; } = new List<RuleContext>();
        public List<ClassContext> Classes { get; } = new List<ClassContext>();
        public List<EnumContext> Enums { get; } = new List<EnumContext>();
        public List<IDeclaration> Declarations { get; } = new List<IDeclaration>();
        public List<Hook> Hooks { get; } = new List<Hook>();
        public List<TokenCapture> NodeCaptures { get; set; }
    }

    public class Node : INodeRange
    {
        public DocRange Range { get; set; }
    }

    // Interfaces
    public interface INodeRange
    {
        DocRange Range { get; set; }
    }
    public interface IParseExpression : INodeRange {}
    public interface IParseStatement : INodeRange {}
    public interface ICommentableStatement
    {
        Token ActionComment { get; }        
    }
    public interface IDeclaration
    {
        AttributeTokens Attributes { get; }
        IParseType Type { get; }
        Token Identifier { get; }
    }
    public interface IListComma
    {
        Token NextComma { get; set; }
    }
    public interface IParseType : INodeRange
    {
        Token GenericToken { get; }
        bool LookaheadValid { get; }
        bool IsVoid { get; }
        bool DefinitelyType { get; }
    }

    public class ParseType : Node, IParseType
    {
        public Token Identifier { get; }
        public List<IParseType> TypeArgs { get; }
        public int ArrayCount { get; }
        public bool IsVoid { get; }

        public ParseType(Token identifier, List<IParseType> typeArgs, int arrayCount)
        {
            Identifier = identifier;
            TypeArgs = typeArgs;
            ArrayCount = arrayCount;
            IsVoid = false;
        }

        public ParseType(Token @void)
        {
            Identifier = @void;
            IsVoid = true;
        }

        public bool HasTypeArgs => TypeArgs != null && TypeArgs.Count > 0;
        public bool IsArray => ArrayCount > 0;
        public bool LookaheadValid => Identifier != null;
        public bool IsDefault => !Identifier || Identifier.TokenType == TokenType.Define;
        public bool DefinitelyType => IsVoid || Identifier.TokenType == TokenType.Define || TypeArgs.Count > 0;
        Token IParseType.GenericToken => Identifier;
    }

    public class LambdaType : Node, IParseType
    {
        public Token ArrowToken { get; }
        public List<IParseType> Parameters { get; }
        public IParseType ReturnType { get; }

        public LambdaType(IParseType singleParameter, IParseType returnType, Token arrowToken)
        {
            ArrowToken = arrowToken;
            Parameters = new List<IParseType> { singleParameter };
            ReturnType = returnType;
        }

        public LambdaType(List<IParseType> parameters, IParseType returnType, Token arrowToken)
        {
            ArrowToken = arrowToken;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public bool LookaheadValid => ArrowToken && ReturnType.LookaheadValid;
        public bool IsVoid => false;
        public bool DefinitelyType => true;
        Token IParseType.GenericToken => throw new NotImplementedException();
    }

    public class GroupType : Node, IParseType
    {
        public IParseType Type { get; }
        public int ArrayCount { get; }

        public GroupType(IParseType type, int arrayCount)
        {
            Type = type;
            ArrayCount = arrayCount;
        }

        public bool LookaheadValid => Type.LookaheadValid;
        public bool IsVoid => Type.IsVoid;
        public bool DefinitelyType => Type.DefinitelyType;
        Token IParseType.GenericToken => Type.GenericToken;
    }

    public class PipeTypeContext : Node, IParseType
    {
        public IParseType Left { get; }
        public IParseType Right { get; }

        public PipeTypeContext(IParseType left, IParseType right)
        {
            Left = left;
            Right = right;
        }

        public Token GenericToken => throw new NotImplementedException();
        public bool LookaheadValid => true;
        public bool IsVoid => false;
        public bool DefinitelyType => true;
    }

    public class RuleContext : Node
    {
        public Token RuleToken { get; }
        public Token NameToken { get; }
        public Token Disabled { get; }
        public NumberExpression Order { get; }
        public List<RuleSetting> Settings { get; }
        public List<IfCondition> Conditions { get; }
        public IParseStatement Statement { get; }
        public string Name => Extras.RemoveQuotes(NameToken.GetText());

        public RuleContext(Token ruleToken, Token name, Token disabled, NumberExpression order, List<RuleSetting> settings, List<IfCondition> conditions, IParseStatement statement)
        {
            RuleToken = ruleToken;
            NameToken = name;
            Disabled = disabled;
            Order = order;
            Settings = settings;
            Conditions = conditions;
            Statement = statement;
        }
    }

    public class RuleSetting : Node
    {
        public Token Setting { get; }
        public Token Dot { get; }
        public Token Value { get; }

        public RuleSetting(Token setting, Token dot, Token value)
        {
            Setting = setting;
            Dot = dot;
            Value = value;
        }
    }

    public class ClassContext : Node
    {
        public Token Identifier { get; }
        public Token InheritToken { get; }
        public List<Token> Inheriting { get; }
        public List<IDeclaration> Declarations { get; } = new List<IDeclaration>();
        public List<ConstructorContext> Constructors { get; } = new List<ConstructorContext>();

        public ClassContext(Token identifier, Token inheritToken, List<Token> inheriting)
        {
            Identifier = identifier;
            InheritToken = inheritToken;
            Inheriting = inheriting;
        }
    }

    public class EnumContext : Node
    {
        public Token Identifier { get; }
        public List<EnumValue> Values { get; }

        public EnumContext(Token identifier, List<EnumValue> values)
        {
            Identifier = identifier;
            Values = values;
        }
    }

    public class EnumValue : Node
    {
        public Token Identifier { get; }
        public IParseExpression Value { get; }

        public EnumValue(Token identifier, IParseExpression value)
        {
            Identifier = identifier;
            Value = value;
        }
    }

    public class FunctionContext : Node, IDeclaration
    {
        public AttributeTokens Attributes { get; }
        public IParseType Type { get; }
        public Token Identifier { get; }
        public List<VariableDeclaration> Parameters { get; }
        public Block Block { get; }
        public Token GlobalVar { get; }
        public Token PlayerVar { get; }
        public Token Subroutine { get; }

        public FunctionContext(AttributeTokens attributes, IParseType type, Token identifier, List<VariableDeclaration> parameters, Block block, Token globalvar, Token playervar, Token subroutine)
        {
            Attributes = attributes;
            Type = type;
            Identifier = identifier;
            Parameters = parameters;
            Block = block;
            GlobalVar = globalvar;
            PlayerVar = playervar;
            Subroutine = subroutine;
        }
    }

    public class MacroFunctionContext : Node, IDeclaration
    {
        public AttributeTokens Attributes { get; }
        public IParseType Type { get; }
        public Token Identifier { get; }
        public List<VariableDeclaration> Parameters { get; }
        public IParseExpression Expression { get; }

        public MacroFunctionContext(AttributeTokens attributes, IParseType type, Token identifier, List<VariableDeclaration> parameters, IParseExpression expression)
        {
            Attributes = attributes;
            Type = type;
            Identifier = identifier;
            Parameters = parameters;
            Expression = expression;
        }
    }

    public class ConstructorContext
    {
        public AttributeTokens Attributes { get; }
        public Token LocationToken { get; }
        public List<VariableDeclaration> Parameters { get; }
        public Token SubroutineName { get; }
        public Block Block { get; }

        public ConstructorContext(AttributeTokens attributes, Token locationToken, List<VariableDeclaration> parameters, Token subroutineName, Block block)
        {
            Attributes = attributes;
            LocationToken = locationToken;
            Parameters = parameters;
            SubroutineName = subroutineName;
            Block = block;
        }
    }

    public class AttributeTokens
    {
        public Token Public { get; set; }
        public Token Private { get; set; }
        public Token Protected { get; set; }
        public Token Static { get; set; }
        public Token Override { get; set; }
        public Token Virtual { get; set; }
        public Token Recursive { get; set; }
        public Token GlobalVar { get; set; }
        public Token PlayerVar { get; set; }
        public Token Ref { get; set; }
        public List<Token> AllAttributes { get; } = new List<Token>();
        public AccessLevel GetAccessLevel() => Public != null ? AccessLevel.Public : Protected != null ? AccessLevel.Protected : AccessLevel.Private;
    }

    public class IfCondition
    {
        public Token If;
        public Token LeftParen;
        public IParseExpression Expression;
        public Token RightParen;

        public override string ToString() => "if (" + Expression.ToString() + ")";
    }

    public class Import
    {
        public Token File { get; }
        public Token As { get; }
        public Token Identifier { get; }

        public Import(Token file, Token @as, Token identifier)
        {
            File = file;
            As = @as;
            Identifier = identifier;
        }
    }

    public class Hook
    {
        public IParseExpression Variable { get; }
        public IParseExpression Value { get; }

        public Hook(IParseExpression variable, IParseExpression value)
        {
            Variable = variable;
            Value = value;
        }
    }

    // Both expressions and statements
    public class FunctionExpression : Node, IParseExpression, IParseStatement
    {
        public IParseExpression Target { get; }
        public List<ParameterValue> Parameters { get; }

        public FunctionExpression(IParseExpression target, List<ParameterValue> parameters)
        {
            Target = target;
            Parameters = parameters;
        }

        public override string ToString() => Target.ToString() + "(" + string.Join(", ", Parameters.Select(p => p.ToString())) + ")";
    }

    public class ParameterValue : IListComma
    {
        public Token PickyParameter { get; }
        public IParseExpression Expression { get; }
        public Token NextComma { get; set; }

        public ParameterValue(Token pickyParameter, IParseExpression value)
        {
            PickyParameter = pickyParameter;
            Expression = value;
        }

        public override string ToString() => Expression.ToString();
    }

    public class NewExpression : Node, IParseExpression, IParseStatement
    {
        public Token ClassIdentifier { get; }
        public List<ParameterValue> Parameters { get; }

        public NewExpression(Token classIdentifier, List<ParameterValue> parameters)
        {
            ClassIdentifier = classIdentifier;
            Parameters = parameters;
        }

        public override string ToString() => "new " + ClassIdentifier.Text + "(" + string.Join(", ", Parameters.Select(p => p.ToString())) + ")"; 
    }

    // Expressions
    public class BooleanExpression : Node, IParseExpression
    {
        public Token Token { get; }
        public bool Value { get; }

        public BooleanExpression(Token token, bool value)
        {
            Token = token;
            Value = value;
            Range = Token.Range;
        }

        public override string ToString() => Value.ToString();
    }

    public class NumberExpression : Node, IParseExpression
    {
        public double Value { get; }

        public NumberExpression(double value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }

    public class StringExpression : Node, IParseExpression
    {
        public Token Localized { get; }
        public Token Token { get; }
        public string Value { get; }
        public List<IParseExpression> Formats { get; }

        public StringExpression(Token localized, Token token)
        {
            Localized = localized;
            Token = token;
            Value = Extras.RemoveQuotes(token.Text);
        }

        public StringExpression(Token localized, Token token, List<IParseExpression> formats) : this(localized, token)
        {
            Formats = formats;
        }

        public override string ToString() => '"' + Value + '"'; 
    }

    public class Identifier : Node, IParseExpression
    {
        public Token Token { get; }
        public List<ArrayIndex> Index { get; }

        public Identifier(Token token, List<ArrayIndex> index)
        {
            Token = token;
            Index = index;
        }

        public override string ToString() => Token.Text + string.Concat(Index.Select(i => i.ToString()));
    }

    public class ArrayIndex
    {
        public IParseExpression Expression { get; }
        public Token LeftBracket { get; }
        public Token RightBracket { get; }

        public ArrayIndex(IParseExpression expression, Token leftBracket, Token rightBracket)
        {
            Expression = expression;
            LeftBracket = leftBracket;
            RightBracket = rightBracket;
        }

        public override string ToString() => "[" + Expression.ToString() + "]";
    }

    public class ValueInArray : Node, IParseExpression
    {
        public IParseExpression Array { get; }
        public IParseExpression Index { get; }

        public ValueInArray(IParseExpression array, IParseExpression index, Token closingToken)
        {
            Array = array;
            Index = index;
            Range = new DocRange(Array.Range.Start, closingToken.Range.End);
        }

        public override string ToString() => Array.ToString() + "[" + Index.ToString() + "]";
    }

    public class CreateArray : Node, IParseExpression
    {
        public List<IParseExpression> Values { get; }
        public Token LeftBracket { get; }
        public Token RightBracket { get; }

        public CreateArray(List<IParseExpression> values, Token leftBracket, Token rightBracket)
        {
            Values = values;
            LeftBracket = leftBracket;
            RightBracket = rightBracket;
        }
    }

    public class NullExpression : Node, IParseExpression
    {
        public Token Token { get; }

        public NullExpression(Token token)
        {
            Token = token;
            Range = Token.Range;
        }

        public override string ToString() => "null";
    }

    public class ThisExpression : Node, IParseExpression
    {
        public Token Token { get; }

        public ThisExpression(Token token)
        {
            Token = token;
            Range = Token.Range;
        }

        public override string ToString() => "this";
    }

    public class RootExpression : Node, IParseExpression
    {
        public Token Token { get; }

        public RootExpression(Token token)
        {
            Token = token;
            Range = Token.Range;
        }

        public override string ToString() => "root";
    }

    public class ExpressionGroup : Node, IParseExpression
    {
        public IParseExpression Expression { get; }
        public Token Left { get; }
        public Token Right { get; }

        public ExpressionGroup(IParseExpression expression, Token left, Token right)
        {
            Expression = expression;
            Left = left;
            Right = right;
        }
    }

    public class TypeCast : Node, IParseExpression
    {
        public IParseType Type { get; }
        public IParseExpression Expression { get; }

        public TypeCast(IParseType type, IParseExpression expression)
        {
            Type = type;
            Expression = expression;
        }

        public override string ToString() => "<" + Type.ToString() + ">" + Expression.ToString();
    }

    public class LambdaExpression : Node, IParseExpression
    {
        public List<LambdaParameter> Parameters { get; }
        public Token Arrow { get; }
        public IParseStatement Statement { get; }

        public LambdaExpression(List<LambdaParameter> parameters, Token arrow, IParseStatement statement)
        {
            Parameters = parameters;
            Arrow = arrow;
            Statement = statement;
        }
    }

    public class LambdaParameter : Node
    {
        public IParseType Type { get; }
        public Token Identifier { get; }

        public LambdaParameter(IParseType type, Token identifier)
        {
            Type = type;
            Identifier = identifier;
        }

        public LambdaParameter(Token identifier)
        {
            Identifier = identifier;
        }
    }

    // Statements
    public class ExpressionStatement : Node, IParseStatement
    {
        public IParseExpression Expression { get; }
        public Token ActionComment { get; }

        public ExpressionStatement(IParseExpression expression, Token actionComment)
        {
            Expression = expression;
            ActionComment = actionComment;
            Range = expression.Range;
        }

        public override string ToString() => Expression.ToString();
    }

    public class Block : Node, IParseStatement
    {
        public List<IParseStatement> Statements { get; }

        public Block(List<IParseStatement> statements)
        {
            Statements = statements;
        }

        public override string ToString() => "block [" + Statements.Count + " statements]";
    }

    public class Assignment : Node, IParseStatement, ICommentableStatement
    {
        public IParseExpression VariableExpression { get; }
        public Token AssignmentToken { get; }
        public IParseExpression Value { get; }
        public Token ActionComment { get; }

        public Assignment(IParseExpression variableExpression, Token assignmentToken, IParseExpression value, Token actionComment)
        {
            VariableExpression = variableExpression;
            AssignmentToken = assignmentToken;
            Value = value;
            ActionComment = actionComment;
        }

        public override string ToString() => VariableExpression.ToString() + " " + AssignmentToken.Text + " " + Value.ToString();
    }

    public class Return : Node, IParseStatement
    {
        public Token Token { get; }
        public IParseExpression Expression { get; }

        public Return(Token token, IParseExpression expression)
        {
            Token = token;
            Expression = expression;
        }

        public override string ToString() => "return " + Expression.ToString();
    }

    public class Continue : Node, IParseStatement
    {
        public override string ToString() => "continue";
    }

    public class Break : Node, IParseStatement
    {
        public override string ToString() => "break";
    }

    public class If : Node, IParseStatement
    {
        public IParseExpression Expression { get; }
        public IParseStatement Statement { get; }
        public List<ElseIf> ElseIfs { get; }
        public Else Else { get; }

        public If(IParseExpression expression, IParseStatement statement, List<ElseIf> elseIfs, Else els)
        {
            Expression = expression;
            Statement = statement;
            ElseIfs = elseIfs;
            Else = els;
        }
    }

    // ElseIf inherits 'IParseStatement' in the case of an 'else if' without an 'if'.
    public class ElseIf : Node, IParseStatement
    {
        public IParseExpression Expression { get; }
        public IParseStatement Statement { get; }

        public ElseIf(IParseExpression expression, IParseStatement statement)
        {
            Expression = expression;
            Statement = statement;
        }
    }

    // Else inherits 'IParseStatement' in the case of an 'else' without an 'if' or 'else if'.
    public class Else : Node, IParseStatement
    {
        public IParseStatement Statement { get; }

        public Else(IParseStatement statement)
        {
            Statement = statement;
        }
    }

    public class Switch : Node, IParseStatement
    {
        public IParseExpression Expression { get; }
        public List<IParseStatement> Statements { get; }

        public Switch(IParseExpression expression, List<IParseStatement> statements)
        {
            Expression = expression;
            Statements = statements;
        }
    }

    public class SwitchCase : Node, IParseStatement
    {
        public Token Token { get; }
        public IParseExpression Value { get; }
        public bool IsDefault { get; }

        public SwitchCase(Token caseToken, IParseExpression value)
        {
            Token = caseToken;
            Value = value;
            IsDefault = false;
        }

        public SwitchCase(Token defaultToken)
        {
            Token = defaultToken;
            IsDefault = true;
        }
    }

    public class For : Node, IParseStatement
    {
        public IParseStatement Initializer { get; }
        public IParseExpression Condition { get; }
        public IParseStatement Iterator { get; }
        public IParseStatement Block { get; }
        public Token InitializerSemicolon { get; }

        public For(IParseStatement initializer, IParseExpression condition, IParseStatement iterator, IParseStatement block, Token initializerSemicolon)
        {
            Initializer = initializer;
            Condition = condition;
            Iterator = iterator;
            Block = block;
            InitializerSemicolon = initializerSemicolon;
        }
    }

    public class While : Node, IParseStatement
    {
        public IParseExpression Condition { get; }
        public IParseStatement Statement { get; }

        public While(IParseExpression condition, IParseStatement statement)
        {
            Condition = condition;
            Statement = statement;
        }
    }

    public class Foreach : Node, IParseStatement
    {
        public IParseType Type { get; }
        public Token Identifier { get; }
        public IParseExpression Expression { get; }
        public IParseStatement Statement { get; }

        public Foreach(IParseType type, Token identifier, IParseExpression expression, IParseStatement statement)
        {
            Type = type;
            Identifier = identifier;
            Expression = expression;
            Statement = statement;
        }
    }

    public class VariableDeclaration : Node, IParseStatement, IDeclaration
    {
        public AttributeTokens Attributes { get; }
        public IParseType Type { get; }
        public Token Identifier { get; }
        public IParseExpression InitialValue { get; }
        public Token Extended { get; }
        public Token ID { get; }

        public VariableDeclaration(AttributeTokens attributes, IParseType type, Token identifier, IParseExpression initialValue, Token ext, Token id)
        {
            Attributes = attributes;
            Type = type;
            Identifier = identifier;
            InitialValue = initialValue;
            Extended = ext;
            ID = id;
        }
    }

    public class MacroVarDeclaration : Node, IDeclaration
    {
        public AttributeTokens Attributes { get; }
        public IParseType Type { get; }
        public Token Identifier { get; }
        public IParseExpression Value { get; }

        public MacroVarDeclaration(AttributeTokens attributes, IParseType type, Token identifier, IParseExpression value)
        {
            Attributes = attributes;
            Type = type;
            Identifier = identifier;
            Value = value;
        }
    }

    public class Increment : Node, IParseStatement
    {
        public IParseExpression VariableExpression { get; }
        public bool Decrement { get; }

        public Increment(IParseExpression variableExpression, bool decrement)
        {
            VariableExpression = variableExpression;
            Decrement = decrement;
        }

        public override string ToString() => VariableExpression.ToString() + (Decrement ? "--" : "++");
    }

    public class Delete : Node, IParseStatement
    {
        public IParseExpression Deleting { get; }

        public Delete(IParseExpression deleting)
        {
            Deleting = deleting;
        }
    }

    // Errors
    public class MissingElement : Node, IParseExpression, IParseStatement
    {
        public MissingElement(DocRange range)
        {
            Range = range;
        }
    }
}