#nullable enable

using System.Collections;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse;

sealed class IsPatternMatchingExpression : IExpression
{
    public IsPatternMatchingExpression(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression op)
    {
        var lhs = parseInfo.GetExpression(scope, op.Left);
    }

    void GetRhsTarget(ParseInfo parseInfo, Scope scope, IParseExpression rhs)
    {
        IParseExpression[] GetRhsPath()
        {
            return [];
        }
        var path = GetRhsPath();

        CodeType? activeType = null;

        for (int i = 0; i < path.Length; i++)
        {
            if (rhs is Identifier rhsAsIdentifier)
            {
                if (i == 0)
                {
                    activeType = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, rhsAsIdentifier);
                }
                else if (activeType is not null)
                {
                    // Find item with name.
                    var activeScope = activeType.ReturningScope();
                    string identifier = rhsAsIdentifier.Token.Text;
                }
            }
        }
    }

    public IWorkshopTree Parse(ActionSet actionSet)
    {
        throw new System.NotImplementedException();
    }

    public CodeType Type()
    {
        throw new System.NotImplementedException();
    }
}