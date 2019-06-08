using System;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class Var
    {
        public static Variable Global { get; private set; }
        public static Variable Player { get; private set; }

        private static int NextFreeGlobalIndex { get; set; }
        private static int NextFreePlayerIndex { get; set; }

        public static void Setup(Variable global, Variable player)
        {
            Global = global;
            Player = player;
        }

        public static int Assign(bool isGlobal)
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

        private static Variable GetVar(bool isGlobal)
        {
            if (isGlobal)
                return Global;
            else
                return Player;
        }

        public static Var AssignVar(bool isGlobal)
        {
            return new Var(isGlobal, GetVar(isGlobal), Assign(isGlobal));
        }

        public static DefinedVar AssignDefinedVar(ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            return new DefinedVar(scopeGroup, name, isGlobal, GetVar(isGlobal), Assign(isGlobal), range);
        }



        public bool IsGlobal { get; protected set; }
        public Variable Variable { get; protected set; }

        public bool IsInArray { get; protected set; }
        public int Index { get; protected set; }

        public Var(bool isGlobal, Variable variable, int index)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            Index = index;
            IsInArray = index != -1;
        }

        protected Var()
        {}

        public Element GetVariable(Element targetPlayer = null, Element getAiIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (getAiIndex == null)
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable), new V_Number(Index));
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(Index));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<V_GlobalVariable>(Variable);
                    else
                        element = Element.Part<V_PlayerVariable>(targetPlayer, Variable);
                }
            }
            else
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable)), new V_Number(Index));
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable)), new V_Number(Index));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable), getAiIndex);
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), getAiIndex);
                }
            }

            return element;
        }

        public Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (setAtIndex == null)
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index), value);
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariable>(Variable, value);
                    else
                        element = Element.Part<A_SetPlayerVariable>(targetPlayer, Variable, value);
                }
            }
            else
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), 
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex), 
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index),
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex),
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, setAtIndex, value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, setAtIndex, value);
                }
            }

            return element;

        }
    }

    public class DefinedVar : Var
    {
        public string Name { get; protected set; }

        public DefinedVar(ScopeGroup scopeGroup, DefinedNode node)
        {
            IsGlobal = node.IsGlobal;

            if (scopeGroup.IsVar(node.VariableName))
                throw new SyntaxErrorException($"The variable {node.VariableName} was already defined.", node.Range);

            Name = node.VariableName;

            if (node.UseVar == null)
            {
                Index = Var.Assign(IsGlobal);

                if (IsGlobal)
                    Variable = Var.Global;
                else
                    Variable = Var.Player;

                IsInArray = true;
            }
            else
            {
                Variable = (Variable)node.UseVar;
                if (node.UseIndex != null)
                {
                    IsInArray = true;
                    Index = (int)node.UseIndex;
                }
            }

            scopeGroup.In(this);
        }

        public DefinedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range)
        {
            if (scopeGroup.IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", range);

            Name = name;
            IsGlobal = isGlobal;
            Variable = variable;

            if (index != -1)
            {
                IsInArray = true;
                Index = index;
            }

            scopeGroup.In(this);
        }
    }
}