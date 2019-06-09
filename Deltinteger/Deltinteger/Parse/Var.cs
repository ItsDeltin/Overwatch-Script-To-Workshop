using System;
using System.Collections.Generic;
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

        public Var AssignVar(bool isGlobal)
        {
            return new Var(isGlobal, UseVar, Assign(isGlobal));
        }

        public DefinedVar AssignDefinedVar(ScopeGroup scopeGroup, bool isGlobal, string name, Range range, List<Diagnostic> diagnostics)
        {
            DefinedVar var = new DefinedVar(scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range, diagnostics);
            return var;
        }
    }

    public class Var
    {
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

        public DefinedVar(ScopeGroup scopeGroup, DefinedNode node, List<Diagnostic> diagnostics, VarCollection varCollection)
            : base(node.IsGlobal, varCollection.UseVar, node.UseIndex ?? varCollection.Assign(node.IsGlobal))
        {
            IsGlobal = node.IsGlobal;

            if (scopeGroup.IsVar(node.VariableName))
                diagnostics.Add(new Diagnostic($"The variable {node.VariableName} was already defined.", node.Range));

            Name = node.VariableName;

            scopeGroup.In(this);
        }

        public DefinedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range, List<Diagnostic> diagnostics)
            : base (isGlobal, variable, index)
        {
            if (scopeGroup.IsVar(name))
                diagnostics.Add(new Diagnostic($"The variable {name} was already defined.", range) { severity = Diagnostic.Error });

            Name = name;

            scopeGroup.In(this);
        }
    }
}