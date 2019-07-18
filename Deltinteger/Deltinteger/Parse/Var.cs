using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class VarCollection
    {
        private const bool REUSE_VARIABLES = false;

        public Variable UseVar = Variable.A;

        private readonly WorkshopArrayBuilder WorkshopArrayBuilder;

        public VarCollection()
        {
            IndexedVar tempArrayBuilderVar = AssignVar(null, "Multidimensional Array Builder", true);
            WorkshopArrayBuilder = new WorkshopArrayBuilder(Variable.B, tempArrayBuilderVar);
            tempArrayBuilderVar.ArrayBuilder = WorkshopArrayBuilder;
        }

        public int Assign(bool isGlobal)
        {
            if (isGlobal)
            {
                int index = Array.IndexOf(GlobalCollection, null);

                if (index == -1)
                    throw new Exception();
                
                return index;
            }
            else
            {
                int index = Array.IndexOf(PlayerCollection, null);

                if (index == -1)
                    throw new Exception();
                
                return index;
            }
        }

        private void Set(bool isGlobal, IndexedVar var)
        {
            if (var.Index.Length != 1)
                throw new Exception();

            if (isGlobal)
                GlobalCollection[var.Index[0]] = var;
            else
                PlayerCollection[var.Index[0]] = var;
        }

        public void Free(IndexedVar var)
        {
            # pragma warning disable
            if (!REUSE_VARIABLES)
                return;
            
            if (var.IsGlobal)
            #pragma warning restore
            {
                if (!GlobalCollection.Contains(var))
                    return;

                if (GlobalCollection[var.Index[0]] == null)
                    throw new Exception();

                GlobalCollection[var.Index[0]] = null;
            }
            else
            {
                if (!PlayerCollection.Contains(var))
                    return;

                if (PlayerCollection[var.Index[0]] == null)
                    throw new Exception();

                PlayerCollection[var.Index[0]] = null;
            }
        }

        public IndexedVar AssignVar(ScopeGroup scope, string name, bool isGlobal)
        {
            IndexedVar var;
            
            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, Constants.INTERNAL_ELEMENT + name, isGlobal, UseVar, new int[] { Assign(isGlobal) }, WorkshopArrayBuilder);
            else
                var = new RecursiveVar(scope, Constants.INTERNAL_ELEMENT + name, isGlobal, UseVar, new int[] { Assign(isGlobal) }, WorkshopArrayBuilder);
            
            Set(isGlobal, var);
            AddVar(var);
            return var;
        }

        public IndexedVar AssignDefinedVar(ScopeGroup scope, bool isGlobal, string name, Node node)
        {
            IndexedVar var;

            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, UseVar, new int[] { Assign(isGlobal) }, WorkshopArrayBuilder, node);
            else
                var = new RecursiveVar(scope, name, isGlobal, UseVar, new int[] { Assign(isGlobal) }, WorkshopArrayBuilder, node);
            
            Set(isGlobal, var);
            AddVar(var);
            return var;
        }

        public IndexedVar AssignDefinedVar(ScopeGroup scope, bool isGlobal, string name, Variable variable, int[] index, Node node)
        {
            IndexedVar var;

            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, variable, index, WorkshopArrayBuilder, node);
            else
                var = new RecursiveVar(scope, name, isGlobal, variable, index, WorkshopArrayBuilder, node);

            AddVar(var);
            return var;
        }

        public ElementReferenceVar AssignElementReferenceVar(ScopeGroup scope, string name, Node node, Element reference)
        {
            ElementReferenceVar var = new ElementReferenceVar(name, scope, node, reference);

            AddVar(var);
            return var; 
        }

        private void AddVar(Var var)
        {
            if (!AllVars.Contains(var))
                AllVars.Add(var);
        }

        public readonly List<Var> AllVars = new List<Var>();

        private readonly IndexedVar[] GlobalCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];
        private readonly IndexedVar[] PlayerCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];
    }

    public abstract class Var
    {
        public string Name { get; }
        public ScopeGroup Scope { get; private set; }
        public bool IsDefinedVar { get; }
        public Node Node { get; }

        public Var(string name)
        {
            Name = name;
        }

        public Var(string name, ScopeGroup scope, Node node = null) : this (name)
        {
            Scope = scope;
            Node = node;
            IsDefinedVar = node != null;

            scope?./* we're */ In(this) /* together! */;
        }

        public abstract Element GetVariable(Element targetPlayer = null);

        public override string ToString()
        {
            return Name;
        }
    }

    public class IndexedVar : Var
    {
        public bool IsGlobal { get; }
        public Variable Variable { get; }
        public int[] Index { get; }
        public bool UsesIndex { get; }

        public WorkshopArrayBuilder ArrayBuilder { get; set; }

        private readonly IWorkshopTree VariableAsWorkshop; 

        public IndexedVar(ScopeGroup scope, string name, bool isGlobal, Variable variable, int[] index, WorkshopArrayBuilder arrayBuilder) : base (name, scope)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = index != null && index.Length > 0;
            this.ArrayBuilder = arrayBuilder;
        }

        public IndexedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int[] index, WorkshopArrayBuilder arrayBuilder, Node node)
            : base (name, scopeGroup, node)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = index != null && index.Length > 0;
            this.ArrayBuilder = arrayBuilder;
        }

        public override Element GetVariable(Element targetPlayer = null)
        {
            return WorkshopArrayBuilder.GetVariable(IsGlobal, targetPlayer, Variable, IntToElement(Index));
        }

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return ArrayBuilder.SetVariable(value, IsGlobal, targetPlayer, Variable, ArrayBuilder<Element>.Build(IntToElement(Index), setAtIndex));
        }
        
        public virtual Element[] InScope(Element initialValue, Element targetPlayer = null)
        {
            if (initialValue != null)
                return SetVariable(initialValue, targetPlayer);
            return null;
        }

        public virtual Element[] OutOfScope(Element targetPlayer = null)
        {
            return null;
        }

        public override string ToString()
        {
            return 
            (IsGlobal ? "global" : "player") + " "
            + Variable + (UsesIndex ? $"[{string.Join(", ", Index)}]" : "") + " "
            + (AdditionalToStringInfo != null ? AdditionalToStringInfo + " " : "")
            + Name;
        }

        protected virtual string AdditionalToStringInfo { get; } = null;

        protected static V_Number[] IntToElement(params int[] numbers)
        {
            V_Number[] elements = new V_Number[numbers?.Length ?? 0];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new V_Number(numbers[i]);

            return elements;
        }
    }

    public class RecursiveVar : IndexedVar
    {
        public RecursiveVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int[] index, WorkshopArrayBuilder arrayBuilder, Node node)
            : base (scopeGroup, name, isGlobal, variable, index, arrayBuilder, node)
        {
        }

        public RecursiveVar(ScopeGroup scope, string name, bool isGlobal, Variable variable, int[] index, WorkshopArrayBuilder arrayBuilder)
            : base (scope, name, isGlobal, variable, index, arrayBuilder)
        {
        }

        public override Element GetVariable(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.GetVariable(targetPlayer));
        }

        public override Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return base.SetVariable(value, targetPlayer, 
                ArrayBuilder<Element>.Build(
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(base.GetVariable(targetPlayer)),
                        new V_Number(1)
                    ),
                    setAtIndex
                )
            );
        }

        public override Element[] InScope(Element initialValue, Element targetPlayer = null)
        {
            return base.SetVariable(initialValue, targetPlayer, Element.Part<V_CountOf>(base.GetVariable(targetPlayer)));
        }

        public override Element[] OutOfScope(Element targetPlayer = null)
        {
            Element get = base.GetVariable(targetPlayer);

            return base.SetVariable(
                Element.Part<V_ArraySlice>(
                    get,
                    new V_Number(0),
                    Element.Part<V_Subtract>(Element.Part<V_CountOf>(get), new V_Number(1))
                ),
                targetPlayer
            );
        }

        protected override string AdditionalToStringInfo { get; } = "RECURSIVE";

        public Element DebugStack(Element targetPlayer = null)
        {
            return base.GetVariable(targetPlayer);
        }
    }

    public class ElementReferenceVar : Var
    {
        public Element Reference { get; set; }

        public ElementReferenceVar(string name, ScopeGroup scope, Node node, Element reference) : base (name, scope, node)
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

            return Reference;
        }

        public override string ToString()
        {
            return "element reference : " + Name;
        }
    }

    public class VarRef : IWorkshopTree
    {
        public IndexedVar Var { get; }
        public Element Target { get; }

        public VarRef(IndexedVar var, Element target)
        {
            Var = var;
            Target = target;
        }

        public string ToWorkshop()
        {
            throw new NotImplementedException();
        }

        public void DebugPrint(Log log, int depth)
        {
            throw new NotImplementedException();
        }

        public double ServerLoadWeight()
        {
            throw new NotImplementedException();
        }
    }

    public class WorkshopArrayBuilder
    {
        Variable Constructor;
        IndexedVar Store;

        public WorkshopArrayBuilder(Variable constructor, IndexedVar store)
        {
            Constructor = constructor;
            Store = store;
        }

        public Element[] SetVariable(Element value, bool isGlobal, Element targetPlayer, Variable variable, params Element[] index)
        {
            if (index == null || index.Length == 0)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariable>(              EnumData.GetEnumValue(variable), value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable), value) };
            }

            if (index.Length == 1)
            {
                if (isGlobal)
                    return new Element[] { Element.Part<A_SetGlobalVariableAtIndex>(              EnumData.GetEnumValue(variable), index[0], value) };
                else
                    return new Element[] { Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, EnumData.GetEnumValue(variable), index[0], value) };
            }

            List<Element> actions = new List<Element>();

            Element root = GetRoot(isGlobal, targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), isGlobal, targetPlayer, Constructor)
            );

            // Set the value in the array.
            actions.AddRange(
                SetVariable(value, isGlobal, targetPlayer, Constructor, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    Store.SetVariable(GetRoot(isGlobal, targetPlayer, Constructor), targetPlayer)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(array, isGlobal, targetPlayer, Constructor)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(Store.GetVariable(targetPlayer), isGlobal, targetPlayer, Constructor, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(GetRoot(isGlobal, targetPlayer, Constructor), isGlobal, targetPlayer, variable, index[0])
            );
            return actions.ToArray();
        }

        public static Element GetVariable(bool isGlobal, Element targetPlayer, Variable variable, params Element[] index)
        {
            Element element = GetRoot(isGlobal, targetPlayer, variable);
            for (int i = index.Length - 1; i >= 0; i--)
                element = Element.Part<V_ValueInArray>(element, index[i]);
            return element;
        }

        private static Element ValueInArrayPath(Element array, Element[] index)
        {
            if (index.Length == 0)
                return array;
            
            if (index.Length == 1)
                return Element.Part<V_ValueInArray>(array, index[0]);
            
            return Element.Part<V_ValueInArray>(ValueInArrayPath(array, index.Take(index.Length - 1).ToArray()), index.Last());
        }
        
        private static Element GetRoot(bool isGlobal, Element targetPlayer, Variable variable)
        {
            if (isGlobal)
                return Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(variable));
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable));
        }
    }
}