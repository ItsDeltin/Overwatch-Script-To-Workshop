using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Models;
using Antlr4.Runtime;
using Deltin.Deltinteger.Images;

namespace Deltin.Deltinteger.Parse
{
    public class VarCollection
    {
        private const bool REUSE_VARIABLES = false;

        private Variable Global { get; }
        private Variable Player { get; }

        public WorkshopArrayBuilder WorkshopArrayBuilder { get; }

        public VarCollection(Variable global, Variable player, Variable builder)
        {
            Global = global;
            Player = player;

            IndexedVar tempArrayBuilderVar = AssignVar(null, "Multidimensional Array Builder", true, null);
            WorkshopArrayBuilder = new WorkshopArrayBuilder(builder, tempArrayBuilderVar);
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
                GlobalCollection[var.CollectionIndex] = var;
            else
                PlayerCollection[var.CollectionIndex] = var;
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

                if (GlobalCollection[var.CollectionIndex] == null)
                    throw new Exception();

                GlobalCollection[var.CollectionIndex] = null;
            }
            else
            {
                if (!PlayerCollection.Contains(var))
                    return;

                if (PlayerCollection[var.CollectionIndex] == null)
                    throw new Exception();

                PlayerCollection[var.CollectionIndex] = null;
            }
        }

        public IndexedVar AssignVar(ScopeGroup scope, string name, bool isGlobal, Node node)
        {
            IndexedVar var;

            if (node == null)
                name = Constants.INTERNAL_ELEMENT + name;

            int collectionIndex = Assign(isGlobal);
            
            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, GetUseVar(isGlobal), Element.IntToElement(collectionIndex), WorkshopArrayBuilder, node);
            else
                var = new RecursiveVar(scope, name, isGlobal, GetUseVar(isGlobal), Element.IntToElement(collectionIndex), WorkshopArrayBuilder, node);
            
            var.CollectionIndex = collectionIndex;
            
            Set(isGlobal, var);
            AddVar(var);
            return var;
        }

        public IndexedVar AssignVar(ScopeGroup scope, string name, bool isGlobal, Variable variable, int[] index, Node node)
        {
            IndexedVar var;

            if (scope == null || !scope.Recursive)
                var = new IndexedVar  (scope, name, isGlobal, variable, Element.IntToElement(index), WorkshopArrayBuilder, node);
            else
                var = new RecursiveVar(scope, name, isGlobal, variable, Element.IntToElement(index), WorkshopArrayBuilder, node);

            AddVar(var);
            return var;
        }

        private void AddVar(Var var)
        {
            if (!AllVars.Contains(var))
                AllVars.Add(var);
        }

        private Variable GetUseVar(bool isGlobal)
        {
            return isGlobal ? Global : Player;
        }

        public readonly List<Var> AllVars = new List<Var>();

        private readonly IndexedVar[] GlobalCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];
        private readonly IndexedVar[] PlayerCollection = new IndexedVar[Constants.MAX_ARRAY_LENGTH];
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
        public WorkshopArrayBuilder ArrayBuilder { get; set; }
        public bool IsGlobal { get; }
        public Variable Variable { get; }
        public Element[] Index { get; }
        public bool UsesIndex { get; }
        public int CollectionIndex { get; set; } = -1;
        public Element DefaultTarget { get; set; } = new V_EventPlayer();

        private readonly IWorkshopTree VariableAsWorkshop; 

        public IndexedVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, Element[] index, WorkshopArrayBuilder arrayBuilder, Node node)
            : base (name, scopeGroup, node)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            VariableAsWorkshop = EnumData.GetEnumValue(Variable);
            Index = index;
            UsesIndex = index != null && index.Length > 0;
            this.ArrayBuilder = arrayBuilder;
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
            return WorkshopArrayBuilder.GetVariable(IsGlobal, targetPlayer, Variable, Index);
        }

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return ArrayBuilder.SetVariable(value, IsGlobal, targetPlayer, Variable, ArrayBuilder<Element>.Build(Index, setAtIndex));
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
            return new IndexedVar(scope, name, IsGlobal, Variable, ArrayBuilder<Element>.Build(Index, index), ArrayBuilder, node);
        }

        public override string ToString()
        {
            return 
            (IsGlobal ? "global" : "player") + " "
            + Variable + (UsesIndex ? 
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
        public Variable Variable { get; }
        public Element[] Index { get; }

        private ElementOrigin(bool isGlobal, Element player, Variable variable, Element[] index)
        {
            IsGlobal = isGlobal;
            Player = player;
            Variable = variable;
            Index = index;
        }

        public IndexedVar OriginVar(VarCollection varCollection, ScopeGroup scope, string name)
        {
            return new IndexedVar(scope, name, IsGlobal, Variable, Index, varCollection.WorkshopArrayBuilder, null);
        }

        public static ElementOrigin GetElementOrigin(Element element)
        {
            bool isGlobal = false;
            Element player = null;
            Variable variable = Variable.A;

            Element checking = element;
            List<Element> index = new List<Element>();
            while (checking != null)
            {
                if (checking is V_GlobalVariable)
                {
                    isGlobal = true;
                    player = null;
                    variable = (Variable)((EnumMember)checking.ParameterValues[0]).Value;
                    checking = null;
                }
                else if (checking is V_PlayerVariable)
                {
                    isGlobal = false;
                    player = (Element)checking.ParameterValues[0];
                    variable = (Variable)((EnumMember)checking.ParameterValues[1]).Value;
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
        public RecursiveVar(ScopeGroup scopeGroup, string name, bool isGlobal, Variable variable, Element[] index, WorkshopArrayBuilder arrayBuilder, Node node)
            : base (scopeGroup, name, isGlobal, variable, index, arrayBuilder, node)
        {
        }

        protected override Element Get(Element targetPlayer = null)
        {
            return Element.Part<V_LastOf>(base.Get(targetPlayer));
        }

        public override Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] setAtIndex)
        {
            return base.SetVariable(value, targetPlayer, 
                ArrayBuilder<Element>.Build(
                    Element.Part<V_Subtract>(
                        Element.Part<V_CountOf>(base.Get(targetPlayer)),
                        new V_Number(1)
                    ),
                    setAtIndex
                )
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
                    Element.Part<V_Subtract>(Element.Part<V_CountOf>(get), new V_Number(1))
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

    public class ImageVar : Var
    {
        public EffectImage Image { get; }

        public ImageVar(string name, ScopeGroup scope, Node node, EffectImage image) : base(name, scope, node)
        {
            Image = image;
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            throw new NotImplementedException();
        }

        override public bool Gettable() => false;
        override public bool Settable() => false;
    }

    public class ModelVar : Var
    {
        public Model Model { get; }

        public ModelVar(string name, ScopeGroup scope, Node node, Model model) : base(name, scope, node)
        {
            Model = model;
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            throw new NotImplementedException();
        }

        override public bool Gettable() => false;
        override public bool Settable() => false;
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
        private Variable Constructor { get; }
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
            //for (int i = index.Length - 1; i >= 0; i--)
            for (int i = 0; i < index.Length; i++)
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