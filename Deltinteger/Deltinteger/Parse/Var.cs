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
        private int NextFreeGlobalIndex = 0;
        private int NextFreePlayerIndex = 0;

        public Variable UseVar = Variable.A;

        public int Assign(bool isGlobal)
        {
            if (isGlobal)
            {
                int index = NextFreeGlobalIndex;
                NextFreeGlobalIndex++;
                return index;
            }
            else
            {
                int index = NextFreePlayerIndex;
                NextFreePlayerIndex++;
                return index;
            }
        }

        public Var AssignVar(string name, bool isGlobal)
        {
            return new Var(Constants.INTERNAL_ELEMENT + name, isGlobal, UseVar, Assign(isGlobal), this);
        }

        public Var AssignDefinedVar(ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            return new Var(scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range, this);
        }

        public RecursiveVar AssignRecursiveVar(List<Element> actions, ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            return new RecursiveVar(actions, scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range, this);
        }

        public readonly List<Var> AllVars = new List<Var>();
    }

    public class Var
    {
        public string Name { get; }
        public bool IsGlobal { get; }
        public Variable Variable { get; }
        public int Index { get; }
        public bool UsesIndex { get; }
        public VarCollection VarCollection { get; }
        public bool IsDefinedVar { get; } = false;
        public Range DefinedRange { get; } = null;

        private readonly IWorkshopTree VariableAsWorkshop; 

        public Var(string name, bool isGlobal, Variable variable, int index, VarCollection varCollection)
        {
            Name = name;
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = Index != -1;
            VarCollection = varCollection;
            varCollection.AllVars.Add(this);
        }

        public Var(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range, VarCollection varCollection)
            : this (name, isGlobal, variable, index, varCollection)
        {
            if (scopeGroup.IsVar(name))
                throw SyntaxErrorException.AlreadyDefined(name, range);

            scopeGroup./* we're */ In(this) /* together! */;

            IsDefinedVar = true;
            DefinedRange = range;
        }

        public virtual Element GetVariable(Element targetPlayer = null)
        {
            return GetSub(targetPlayer ?? new V_EventPlayer());
        }

        private Element GetSub(Element targetPlayer)
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

        public virtual Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
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

            return element;

        }

        public override string ToString()
        {
            return (IsGlobal ? "global" : "player") + " " + Variable + (UsesIndex ? $"[{Index}]" : "") + " " + Name;
        }
    }

    public class RecursiveVar : Var
    {
        private static readonly IWorkshopTree bAsWorkshop = EnumData.GetEnumValue(Variable.B); // TODO: Remove when multidimensional temp var can be set.

        private readonly List<Element> Actions = new List<Element>();

        public RecursiveVar(List<Element> actions, ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range, VarCollection varCollection)
            : base (scopeGroup, name, isGlobal, variable, index, range, varCollection)
        {
            Actions = actions;
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.GetVariable(targetPlayer));
        }

        override public Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Actions.Add(
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer))
                );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, 
                    Element.Part<V_Subtract>(Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), new V_Number(1)), value)
            );

            return base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer);
        }

        public void Push(Element value, Element targetPlayer = null)
        {
            Actions.Add(
                Element.Part<A_SetGlobalVariable>(bAsWorkshop, base.GetVariable(targetPlayer))
            );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(bAsWorkshop, Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(bAsWorkshop)), value)
            );

            Actions.Add(
                base.SetVariable(Element.Part<V_GlobalVariable>(bAsWorkshop), targetPlayer)
            );
        }

        public void Pop(Element targetPlayer = null)
        {
            Element get = base.GetVariable(targetPlayer);
            Actions.Add(base.SetVariable(
                Element.Part<V_ArraySlice>(
                    get,
                    new V_Number(0),
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(get), new V_Number(1)
                    )
                ), targetPlayer
            ));
        }

        public Element DebugStack(Element targetPlayer = null)
        {
            return base.GetVariable(targetPlayer);
        }
    }

    public class VarRef : IWorkshopTree
    {
        public Var Var { get; }
        public Element Target { get; }

        public VarRef(Var var, Element target)
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
                Element array = ValueInArrayPath(root, index.Take(dimensions - i).ToArray());
                
                actions.AddRange(
                    SetVariable(GetRoot(targetPlayer, Variable.B), targetPlayer, variable)
                );
                actions.AddRange(
                    SetVariable(array, targetPlayer, Variable.B)
                );
                actions.AddRange(
                    SetVariable(GetRoot(targetPlayer, variable), targetPlayer, Variable.B, index[i])
                );
            }
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