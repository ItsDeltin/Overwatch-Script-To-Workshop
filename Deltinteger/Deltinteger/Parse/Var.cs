using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class IndexReference : IGettable
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

        public virtual IWorkshopTree GetVariable(Element targetPlayer = null)
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

        public IndexReference CreateChild(params Element[] index)
        {
            // Note: `ArrayBuilder` and `ArrayBuilder<Element>` are 2 very different things.
            return new IndexReference(ArrayBuilder, WorkshopVariable, ArrayBuilder<Element>.Build(Index, index));
        }
    }

    public class RecursiveIndexReference : IndexReference
    {
        public RecursiveIndexReference(WorkshopArrayBuilder arrayBuilder, WorkshopVariable workshopVariable, params Element[] index) : base(arrayBuilder, workshopVariable, index)
        {
        }

        public override IWorkshopTree GetVariable(Element targetPlayer = null)
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

    public class WorkshopElementReference : IGettable
    {
        public IWorkshopTree WorkshopElement { get; }

        public WorkshopElementReference(IWorkshopTree element)
        {
            WorkshopElement = element;
        }

        public IWorkshopTree GetVariable(Element targetPlayer) => WorkshopElement;
    }

    public class VarIndexAssigner
    {
        private readonly Dictionary<Var, IGettable> references = new Dictionary<Var, IGettable>();
        private readonly List<VarIndexAssigner> children = new List<VarIndexAssigner>();
        private readonly VarIndexAssigner parent = null;

        public VarIndexAssigner() {}
        private VarIndexAssigner(VarIndexAssigner parent)
        {
            this.parent = parent;
        }

        public void Add(VarCollection varCollection, Var var, bool isGlobal, IWorkshopTree referenceValue)
        {
            if (varCollection == null) throw new ArgumentNullException(nameof(varCollection));
            if (var == null)           throw new ArgumentNullException(nameof(var          ));

            // A gettable/settable variable
            if (var.Settable())
                references.Add(var, varCollection.Assign(var, isGlobal));
            
            // Element reference
            else if (var.VariableType == VariableType.ElementReference)
            {
                if (referenceValue == null) throw new ArgumentNullException(nameof(referenceValue));
                references.Add(var, new WorkshopElementReference(referenceValue));
            }
            
            else throw new NotImplementedException();
        }

        public void Add(Var var, IndexReference reference)
        {
            references.Add(var, reference);
        }

        public VarIndexAssigner CreateContained()
        {
            VarIndexAssigner newAssigner = new VarIndexAssigner(this);
            children.Add(newAssigner);
            return newAssigner;
        }

        public IGettable this[Var var]
        {
            get {
                VarIndexAssigner current = this;
                while (current != null)
                {
                    if (current.references.ContainsKey(var))
                        return current.references[var];

                    current = current.parent;
                }

                throw new Exception(string.Format("The variable {0} is not assigned to an index.", var.Name));
            }
            private set {}
        }
    }

    public class Var : IScopeable, IExpression, ICallable
    {
        // IScopeable
        public string Name { get; }
        public AccessLevel AccessLevel { get; private set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; private set; }

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

        public bool Settable()
        {
            return (CodeType == null || !CodeType.Constant()) && (VariableType == VariableType.Global || VariableType == VariableType.Player || VariableType == VariableType.Dynamic);
            // if (CodeType == null) return true;
            // else return CodeType.Constant();
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
        public void Call(ScriptFile script, DocRange callRange)
        {
            ThrowIfNotFinalized();
            CalledFrom.Add(new Location(script.Uri, callRange));
            script.AddDefinitionLink(callRange, DefinedAt);
        }

        public static Var CreateVarFromContext(VariableDefineType defineType, ScriptFile script, DeltinScript translateInfo, DeltinScriptParser.DefineContext context)
        {
            Var newVar;
            if (context.name != null)
                newVar = new Var(context.name.Text, new Location(script.Uri, DocRange.GetRange(context.name)));
            else
                newVar = new Var(null, new Location(script.Uri, DocRange.GetRange(context)));


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
            else
            {
                if (context.GLOBAL() != null)
                    script.Diagnostics.Error("The globalvar attribute is only allowed on variables defined at the rule level.", DocRange.GetRange(context.GLOBAL()));
                if (context.PLAYER() != null)
                    script.Diagnostics.Error("The playervar attribute is only allowed on variables defined at the rule level.", DocRange.GetRange(context.PLAYER()));
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

            // If the type is InClass or RuleLevel, set WholeContext to true.
            // WholeContext's value for parameters don't matter since parameters are defined at the start anyway.
            newVar.WholeContext = defineType == VariableDefineType.InClass || defineType == VariableDefineType.RuleLevel;

            // Get the type.
            CodeType type = null;
            if (context.type != null)
            {
                type = translateInfo.GetCodeType(context.type.Text, script.Diagnostics, DocRange.GetRange(context.type));

                if (type != null)
                {
                    if (type is DefinedType)
                        ((DefinedType)type).Call(script, DocRange.GetRange(context.type));
                    // If variables with the type cannot be set to, set referenceInitialValue to true. 'type' can still equal null if the type name is invalid.
                    if (type.Constant())
                        newVar.VariableType = VariableType.ElementReference;
                }
            }

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
            scope.AddVariable(this, script.Diagnostics, DefinedAt.range);
            finalized = true;
        }

        private void ThrowIfNotFinalized()
        {
            if (!finalized) throw new Exception("Var not finalized.");
        }
    
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return actionSet.IndexAssigner[this].GetVariable();
        }
    
        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Variable
            };
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
            Element initialValue = 0;
            if (DefiningVariable.InitialValue != null)
                // TODO: Don't cast to Element.
                initialValue = (Element)DefiningVariable.InitialValue.Parse(actionSet);
            
            actionSet.IndexAssigner.Add(actionSet.VarCollection, DefiningVariable, actionSet.IsGlobal, initialValue);

            if (DefiningVariable.Settable())
                actionSet.AddAction(
                    ((IndexReference)actionSet.IndexAssigner[DefiningVariable]).SetVariable(
                        initialValue
                    )
                );
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
        // Dynamic variables are either global or player, depending on the rule it is defined in.
        Dynamic,
        // Global variable.
        Global,
        // Player variable.
        Player,
        // The variable references an element.
        ElementReference
    }
}