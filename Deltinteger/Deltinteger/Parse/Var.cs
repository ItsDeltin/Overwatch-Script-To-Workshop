using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class IndexReference
    {
        public WorkshopArrayBuilder ArrayBuilder { get; set; }
        public WorkshopVariable WorkshopVariable { get; }
        public Element[] Index { get; }

        public IndexReference(WorkshopArrayBuilder arrayBuilder, WorkshopVariable workshopVariable, params Element[] index)
        {
            ArrayBuilder = arrayBuilder;
            WorkshopVariable = workshopVariable;
            Index = index;
        }

        public virtual Element GetVariable(Element targetPlayer = null)
        {
            return WorkshopArrayBuilder.GetVariable(targetPlayer, WorkshopVariable, Index);
        }

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] index)
        {
            return WorkshopArrayBuilder.SetVariable(ArrayBuilder, value, targetPlayer, WorkshopVariable, false, ArrayBuilder<Element>.Build(Index, index));
        }

        public virtual Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] index)
        {
            return WorkshopArrayBuilder.ModifyVariable(ArrayBuilder, operation, value, targetPlayer, WorkshopVariable, ArrayBuilder<Element>.Build(Index, index));
        }
    }

    public class RecursiveIndexReference : IndexReference
    {
        public RecursiveIndexReference(WorkshopArrayBuilder arrayBuilder, WorkshopVariable workshopVariable, params Element[] index) : base(arrayBuilder, workshopVariable, index)
        {
        }

        public override Element GetVariable(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.GetVariable(targetPlayer));
        }

        public override Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] index)
        {
            return base.SetVariable(value, targetPlayer, CurrentIndex(targetPlayer, index));
        }

        public override Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] index)
        {
            return base.ModifyVariable(operation, value, targetPlayer, CurrentIndex(targetPlayer, index));
        }

        private Element[] CurrentIndex(Element targetPlayer, params Element[] setAtIndex)
        {
            return ArrayBuilder<Element>.Build(
                Element.Part<V_CountOf>(base.GetVariable(targetPlayer)) - 1,
                setAtIndex
            );
        }
    }

    public class VarScopeAssigner
    {
        private readonly Dictionary<Var, IndexReference> References = new Dictionary<Var, IndexReference>();
        private readonly VarCollection VarCollection;
        private readonly Scope Scope;

        public VarScopeAssigner(VarCollection varCollection, Scope scope, bool isGlobal)
        {
            VarCollection = varCollection;
            Scope = scope;
            IScopeable[] variables = Scope.AllChildVariables();

            foreach (var scopeable in variables)
            {
                Var var = (Var)scopeable;

                // variableIsGlobal will equal isGlobal if var.VariableType is dynamic. Otherwise, it will equal is var.VariableType global.
                bool variableIsGlobal = var.VariableType == VariableType.Dynamic ? isGlobal : var.VariableType == VariableType.Global;
                References.Add(var, varCollection.Assign(var.Name, isGlobal, var.InExtendedCollection));
            }
        }
    }

    public class Var : IScopeable, IExpression, ICallable
    {
        // IScopeable
        public string Name { get; }
        public AccessLevel AccessLevel { get; private set; }
        public Location DefinedAt { get; }
        public string ScopeableType { get; } = "variable";

        private List<Location> CalledFrom { get; } = new List<Location>();

        public CodeType CodeType { get; private set; }

        // Attributes
        public VariableType VariableType { get; private set; }
        public bool InExtendedCollection { get; private set; }
        public VariableDefineType DefineType { get; private set; }
        public int ID { get; private set; } = -1;
        public bool Static { get; private set; }

        private DeltinScriptParser.DefineContext context;
        private ScriptFile script;
        private DeltinScript translateInfo;
        private bool finalized;

        public IExpression InitialValue { get; private set; }

        protected Var(string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
        }

        // IExpression
        public Scope ReturningScope()
        {
            ThrowIfNotFinalized();
            if (CodeType == null) return translateInfo.PlayerVariableScope;
            else return CodeType.GetObjectScope();
        }
        public CodeType Type()
        {
            ThrowIfNotFinalized();
            return CodeType;
        }

        // ICallable
        public void Call(Location calledFrom)
        {
            ThrowIfNotFinalized();
            CalledFrom.Add(calledFrom);
        }

        public static Var CreateVarFromContext(VariableDefineType defineType, ScriptFile script, DeltinScript translateInfo, DeltinScriptParser.DefineContext context)
        {
            Var newVar = new Var(context.name.Text, new Location(script.File, DocRange.GetRange(context.name)));
            newVar.context = context;
            newVar.script = script;
            newVar.translateInfo = translateInfo;

            if (context.accessor() != null) newVar.AccessLevel = context.accessor().GetAccessLevel();
            if (context.type != null) newVar.CodeType = translateInfo.GetCodeType(context.type.Text, script.Diagnostics, DocRange.GetRange(context.type));

            newVar.InExtendedCollection = context.NOT() != null;
            newVar.DefineType = defineType;

            // Check if global/player.
            if (defineType == VariableDefineType.RuleLevel)
            {
                if (context.GLOBAL() != null)
                    newVar.VariableType = VariableType.Global;
                else if (context.PLAYER() != null)
                    newVar.VariableType = VariableType.Player;
                else
                    script.Diagnostics.Error("Expected the globalvar/playervar attribute.", DocRange.GetRange(context));
            }

            // Get the ID
            if (context.id != null)
            {
                if (defineType != VariableDefineType.RuleLevel)
                    script.Diagnostics.Error("Only defined variables at the rule level can be assigned an ID.", DocRange.GetRange(context.id));
                else
                {
                    newVar.ID = int.Parse(context.id.GetText());
                    translateInfo.VarCollection.Reserve(newVar.ID, newVar.VariableType == VariableType.Global, script.Diagnostics, DocRange.GetRange(context.id));
                }
            }

            if (defineType == VariableDefineType.InClass)
            {
                // Get the accessor
                newVar.AccessLevel = AccessLevel.Private;
                if (context.accessor() != null)
                    newVar.AccessLevel = context.accessor().GetAccessLevel();
                // Get the static attribute.
                newVar.Static = context.STATIC() != null;

                // Syntax error if the variable has '!'.
                if (!newVar.Static && newVar.InExtendedCollection)
                    script.Diagnostics.Error("Non-static type variables can not be placed in the extended collection.", DocRange.GetRange(context.NOT()));
            }
            else
            {
                // Syntax error if class only attributes is used somewhere else.
                if (context.accessor() != null)
                    script.Diagnostics.Error("Only defined variables in classes can have an accessor.", DocRange.GetRange(context.accessor()));
                if (context.STATIC() != null)
                    script.Diagnostics.Error("Only defined variables in classes can be static.", DocRange.GetRange(context.STATIC()));
            }

            // Get the type.
            CodeType type = null;
            if (context.type != null)
                type = translateInfo.GetCodeType(context.type.Text, script.Diagnostics, DocRange.GetRange(context.type));

            // Syntax error if there is an '=' but no expression.
            if (context.EQUALS() != null && context.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context).end.ToRange());
            
            return newVar;
        }

        public void Finalize(Scope scope)
        {
            // Get the initial value.
            if (context.expr() != null)
                InitialValue = DeltinScript.GetExpression(script, translateInfo, scope, context.expr());
            
            // Add the variable to the scope.
            scope.AddVariable(this, script.Diagnostics, DocRange.GetRange(context.name));
            finalized = true;
        }

        private void ThrowIfNotFinalized()
        {
            if (!finalized) throw new Exception("Var not finalized.");
        }
    
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            // TODO: return variable index or workshop enum or constant or whatever.
            throw new NotImplementedException();
        }
    }

    class DefineAction : IStatement
    {
        public Var DefiningVariable { get; }

        public DefineAction(Var var)
        {
            DefiningVariable = var;
        }

        public void Translate(ActionSet actionSet)
        {
            // TODO: Set DefiningVariable to its initial value.
            throw new NotImplementedException();
        }
    }

    public enum VariableDefineType
    {
        RuleLevel,
        Scoped,
        InClass,
        Parameter
    }

    public enum VariableType
    {
        Dynamic,
        Global,
        Player
    }
}