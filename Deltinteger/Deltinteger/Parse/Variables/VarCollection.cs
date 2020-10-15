using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class VarCollection
    {
        public WorkshopArrayBuilder ArrayBuilder { get; private set; }

        // Indicates the workshop variables to store the extended collections at.
        private WorkshopVariable global;
        private WorkshopVariable player;

        // Reserved IDs and names.
        private readonly List<int> reservedGlobalIDs = new List<int>();
        private readonly List<int> reservedPlayerIDs = new List<int>();
        List<int> reservedIDs(bool isGlobal) => isGlobal ? reservedGlobalIDs : reservedPlayerIDs;
        private readonly List<string> reservedGlobalNames = new List<string>();
        private readonly List<string> reservedPlayerNames = new List<string>();
        List<string> reservedNames(bool isGlobal) => isGlobal ? reservedGlobalNames : reservedPlayerNames;

        // Variables
        private readonly List<WorkshopVariable> globalVariables = new List<WorkshopVariable>();
        private readonly List<WorkshopVariable> playerVariables = new List<WorkshopVariable>();
        List<WorkshopVariable> variableList(bool isGlobal) => isGlobal ? globalVariables : playerVariables;
        // Variables in the extended collections
        private readonly List<ExtendedVariable> extendedGlobalVariables = new List<ExtendedVariable>();
        private readonly List<ExtendedVariable> extendedPlayerVariables = new List<ExtendedVariable>();
        public List<ExtendedVariable> ExtendedVariableList(bool isGlobal) => isGlobal ? extendedGlobalVariables : extendedPlayerVariables;

        private bool globalLimitReached = false;
        private bool playerLimitReached = false;
        private bool extGlobalLimitReached = false;
        private bool extPlayerLimitReached = false;

        public VarCollection() {}

        public void Setup()
        {
            global      = AssignWorkshopVariable("_extendedGlobalCollection", true);
            player      = AssignWorkshopVariable("_extendedPlayerCollection", false);
            var builder = AssignWorkshopVariable("_arrayBuilder", true);

            IndexReference store = Assign("_arrayBuilderStore", true, true);
            ArrayBuilder = new WorkshopArrayBuilder(builder, store);
            // The store shouldn't require an instance of the WorkshopArrayBuilder, but if for some reason it does uncomment the line below.
            // store.ArrayBuilder = arrayBuilder;
        }

        public void Reserve(int id, bool isGlobal, FileDiagnostics diagnostics, DocRange range)
        {
            // Throw a syntax error if the ID was already reserved.
            if (reservedIDs(isGlobal).Contains(id))
            {
                string msg = string.Format("The id {0} is already reserved in the {1} collection.", id, isGlobal ? "global" : "player");

                if (range != null)
                    diagnostics.Error(msg, range);
            }
            // Add the ID to the reserved list.
            else reservedIDs(isGlobal).Add(id);
        }
        public void Reserve(string name, bool isGlobal)
        {
            // Add the ID to the reserved list.
            if (!reservedNames(isGlobal).Contains(name)) reservedNames(isGlobal).Add(name);
        }

        private string[] NamesTaken(bool isGlobal)
        {
            List<string> names = new List<string>();
            names.AddRange(variableList(isGlobal).Where(v => v != null).Select(v => v.Name));
            names.AddRange(reservedNames(isGlobal));
            return names.ToArray();
        }
    
        private WorkshopVariable AssignWorkshopVariable(string name, bool isGlobal)
        {
            int id = NextFreeID(isGlobal);
            WorkshopVariable workshopVariable = new WorkshopVariable(isGlobal, id, MetaElement.WorkshopNameFromCodeName(name, NamesTaken(isGlobal)));
            variableList(isGlobal).Add(workshopVariable);
            return workshopVariable;
        }
        
        private int NextFreeID(bool isGlobal)
        {
            // Get the next free ID.
            for (int i = 0;; i++)
                // Make sure the ID is not reserved.
                if (!variableList(isGlobal).Any(var => var.ID == i) && !reservedIDs(isGlobal).Contains(i))
                {
                    // Set 'globalLimitReached' or 'playerLimitReached' to true when the variable limit is reached.
                    if (i > Constants.NUMBER_OF_VARIABLES)
                    {
                        if (isGlobal) globalLimitReached = true;
                        else playerLimitReached = true;
                    }
                    return i;
                }
        }

        private int NextFreeExtended(bool isGlobal)
        {
            for (int i = 0;; i++)
                if (!ExtendedVariableList(isGlobal).Any(ex => ex.Index == i))
                {
                    // Set 'extGlobalLimitReached' or 'extPlayerLimitReached' to true when the variable limit is reached.
                    if (i > Constants.MAX_ARRAY_LENGTH)
                    {
                        if (isGlobal) extGlobalLimitReached = true;
                        else extPlayerLimitReached = true;
                    }
                    return i;
                }
        }
    
        public IndexReference Assign(string name, bool isGlobal, bool extended)
        {
            if (!extended)
                return new IndexReference(ArrayBuilder, AssignWorkshopVariable(name, isGlobal));
            else
            {
                int index = NextFreeExtended(isGlobal);
                IndexReference reference = new IndexReference(ArrayBuilder, isGlobal ? global : player, Element.Num(index));
                ExtendedVariableList(isGlobal).Add(new ExtendedVariable(name, reference, index));
                return reference;
            }
        }

        public IndexReference Assign(Var var, bool isGlobal)
        {
            // variableIsGlobal will equal isGlobal if var.VariableType is dynamic. Otherwise, it will equal is var.VariableType global.
            bool variableIsGlobal = var.VariableType == VariableType.Dynamic ? isGlobal : var.VariableType == VariableType.Global;

            if (!var.InExtendedCollection)
            {
                if (var.ID == -1)
                    return new IndexReference(ArrayBuilder, AssignWorkshopVariable(var.Name, variableIsGlobal));
                else
                {
                    WorkshopVariable workshopVariable = new WorkshopVariable(variableIsGlobal, var.ID, MetaElement.WorkshopNameFromCodeName(var.Name, NamesTaken(variableIsGlobal)));
                    variableList(variableIsGlobal).Add(workshopVariable);
                    return new IndexReference(ArrayBuilder, workshopVariable);
                }
            }
            else
            {
                int index = NextFreeExtended(variableIsGlobal);
                IndexReference reference = new IndexReference(ArrayBuilder, variableIsGlobal ? global : player, Element.Num(index));
                ExtendedVariableList(variableIsGlobal).Add(new ExtendedVariable(var.Name, reference, index));
                return reference;
            }
        }
    
        public void ToWorkshop(WorkshopBuilder builder)
        {
            if (globalLimitReached || playerLimitReached || extGlobalLimitReached || extPlayerLimitReached)
            {
                List<string> collectionLimitsReached = new List<string>();

                // Add names of the collections that exceed their variable limit.
                if (globalLimitReached) collectionLimitsReached.Add("global");
                if (playerLimitReached) collectionLimitsReached.Add("player");
                if (extGlobalLimitReached) collectionLimitsReached.Add("ext. global");
                if (extPlayerLimitReached) collectionLimitsReached.Add("ext. player");

                builder.AppendLine(string.Format(
                    "// The {0} reached the variable limit. Only a maximum of 128 variables and 1000 extended variables can be assigned.",
                    Extras.ListJoin("variable collection", collectionLimitsReached.ToArray())
                ));
                builder.AppendLine();
            }

            builder.AppendKeywordLine("variables");
            builder.AppendLine("{");
            builder.Indent();
            builder.AppendKeyword("global"); builder.Append(":"); builder.AppendLine();
            builder.Indent();
            WriteCollection(builder, variableList(true));
            builder.Outdent();

            builder.AppendKeyword("player"); builder.Append(":"); builder.AppendLine();
            builder.Indent();
            WriteCollection(builder, variableList(false));
            builder.Outdent();
            builder.Outdent();
            builder.AppendLine("}");

            bool anyExtendedGlobal = ExtendedVariableList(true).Any(v => v != null);
            bool anyExtendedPlayer = ExtendedVariableList(false).Any(v => v != null);
            if (anyExtendedGlobal || anyExtendedPlayer)
            {
                builder.AppendLine();
                builder.AppendLine($"// Extended collection variables:");

                foreach (var ex in ExtendedVariableList(true))
                    builder.AppendLine($"// global [{ex.Index}]: {ex.DebugName}");
                foreach (var ex in ExtendedVariableList(false))
                    builder.AppendLine($"// player [{ex.Index}]: {ex.DebugName}");
            }
        }
        private void WriteCollection(WorkshopBuilder builder, List<WorkshopVariable> collection)
        {
            foreach (var var in collection) {
				if(var.Name == "_extendedGlobalCollection")
					if(extendedGlobalVariables.Count == 0)
						continue;
				else if(var.Name == "_extendedPlayerCollection")
					if(extendedPlayerVariables.Count == 0)
						continue;
				else 
					builder.AppendLine(var.ID + ": " + var.Name);
			}
        }
    }

    public class ExtendedVariable
    {
        public string DebugName { get; }
        public IndexReference Reference { get; }
        public int Index { get; }

        public ExtendedVariable(string debugName, IndexReference reference, int index)
        {
            DebugName = debugName;
            Reference = reference;
            Index = index;
        }
    }
}
