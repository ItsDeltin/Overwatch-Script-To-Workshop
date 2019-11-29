using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface IStatement {}

    public class DefineAction : CodeAction, IStatement
    {
        public IExpression InitialValue { get; }
        public bool InExtendedCollection { get; }
        public string Name { get; }
        public Var Var { get; }

        public DefineAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.DefineContext defineContext)
        {
            Name = defineContext.name.Text;
            InExtendedCollection = defineContext.NOT() != null;

            // Syntax error if there is an '=' but no expression.
            if (defineContext.EQUALS() != null && defineContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(defineContext).end.ToRange());
            
            // Get the initial value.
            if (defineContext.expr() != null)
                InitialValue = GetExpression(script, translateInfo, scope, defineContext.expr());
            
            // Syntax error if the variable was already defined.
            if (scope.WasDefined(Name))
                script.Diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", Name), DocRange.GetRange(defineContext.name));
            else
            {
                // Add the new variable to the scope.
                Var = new Var(Name, AccessLevel.Private, new Location(script.File, DocRange.GetRange(defineContext.name)));
                scope.In(Var);
            }
        }
    }
}