using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Models;
using Deltin.Deltinteger.Pathfinder;

namespace Deltin.Deltinteger.Parse
{
    public class VarCollection
    {
        public WorkshopVariable Global { get; }
        public WorkshopVariable Player { get; }

        public WorkshopArrayBuilder WorkshopArrayBuilder { get; }

        private int[] ReservedGlobalIDs { get; }
        private string[] ReservedGlobalNames { get; }
        private int[] ReservedPlayerIDs { get; }
        private string[] ReservedPlayerNames { get; }

        public VarCollection(int[] reservedGlobalIDs, string[] reservedGlobalNames, int[] reservedPlayerIDs, string[] reservedPlayerNames)
        {
            ReservedGlobalIDs   = reservedGlobalIDs;
            ReservedPlayerIDs   = reservedPlayerIDs;
            ReservedGlobalNames = reservedGlobalNames;
            ReservedPlayerNames = reservedPlayerNames;

            Global      = Assign("_extendedGlobalCollection", true);
            Player      = Assign("_extendedPlayerCollection", false);
            var builder = Assign("_arrayBuilder", true);

            IndexedVar tempArrayBuilderVar = IndexedVar.AssignInternalVar(this, null, "_arrayBuilderStore", true);
            WorkshopArrayBuilder = new WorkshopArrayBuilder(builder, tempArrayBuilderVar);
            tempArrayBuilderVar.ArrayBuilder = WorkshopArrayBuilder;
        }

        public WorkshopVariable Assign(string name, bool isGlobal)
        {
            int index = NextFree(isGlobal);

            WorkshopVariable workshopVariable = new WorkshopVariable(isGlobal, index, WorkshopNameFromCodeName(isGlobal, name));

            UseCollection(isGlobal)[index] = workshopVariable;

            return workshopVariable;
        }

        public WorkshopVariable FromID(bool isGlobal, int id) 
        {
            return UseCollection(isGlobal).FirstOrDefault(var => var != null && var.ID == id);
        }

        public int NextFree(bool isGlobal)
        {
            // Get the next free ID.
            int id = -1;
            var collection = UseCollection(isGlobal);
            for (int i = 0; i < Constants.NUMBER_OF_VARIABLES; i++)
                // Make sure the ID is not reserved.
                if (collection[i] == null && !(isGlobal ? ReservedGlobalIDs : ReservedPlayerIDs).Contains(i))
                {
                    id = i;
                    break;
                }

            // If ID still equals -1, there are no more free variables.
            if (id == -1)
                throw new Exception();
            return id;
        }

        public int NextFreeExtended(bool isGlobal)
        {
            int index = Array.IndexOf(isGlobal ? ExtendedGlobalCollection : ExtendedPlayerCollection, null);

            if (index == -1)
                throw new Exception();
            return index;
        }

        public string WorkshopNameFromCodeName(bool isGlobal, string name)
        {
            StringBuilder newName = new StringBuilder();

            // Remove invalid characters and replace ' ' with '_'.
            for (int i = 0; i < name.Length; i++)
                if (name[i] == ' ')
                    newName.Append('_');
                else if (WorkshopVariable.ValidVariableCharacters.Contains(name[i]))
                    newName.Append(name[i]);

            // Add a number to the end of the variable name if a variable with the same name was already created.
            if (NameTaken(isGlobal, newName.ToString()))
            {
                int num = 0;
                while (NameTaken(isGlobal, newName.ToString() + "_" + num)) num++;
                newName.Append("_" + num);
            }
            return newName.ToString();
        }

        private bool NameTaken(bool isGlobal, string name)
        {
            return UseCollection(isGlobal).Any(gv => gv != null && gv.Name == name) || (isGlobal ? ReservedGlobalNames : ReservedPlayerNames).Contains(name);
        }

        private void Add(WorkshopVariable variable)
        {
            UseCollection(variable.IsGlobal)[variable.ID] = variable;
        }

        public WorkshopVariable UseVariable(bool isGlobal) => isGlobal ? Global : Player;
        public WorkshopVariable[] UseCollection(bool isGlobal) => isGlobal ? GlobalVariables : PlayerVariables;
        public IndexedVar[] UseExtendedCollection(bool isGlobal) => isGlobal ? ExtendedGlobalCollection : ExtendedPlayerCollection;

        public readonly List<Var> AllVars = new List<Var>();

        private readonly WorkshopVariable[] GlobalVariables = new WorkshopVariable[Constants.NUMBER_OF_VARIABLES]; 
        private readonly WorkshopVariable[] PlayerVariables = new WorkshopVariable[Constants.NUMBER_OF_VARIABLES]; 
        private readonly IndexedVar[] ExtendedGlobalCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];
        private readonly IndexedVar[] ExtendedPlayerCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];

        public void ToWorkshop(StringBuilder builder)
        {
            builder.AppendLine("variables");
            builder.AppendLine("{");
            builder.AppendLine("    global:");
            WriteCollection(builder, UseCollection(true));
            builder.AppendLine("    player:");
            WriteCollection(builder, UseCollection(false));
            builder.AppendLine("}");
        }

        private void WriteCollection(StringBuilder builder, WorkshopVariable[] variables)
        {
            for (int i = 0; i < Constants.NUMBER_OF_VARIABLES; i++)
                if (variables[i] != null)
                    builder.AppendLine("        " + variables[i].ID + ": " + variables[i].Name);

        }
    }

    public abstract class Var : IScopeable
    {
        public string Name { get; }
        public ScopeGroup Scope { get; private set; }
        public bool IsDefinedVar { get; }
        public Node Node { get; }

        public DefinedType Type { get; set; }

        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;

        public Var(string name, ScopeGroup scope, Node node = null)
        {
            Name = name;
            Scope = scope;
            Node = node;
            IsDefinedVar = node != null;

            scope?./* we're */ In(this) /* together! */;
        }

        public abstract Element GetVariable(Element targetPlayer = null);

        public abstract bool Gettable();
        public abstract bool Settable();

        public override string ToString()
        {
            return Name;
        }
    }

    public class IndexedVar : Var
    {
        public static IndexedVar AssignVar(VarCollection collection, ScopeGroup scope, string name, bool isGlobal, Node node)
        {
            WorkshopVariable assignedVariable = collection.Assign(name, isGlobal);
            IndexedVar var = CreateVar(collection.WorkshopArrayBuilder, scope, name, isGlobal, assignedVariable, null, node);
            collection.AllVars.Add(var);
            return var;
        }
        public static IndexedVar AssignInternalVar(VarCollection collection, ScopeGroup scope, string name, bool isGlobal)
            => AssignVar(collection, scope, name, isGlobal, null);

        public static IndexedVar AssignVar(VarCollection collection, ScopeGroup scope, string name, bool isGlobal, WorkshopVariable variable, Node node)
        {
            string workshopName = variable.Name;

            if (workshopName == null)
                workshopName = collection.WorkshopNameFromCodeName(isGlobal, name);
            
            int id = variable.ID;

            if (id == -1)
                id = collection.NextFree(isGlobal);
            else 
            {
                WorkshopVariable assigned = collection.FromID(isGlobal, variable.ID);
                if (assigned != null)
                    throw new SyntaxErrorException("Variable ID '" + variable.ID + "' has already been assigned by '" + assigned.Name + "'.", node.Location);
            }
            
            WorkshopVariable use = new WorkshopVariable(isGlobal, id, workshopName);

            IndexedVar var = CreateVar(
                collection.WorkshopArrayBuilder,
                scope,
                name,
                isGlobal,
                use,
                null,
                node
            );
            collection.AllVars.Add(var);
            collection.UseCollection(isGlobal)[id] = use;
            return var;
        }
        
        public static IndexedVar AssignVarExt(VarCollection collection, ScopeGroup scope, string name, bool isGlobal, Node node)
        {
            int index = collection.NextFreeExtended(isGlobal);
            IndexedVar var = CreateVar(
                collection.WorkshopArrayBuilder,
                scope,
                name,
                isGlobal,
                collection.UseVariable(isGlobal),
                new Element[] { new V_Number(index) },
                node
            );
            collection.AllVars.Add(var);
            collection.UseExtendedCollection(isGlobal)[index] = var;
            return var;
        }
        public static IndexedVar AssignInternalVarExt(VarCollection collection, ScopeGroup scope, string name, bool isGlobal)
            => AssignVarExt(collection, scope, name, isGlobal, null);

        private static IndexedVar CreateVar(WorkshopArrayBuilder builder, ScopeGroup scope, string name, bool isGlobal, WorkshopVariable variable, Element[] index, Node node)
        {
            if (scope == null || !scope.Recursive)
                return new IndexedVar(builder, scope, name, isGlobal, variable, index, node);
            else
                return new RecursiveVar(builder, scope, name, isGlobal, variable, index, node);
        }

        public WorkshopArrayBuilder ArrayBuilder { get; set; }
        public bool IsGlobal { get; }
        public WorkshopVariable Variable { get; private set; }
        public Element[] Index { get; private set; }
        public bool UsesIndex { get { return Index != null && Index.Length > 0; } }
        public Element DefaultTarget { get; set; } = new V_EventPlayer();
        public bool Optimize2ndDim { get; set; } = false;

        public IndexedVar(WorkshopArrayBuilder arrayBuilder, ScopeGroup scopeGroup, string name, bool isGlobal, WorkshopVariable variable, Element[] index, Node node)
            : base (name, scopeGroup, node)
        {
            this.ArrayBuilder = arrayBuilder;
            IsGlobal = isGlobal;
            Variable = variable;
            Index = index;
        }

        override public bool Gettable() { return true; }
        override public bool Settable() { return true; }

        public override Element GetVariable(Element targetPlayer = null)
        {
            if (targetPlayer == null) targetPlayer = DefaultTarget;
            Element element = Get(targetPlayer);
            if (Type != null)
                element.SupportedType = this;
            return element;
        }

        protected virtual Element Get(Element targetPlayer = null)
        {
            return WorkshopArrayBuilder.GetVariable(targetPlayer, Variable, Index);
        }

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return WorkshopArrayBuilder.SetVariable(ArrayBuilder, value, targetPlayer, Variable, Optimize2ndDim, ArrayBuilder<Element>.Build(Index, setAtIndex));
        }

        public virtual Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return WorkshopArrayBuilder.ModifyVariable(ArrayBuilder, operation, value, targetPlayer, Variable, ArrayBuilder<Element>.Build(Index, setAtIndex));
        }
        
        public virtual Element[] InScope(Element initialValue, Element targetPlayer = null)
        {
            if (initialValue != null)
                return SetVariable(initialValue, targetPlayer);
            return null;
        }

        public virtual void OutOfScope(TranslateRule context, Element targetPlayer = null)
        {
        }

        public IndexedVar CreateChild(ScopeGroup scope, string name, Element[] index, Node node)
        {
            return new IndexedVar(ArrayBuilder, scope, name, IsGlobal, Variable, ArrayBuilder<Element>.Build(Index, index), node);
        }

        public override string ToString()
        {
            return 
            (IsGlobal ? "global" : "player") + " " + Variable.ID + ":" + Variable.Name + 
            (UsesIndex ? 
                "[" + string.Join(", ", Index.Select(i => i is V_Number ? ((V_Number)i).Value.ToString() : "?")) + "]"
            : "") + " "
            + (AdditionalToStringInfo != null ? AdditionalToStringInfo + " " : "")
            + Name;
        }

        protected virtual string AdditionalToStringInfo { get; } = null;
    }

    class ElementOrigin
    {
        public bool IsGlobal { get; }
        public Element Player { get; }
        public WorkshopVariable Variable { get; }
        public Element[] Index { get; }

        private ElementOrigin(bool isGlobal, Element player, WorkshopVariable variable, Element[] index)
        {
            IsGlobal = isGlobal;
            Player = player;
            Variable = variable;
            Index = index;
        }

        public IndexedVar OriginVar(VarCollection varCollection, ScopeGroup scope, string name)
        {
            return new IndexedVar(varCollection.WorkshopArrayBuilder, scope, name, IsGlobal, Variable, Index, null);
        }

        public static ElementOrigin GetElementOrigin(Element element)
        {
            bool isGlobal = false;
            Element player = null;
            WorkshopVariable variable = null;

            Element checking = element;
            List<Element> index = new List<Element>();
            while (checking != null)
            {
                if (checking is V_GlobalVariable)
                {
                    isGlobal = true;
                    player = null;
                    variable = (WorkshopVariable)checking.ParameterValues[0];
                    checking = null;
                }
                else if (checking is V_PlayerVariable)
                {
                    isGlobal = false;
                    player = (Element)checking.ParameterValues[0];
                    variable = (WorkshopVariable)checking.ParameterValues[1];
                    checking = null;
                }
                else if (checking is V_ValueInArray)
                {
                    index.Add((Element)checking.ParameterValues[1]);
                    checking = (Element)checking.ParameterValues[0];
                }
                else return null;
            }
            
            return new ElementOrigin(isGlobal, player, variable, index.ToArray());
        }
    }

    public class RecursiveVar : IndexedVar
    {
        public RecursiveVar(WorkshopArrayBuilder arrayBuilder, ScopeGroup scopeGroup, string name, bool isGlobal, WorkshopVariable variable, Element[] index, Node node)
            : base (arrayBuilder, scopeGroup, name, isGlobal, variable, index, node)
        {
        }

        protected override Element Get(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.Get(targetPlayer));
        }

        public override Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return base.SetVariable(value, targetPlayer, CurrentIndex(targetPlayer, setAtIndex));
        }

        public override Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return base.ModifyVariable(operation, value, targetPlayer, CurrentIndex(targetPlayer, setAtIndex));
        }

        private Element[] CurrentIndex(Element targetPlayer, params Element[] setAtIndex)
        {
            return ArrayBuilder<Element>.Build(
                Element.Part<V_CountOf>(base.Get(targetPlayer)) - 1,
                setAtIndex
            );
        }

        public override Element[] InScope(Element initialValue, Element targetPlayer = null)
        {
            return base.SetVariable(initialValue, targetPlayer, Element.Part<V_CountOf>(base.Get(targetPlayer)));
        }

        public override void OutOfScope(TranslateRule context, Element targetPlayer = null)
        {
            Element get = base.Get(targetPlayer);
            context.Actions.AddRange(base.SetVariable(
                Element.Part<V_ArraySlice>(
                    get,
                    new V_Number(0),
                    Element.Part<V_CountOf>(get) - 1
                ),
                targetPlayer
            ));

            base.OutOfScope(context, targetPlayer);
        }

        protected override string AdditionalToStringInfo { get; } = "RECURSIVE";

        public Element DebugStack(Element targetPlayer = null)
        {
            return base.Get(targetPlayer);
        }
    }

    public class ElementReferenceVar : Var
    {
        public IWorkshopTree Reference { get; set; }

        public ElementReferenceVar(string name, ScopeGroup scope, Node node, IWorkshopTree reference) : base (name, scope, node)
        {
            Reference = reference;
        }

        public override Element GetVariable(Element targetPlayer = null)
        {
            if (targetPlayer != null && !(targetPlayer is V_EventPlayer))
                throw new Exception($"{nameof(targetPlayer)} must be null or EventPlayer.");
            
            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();
            
            if (Reference == null)
                throw new ArgumentNullException(nameof(Reference));

            if (Reference is Element == false)
                throw new Exception("Reference is not an element, can't get the variable.");

            return (Element)Reference;
        }

        override public bool Gettable() { return Reference is Element; }
        override public bool Settable() { return false; }

        public override string ToString()
        {
            return "element reference : " + Name;
        }
    }

    public class VarRef : IWorkshopTree
    {
        public Var Var { get; }
        public Element[] Index { get; }
        public Element Target { get; }

        public VarRef(Var var, Element[] index, Element target)
        {
            Var = var;
            Index = index;
            Target = target;
        }

        public string ToWorkshop()
        {
            return ((IndexedVar)Var).Variable.Name;
        }

        public void DebugPrint(Log log, int depth)
        {
            // throw new NotImplementedException();
        }
    }
}