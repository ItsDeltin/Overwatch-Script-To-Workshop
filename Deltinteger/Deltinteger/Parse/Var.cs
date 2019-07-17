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
            if (isGlobal)
                GlobalCollection[var.Index] = var;
            else
                PlayerCollection[var.Index] = var;
        }

        public void Free(IndexedVar var)
        {
            # pragma warning disable
            if (!REUSE_VARIABLES)
                return;
            
            if (var.IsGlobal)
            #pragma warning restore
            {
                if (GlobalCollection[var.Index] == null)
                    throw new Exception();

                GlobalCollection[var.Index] = null;
            }
            else
            {
                if (PlayerCollection[var.Index] == null)
                    throw new Exception();

                PlayerCollection[var.Index] = null;
            }
        }

        public IndexedVar AssignVar(ScopeGroup scope, string name, bool isGlobal)
        {
            IndexedVar var;
            
            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, Constants.INTERNAL_ELEMENT + name, isGlobal, UseVar, Assign(isGlobal));
            else
                var = new RecursiveVar(scope, Constants.INTERNAL_ELEMENT + name, isGlobal, UseVar, Assign(isGlobal));
            
            Set(isGlobal, var);
            AddVar(var);
            return var;
        }

        public IndexedVar AssignDefinedVar(ScopeGroup scope, bool isGlobal, string name, Node node)
        {
            IndexedVar var;

            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, UseVar, Assign(isGlobal), node);
            else
                var = new RecursiveVar(scope, name, isGlobal, UseVar, Assign(isGlobal), node);
            
            Set(isGlobal, var);
            AddVar(var);
            return var;
        }

        public IndexedVar AssignDefinedVar(ScopeGroup scope, bool isGlobal, string name, Variable variable, int index, Node node)
        {
            IndexedVar var;

            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, variable, index, node);
            else
                var = new RecursiveVar(scope, name, isGlobal, variable, index, node);

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
        public int Index { get; }
        public bool UsesIndex { get; }

        private readonly IWorkshopTree VariableAsWorkshop; 

        public IndexedVar(ScopeGroup scope, string name, bool isGlobal, Variable variable, int index) : base (name, scope)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = Index != -1;
        }

        public IndexedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Node node)
            : base (name, scopeGroup, node)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = Index != -1;
        }

        public override Element GetVariable(Element targetPlayer = null)
        {
            if (UsesIndex)
                return Element.Part<V_ValueInArray>(GetRoot(targetPlayer), new V_Number(Index));
            else
                return GetRoot(targetPlayer);
        }

        private Element GetRoot(Element targetPlayer)
        {
            if (IsGlobal)
                return Element.Part<V_GlobalVariable>(VariableAsWorkshop);
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, VariableAsWorkshop);
        }

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (setAtIndex == null)
            {
                if (UsesIndex)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, new V_Number(Index), value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, new V_Number(Index), value);
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariable>(VariableAsWorkshop, value);
                    else
                        element = Element.Part<A_SetPlayerVariable>(targetPlayer, VariableAsWorkshop, value);
                }
            }
            else
            {
                if (UsesIndex)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, new V_Number(Index), 
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex), 
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), V_Number.LargeArbitraryNumber)));
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, new V_Number(Index),
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex),
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), V_Number.LargeArbitraryNumber)));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(VariableAsWorkshop, setAtIndex, value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, VariableAsWorkshop, setAtIndex, value);
                }
            }

            return new Element[] { element };
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
            + Variable + (UsesIndex ? $"[{Index}]" : "") + " "
            + (AdditionalToStringInfo != null ? AdditionalToStringInfo + " " : "")
            + Name;
        }

        protected virtual string AdditionalToStringInfo { get; } = null;
    }

    public class RecursiveVar : IndexedVar
    {
        private static readonly IWorkshopTree bAsWorkshop = EnumData.GetEnumValue(Variable.B); // TODO: Remove when multidimensional temp var can be set.

        public RecursiveVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Node node)
            : base (scopeGroup, name, isGlobal, variable, index, node)
        {
        }

        public RecursiveVar(ScopeGroup scope, string name, bool isGlobal, Variable variable, int index)
            : base (scope, name, isGlobal, variable, index)
        {
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.GetVariable(targetPlayer));
        }

        override public Element[] SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            return new Element[]
            {
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer)),

                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, 
                        Element.Part<V_Subtract>(Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), new V_Number(1)), value),
                
                base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer)[0]
            };
        }

        public override Element[] InScope(Element initialValue, Element targetPlayer = null)
        {
            return new Element[]
            {
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer)),

                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), initialValue ?? Element.DefaultElement),

                base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer)[0]
            };
        }

        public override Element[] OutOfScope(Element targetPlayer = null)
        {
            Element get = base.GetVariable(targetPlayer);

            return base.SetVariable(
                Element.Part<V_ArraySlice>(
                    get,
                    new V_Number(0),
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(get), new V_Number(1)
                    )
                ), targetPlayer
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

    public class WorkshopDArray
    {
        public static Element[] SetVariable(Element value, Element targetPlayer, Variable variable, params V_Number[] index)
        {
            bool isGlobal = targetPlayer == null;

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

            Element root = GetRoot(targetPlayer, variable);

            // index is 2 or greater
            int dimensions = index.Length - 1;

            // Get the last array in the index path and copy it to variable B.
            actions.AddRange(
                SetVariable(ValueInArrayPath(root, index.Take(index.Length - 1).ToArray()), targetPlayer, Variable.B)
            );

            // Set the value in the array.
            actions.AddRange(
                SetVariable(value, targetPlayer, Variable.B, index.Last())
            );

            // Reconstruct the multidimensional array.
            for (int i = 1; i < dimensions; i++)
            {
                // Copy the array to the C variable
                actions.AddRange(
                    SetVariable(GetRoot(targetPlayer, Variable.B), targetPlayer, Variable.C)
                );

                // Copy the next array dimension
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());

                actions.AddRange(
                    SetVariable(array, targetPlayer, Variable.B)
                );

                // Copy back the variable at C to the correct index
                actions.AddRange(
                    SetVariable(GetRoot(targetPlayer, Variable.C), targetPlayer, Variable.B, index[i])
                );
            }
            // Set the final variable using Set At Index.
            actions.AddRange(
                SetVariable(GetRoot(targetPlayer, Variable.B), targetPlayer, variable, index[0])
            );
            return actions.ToArray();
        }

        private static Element ValueInArrayPath(Element array, V_Number[] index)
        {
            if (index.Length == 0)
                return array;
            
            if (index.Length == 1)
                return Element.Part<V_ValueInArray>(array, index[0]);
            
            return Element.Part<V_ValueInArray>(ValueInArrayPath(array, index.Take(index.Length - 1).ToArray()), index.Last());
        }
        
        private static Element GetRoot(Element targetPlayer, Variable variable)
        {
            if (targetPlayer == null)
                return Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(variable));
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, EnumData.GetEnumValue(variable));
        }
    }
}