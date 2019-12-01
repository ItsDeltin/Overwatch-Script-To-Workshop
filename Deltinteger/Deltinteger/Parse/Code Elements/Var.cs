using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IScopeable
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public Location DefinedAt { get; }
        public List<Location> CalledFrom { get; } = new List<Location>();
        public string ScopeableType { get; } = "variable";
        public CodeType Type { get; }

        public Var(string name, AccessLevel accessLevel, Location definedAt, CodeType type)
        {
            Name = name;
            AccessLevel = accessLevel;
            DefinedAt = definedAt;
            Type = type;
        }

        public CallVariableAction Call(DeltinScript translateInfo, Location calledFrom)
        {
            CalledFrom.Add(calledFrom);
            return new CallVariableAction(translateInfo, this);
        }
    }

    // TODO: Move IsGlobal, AccessLevel, Static, (maybe ID and DefineType) into the Var class.
    public class DefineAction : CodeAction, IStatement
    {
        public string Name { get; }
        public bool InExtendedCollection { get; }
        public IExpression InitialValue { get; }
        public Var Var { get; protected set; }

        public bool IsGlobal { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public bool Static { get; } = false;
        public int ID { get; }
        public VariableDefineType DefineType { get; }

        public DefineAction(VariableDefineType defineType, ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.DefineContext defineContext)
        {
            Name = defineContext.name.Text;
            InExtendedCollection = defineContext.NOT() != null;
            DefineType = defineType;

            if (defineContext.id != null)
            {
                if (defineType != VariableDefineType.RuleLevel)
                    script.Diagnostics.Error("Only defined variables at the rule level can be assigned an ID.", DocRange.GetRange(defineContext.id));
                else
                    ID = int.Parse(defineContext.id.GetText());
            }

            if (defineType == VariableDefineType.RuleLevel)
            {
                if (defineContext.GLOBAL() != null)
                    IsGlobal = true;
                else if (defineContext.PLAYER() != null)
                    IsGlobal = false;
                else
                    script.Diagnostics.Error("Expected the globalvar/playervar attribute.", DocRange.GetRange(defineContext));
            }

            if (defineType == VariableDefineType.InClass)
            {
                // Get the accessor
                AccessLevel = AccessLevel.Private;
                if (defineContext.accessor() != null)
                {
                    // Public
                    if (defineContext.accessor().PUBLIC() != null)
                        AccessLevel = AccessLevel.Public;
                    // Private
                    else if (defineContext.accessor().PRIVATE() != null)
                        AccessLevel = AccessLevel.Private;
                    else throw new NotImplementedException();
                }
                // Get the static attribute.
                Static = defineContext.STATIC() != null;

                // Syntax error if the variable has '!'.
                if (!Static && InExtendedCollection)
                    script.Diagnostics.Error("Non-static type variables can not be placed in the extended collection.", DocRange.GetRange(defineContext.NOT()));
            }
            else
            {
                // Syntax error if class only attributes is used somewhere else.
                if (defineContext.accessor() != null)
                    script.Diagnostics.Error("Only defined variables in classes can have an accessor.", DocRange.GetRange(defineContext.accessor()));
                if (defineContext.STATIC() != null)
                    script.Diagnostics.Error("Only defined variables in classes can be static.", DocRange.GetRange(defineContext.STATIC()));
            }

            // Syntax error if there is an '=' but no expression.
            if (defineContext.EQUALS() != null && defineContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(defineContext).end.ToRange());
            
            // Get the initial value.
            if (defineContext.expr() != null)
                InitialValue = GetExpression(script, translateInfo, scope, defineContext.expr());

            // Get the type.
            CodeType type = null;
            if (defineContext.type != null)
            {
                type = translateInfo.GetCodeType(defineContext.type.Text);
                if (type == null)
                    script.Diagnostics.Error(string.Format("The type {0} does not exist.", defineContext.type.Text), DocRange.GetRange(defineContext.type));
            }
            
            // Syntax error if the variable was already defined.
            if (scope.WasDefined(Name))
                script.Diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", Name), DocRange.GetRange(defineContext.name));
            else
            {
                // Add the new variable to the scope.
                Var = new Var(Name, AccessLevel, new Location(script.File, DocRange.GetRange(defineContext.name)), type);
                scope.In(Var);
            }
        }
    }

    public enum VariableDefineType
    {
        RuleLevel,
        Scoped,
        InClass,
        Parameter
    }
}