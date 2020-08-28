using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    // Interfaces
    public interface IParseExpression {}
    public interface IParseStatement {}


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

    public class Identifier : IParseExpression
    {
        public Token Token { get; }

        public Identifier(Token token)
        {
            Token = token;
        }

        public override string ToString() => Token.Text;
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

    // Errors
    public class MissingExpression : IParseExpression {}
}