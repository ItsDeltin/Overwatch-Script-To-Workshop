using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    // Interfaces
    public interface IParseExpression {}
    public interface IParseStatement {}

    public class ParseType
    {
        public Token Identifier { get; }
        public List<ParseType> TypeArgs { get; }
        public int ArrayCount { get; }

        public ParseType(Token identifier, List<ParseType> typeArgs, int arrayCount)
        {
            Identifier = identifier;
            TypeArgs = typeArgs;
            ArrayCount = arrayCount;
        }

        public bool HasTypeArgs => TypeArgs != null && TypeArgs.Count > 0;
        public bool IsArray => ArrayCount > 0;
    }

    public class RuleContext
    {
        public Token NameToken { get; }
        public List<IfCondition> Conditions { get; }
        public IParseStatement Statement { get; }
        public string Name => Extras.RemoveQuotes(NameToken.Text);

        public RuleContext(Token name, List<IfCondition> conditions, IParseStatement statement)
        {
            NameToken = name;
            Conditions = conditions;
            Statement = statement;
        }
    }

    public class IfCondition
    {
        public Token If;
        public Token LeftParen;
        public IParseExpression Expression;
        public Token RightParen;

        public override string ToString() => "if (" + Expression.ToString() + ")";
    }

    // Both expressions and statements
    public class FunctionExpression : IParseExpression, IParseStatement
    {
        public Token Identifier { get; }
        public List<FunctionParameter> Parameters { get; }

        public FunctionExpression(Token identifier, List<FunctionParameter> parameters)
        {
            Identifier = identifier;
            Parameters = parameters;
        }

        public override string ToString() => Identifier.Text + "(" + string.Join(", ", Parameters.Select(p => p.ToString())) + ")";
    }

    public class FunctionParameter
    {
        public IParseExpression Expression { get; }
        public Token NextComma { get; }

        public FunctionParameter(IParseExpression value, Token comma)
        {
            Expression = value;
            NextComma = comma;
        }

        public override string ToString() => Expression.ToString();
    }

    // Expressions
    public class BooleanExpression : IParseExpression
    {
        public Token Token { get; }
        public bool Value { get; }

        public BooleanExpression(Token token, bool value)
        {
            Token = token;
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }

    public class NumberExpression : IParseExpression
    {
        public Token Token { get; }
        public double Value { get; }

        public NumberExpression(Token token)
        {
            Token = token;
            Value = double.Parse(token.Text);
        }

        public override string ToString() => Value.ToString();
    }

    public class StringExpression : IParseExpression
    {
        public Token Token { get; }
        public string Value { get; }

        public StringExpression(Token token)
        {
            Token = token;
            Value = Extras.RemoveQuotes(token.Text);
        }

        public override string ToString() => '"' + Value + '"'; 
    }

    public class Identifier : IParseExpression
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

    // Statements
    public class ExpressionStatement : IParseStatement
    {
        public IParseExpression Expression { get; }

        public ExpressionStatement(IParseExpression expression)
        {
            Expression = expression;
        }

        public override string ToString() => Expression.ToString();
    }

    public class Block : IParseStatement
    {
        public List<IParseStatement> Statements { get; }

        public Block(List<IParseStatement> statements)
        {
            Statements = statements;
        }

        public override string ToString() => "block [" + Statements.Count + " statements]";
    }

    public class Assignment : IParseStatement
    {
        public IParseExpression VariableExpression { get; }
        public Token AssignmentToken { get; }
        public IParseExpression Value { get; }

        public Assignment(IParseExpression variableExpression, Token assignmentToken, IParseExpression value)
        {
            VariableExpression = variableExpression;
            AssignmentToken = assignmentToken;
            Value = value;
        }

        public override string ToString() => VariableExpression.ToString() + " " + AssignmentToken.Text + " " + Value.ToString();
    }

    public class Return : IParseStatement
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

    public class Continue : IParseStatement
    {
        public override string ToString() => "continue";
    }

    public class Break : IParseStatement
    {
        public override string ToString() => "break";
    }

    public class If : IParseStatement
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
    public class ElseIf : IParseStatement
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
    public class Else : IParseStatement
    {
        public IParseStatement Statement { get; }

        public Else(IParseStatement statement)
        {
            Statement = statement;
        }
    }

    public class For : IParseStatement
    {
        public IParseStatement Initializer { get; }
        public IParseExpression Condition { get; }
        public IParseStatement Iterator { get; }
        public IParseStatement Block { get; }

        public For(IParseStatement initializer, IParseExpression condition, IParseStatement iterator, IParseStatement block)
        {
            Initializer = initializer;
            Condition = condition;
            Iterator = iterator;
            Block = block;
        }
    }

    public class Declaration : IParseStatement
    {
        public ParseType Type { get; }
        public Token Identifier { get; }
        public Token Assignment { get; }
        public IParseExpression InitialValue { get; }

        public Declaration(ParseType type, Token identifier, Token assignment, IParseExpression initialValue)
        {
            Type = type;
            Identifier = identifier;
            Assignment = assignment;
            InitialValue = initialValue;
        }
    }

    public class Increment : IParseStatement
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

    // Errors
    public class MissingElement : IParseExpression, IParseStatement {}
}