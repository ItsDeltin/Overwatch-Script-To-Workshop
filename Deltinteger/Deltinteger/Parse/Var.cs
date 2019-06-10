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

        public DefinedVar AssignDefinedVar(ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            return new DefinedVar(scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range);
        }

        public ParameterVar AssignParameterVar(List<Element> actions, ScopeGroup scopeGroup, bool isGlobal, string name, Range range)
        {
            return new ParameterVar(actions, scopeGroup, name, isGlobal, UseVar, Assign(isGlobal), range);
        }
    }

    public class Var
    {
        public bool IsGlobal { get; protected set; }
        public Variable Variable { get; protected set; }
        public int Index { get; protected set; }

        public Var(bool isGlobal, Variable variable, int index)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            Index = index;
        }

        public virtual Element GetVariable(Element targetPlayer = null)
        {
            return GetSub(targetPlayer ?? new V_EventPlayer());
        }

        private Element GetSub(Element targetPlayer)
        {
            if (Index != -1)
                return Element.Part<V_ValueInArray>(GetRoot(targetPlayer), new V_Number(Index));
            else
                return GetRoot(targetPlayer);
        }

        private Element GetRoot(Element targetPlayer)
        {
            if (IsGlobal)
                return Element.Part<V_GlobalVariable>(Variable);
            else
                return Element.Part<V_PlayerVariable>(targetPlayer, Variable);
        }

        public virtual Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (setAtIndex == null)
            {
                if (Index != -1)
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
                if (Index != -1)
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

        public DefinedVar(ScopeGroup scopeGroup, DefinedNode node, VarCollection varCollection)
            : base(node.IsGlobal, varCollection.UseVar, node.UseIndex ?? varCollection.Assign(node.IsGlobal))
        {
            if (scopeGroup.IsVar(node.VariableName))
            throw new SyntaxErrorException($"The variable {node.VariableName} was already defined.", node.Range);
                //diagnostics.Add(new Diagnostic($"The variable {node.VariableName} was already defined.", node.Range));

            Name = node.VariableName;

            scopeGroup.In(this);
        }

        public DefinedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range)
            : base (isGlobal, variable, index)
        {
            if (scopeGroup.IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", range);
                //diagnostics.Add(new Diagnostic($"The variable {name} was already defined.", range) { severity = Diagnostic.Error });

            Name = name;

            scopeGroup.In(this);
        }
    }

    public class ParameterVar : DefinedVar
    {
        private readonly List<Element> Actions = new List<Element>();

        public ParameterVar(List<Element> actions, ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, int index, Range range)
            : base (scopeGroup, name, isGlobal, variable, index, range)
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
                Element.Part<A_SetGlobalVariable>(Variable.B, base.GetVariable(targetPlayer))
                );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(Variable.B, 
                    Element.Part<V_Subtract>(Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(Variable.B)), new V_Number(1)), value)
            );

            return base.SetVariable(Element.Part<V_GlobalVariable>(Variable.B), targetPlayer);
        }

        public Element Push(Element value, Element targetPlayer = null)
        {
            Actions.Add(
                Element.Part<A_SetGlobalVariable>(Variable.B, base.GetVariable(targetPlayer))
            );
            
            Actions.Add(
                Element.Part<A_SetGlobalVariableAtIndex>(Variable.B, Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(Variable.B)), value)
            );

            return base.SetVariable(Element.Part<V_GlobalVariable>(Variable.B), targetPlayer);

            //return SetVariable(Element.Part<V_Append>(base.GetVariable(targetPlayer), value), targetPlayer);
        }

        public Element Pop(Element targetPlayer = null)
        {
            Element get = base.GetVariable(targetPlayer);
            return SetVariable(
                Element.Part<V_ArraySlice>(
                    get, 
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(get), new V_Number(1)
                    )
                ), targetPlayer
            );
        }
    }
}