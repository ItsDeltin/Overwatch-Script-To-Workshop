using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedType : ITypeRegister
    {
        public static DefinedType GetDefinedType(TypeDefineNode node)
        {
            if (node.TypeKind == TypeKind.Struct)
                return new DefinedStruct(node);
            else if (node.TypeKind == TypeKind.Class)
                return new DefinedClass(node);
            else
                throw new Exception();
        }

        public string Name { get; }
        public InclassDefineNode[] DefinedVars { get; }
        public UserMethodBase[] MethodNodes { get; }
        public Constructor[] Constructors { get; private set; }
        private ConstructorNode[] ConstructorNodes { get; }
        public abstract TypeKind TypeKind { get; }

        protected DefinedType(TypeDefineNode node)
        {
            if (EnumData.GetEnum(node.Name) != null)
                throw SyntaxErrorException.TypeNameConflict(node.Name, node.Location);

            Name = node.Name;
            DefinedVars = node.DefinedVars;
            MethodNodes = node.Methods;

            ConstructorNodes = node.Constructors;
        }

        public void RegisterParameters(ParsingData parser)
        {
            Constructors = new Constructor[ConstructorNodes.Length];
            for (int i = 0; i < Constructors.Length; i++)
            {
                if (ConstructorNodes[i].Name != Name)
                    throw SyntaxErrorException.ConstructorName(ConstructorNodes[i].Location);
                Constructors[i] = new Constructor(parser, ConstructorNodes[i]);
            }
        }

        public ScopeGroup GetRootScope(IndexedVar var, ParsingData parseData, Element target = null)
        {
            if (target == null) target = new V_EventPlayer();

            IndexedVar root = GetRoot(var, parseData, target);
            root.DefaultTarget = target;
            
            ScopeGroup typeScope = new ScopeGroup(parseData.VarCollection);
            typeScope.This = root;

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                IndexedVar newVar = root.CreateChild(typeScope, DefinedVars[i].VariableName, Element.IntToElement(i), DefinedVars[i]);
                newVar.DefaultTarget = target;
                if (DefinedVars[i].Type != null)
                    newVar.Type = parseData.GetDefinedType(DefinedVars[i].Type, DefinedVars[i].Location);
                newVar.AccessLevel = DefinedVars[i].AccessLevel;
            }

            for (int i = 0; i < MethodNodes.Length; i++)
            {
                UserMethod method = UserMethod.CreateUserMethod(typeScope, MethodNodes[i]);
                method.RegisterParameters(parseData);
                method.AccessLevel = MethodNodes[i].AccessLevel;
            }

            return typeScope;
        }

        abstract protected IndexedVar GetRoot(IndexedVar req, ParsingData context, Element target);
        
        abstract public Element New(CreateObjectNode node, ScopeGroup getter, ScopeGroup scope, TranslateRule context);

        protected void SetupNew(ScopeGroup getter, ScopeGroup scope, IndexedVar store, ScopeGroup typeScope, TranslateRule context, CreateObjectNode node)
        {
            // Set the default variables in the struct
            for (int i = 0; i < DefinedVars.Length; i++)
            {
                if (DefinedVars[i].Value != null)
                    context.Actions.AddRange(
                        store.SetVariable(context.ParseExpression(typeScope, typeScope, DefinedVars[i].Value), null, new V_Number(i))
                    );
            }

            Constructor constructor = Constructors.FirstOrDefault(c => c.Parameters.Length == node.Parameters.Length);
            if (constructor == null && !(node.Parameters.Length == 0 && Constructors.Length == 0))
                throw SyntaxErrorException.NotAConstructor(TypeKind, Name, node.Parameters.Length, node.Location);
            
            if (constructor != null)
            {
                ScopeGroup constructorScope = typeScope.Child();

                IWorkshopTree[] parameters = context.ParseParameters(
                    getter,
                    scope,
                    constructor.Parameters,
                    node.Parameters,
                    node.TypeName,
                    node.Location
                );

                context.AssignParameterVariables(constructorScope, constructor.Parameters, parameters, node);
                context.ParseBlock(typeScope, constructorScope, constructor.BlockNode, true, null);
                constructorScope.Out(context);
            }
        }

        abstract public void GetSource(TranslateRule context, Element element, Location location);

        public static CompletionItem[] CollectionCompletion(DefinedType[] definedTypes)
        {
            return definedTypes.Select(
                dt => new CompletionItem(dt.Name)
                {
                    kind = CompletionItem.Struct
                }
            ).ToArray();
        }
    }

    public class DefinedStruct : DefinedType
    {
        override public TypeKind TypeKind { get; } = TypeKind.Struct;

        public DefinedStruct(TypeDefineNode definedType) : base(definedType) {}

        override public Element New(CreateObjectNode node, ScopeGroup getter, ScopeGroup scope, TranslateRule context)
        {
            IndexedVar store = context.VarCollection.AssignVar(scope, Name + " store", context.IsGlobal, null);
            store.Type = this;
            ScopeGroup typeScope = GetRootScope(store, context.ParserData);

            SetupNew(getter, scope, store, typeScope, context, node);

            return store.GetVariable();
        }

        override protected IndexedVar GetRoot(IndexedVar req, ParsingData context, Element target)
        {
            return req;
        }

        override public void GetSource(TranslateRule context, Element element, Location location)
        {
            ElementOrigin origin = ElementOrigin.GetElementOrigin(element);

            if (origin == null)
                throw new SyntaxErrorException("Could not get the type source.", location);

            IndexedVar typeVar = origin.OriginVar(context.VarCollection, null, Name + " origin");
            typeVar.Type = this;
            element.SupportedType = typeVar;
        }
    }

    public class DefinedClass : DefinedType
    {
        override public TypeKind TypeKind { get; } = TypeKind.Class;

        public DefinedClass(TypeDefineNode definedType) : base(definedType) {}

        override public Element New(CreateObjectNode node, ScopeGroup getter, ScopeGroup scope, TranslateRule context)
        {
            // Get the index to store the class.
            IndexedVar index = context.VarCollection.AssignVar(scope, "New " + Name + " class index", context.IsGlobal, null); // Assigns the index variable.
            Element takenIndexes = context.ParserData.ClassIndexes.GetVariable();

            // Get an empty index in the class array to store the new class.
            Element firstFree = Element.Part<V_Subtract>(
                Element.Part<V_FirstOf>(
                    Element.Part<V_FilteredArray>(
                        // Sort the taken index array.
                        Element.Part<V_SortedArray>(takenIndexes, new V_ArrayElement()),
                        // Filter
                        Element.Part<V_And>(
                            // If the previous index was not taken, use that index.
                            Element.Part<V_Not>(Element.Part<V_ArrayContains>(
                                takenIndexes,
                                Element.Part<V_Subtract>(new V_ArrayElement(), new V_Number(1))
                            )),
                            // Make sure the index does not equal 0 so the resulting index is not -1.
                            new V_Compare(new V_ArrayElement(), Operators.NotEqual, new V_Number(0))
                        )
                    )
                ),
                new V_Number(1) // Subtract 1 to get the previous index
            );
            // If the taken index array has 0 elements, just use the length of the class array subtracted by 1.
            firstFree = Element.TernaryConditional(
                new V_Compare(Element.Part<V_CountOf>(takenIndexes), Operators.NotEqual, new V_Number(0)),
                firstFree,
                Element.Part<V_Subtract>(Element.Part<V_CountOf>(WorkshopArrayBuilder.GetVariable(true, null, Variable.C)), new V_Number(1))
            );

            context.Actions.AddRange(index.SetVariable(firstFree));

            context.Actions.AddRange(
                index.SetVariable(
                    Element.TernaryConditional(
                        // If the index equals -1, use the length of the class array instead.
                        new V_Compare(index.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(
                            WorkshopArrayBuilder.GetVariable(true, null, Variable.C)
                        ),
                        index.GetVariable()
                    )
                )
            );

            // Add the selected index to the taken indexes array.
            context.Actions.AddRange(
                context.ParserData.ClassIndexes.SetVariable(
                    Element.Part<V_Append>(
                        context.ParserData.ClassIndexes.GetVariable(),
                        index.GetVariable()
                    )
                )
            );

            // The direct reference to the class variable.
            IndexedVar store = new IndexedVar(
                scope,
                Name + " root",
                true,
                Variable.C,
                new Element[] { index.GetVariable() },
                context.VarCollection.WorkshopArrayBuilder,
                null
            );
            store.Index[0].SupportedType = store;
            store.Type = this;

            ScopeGroup typeScope = GetRootScope(store, context.ParserData);

            SetupNew(getter, scope, store, typeScope, context, node);

            return index.GetVariable();
        }

        override protected IndexedVar GetRoot(IndexedVar req, ParsingData context, Element target)
        {
            if (req.Name == Name + " root") return req;
            return new IndexedVar(
                null,
                Name + " root",
                true,
                Variable.C,
                new Element[] { req.GetVariable(target) },
                context.VarCollection.WorkshopArrayBuilder,
                null
            );
        }

        public static void Delete(Element index, TranslateRule context)
        {
            context.Actions.AddRange(context.VarCollection.WorkshopArrayBuilder.SetVariable(
                new V_Null(), true, null, Variable.C, index
            ));
            context.Actions.AddRange(context.ParserData.ClassIndexes.SetVariable(
                Element.Part<V_RemoveFromArray>(
                    context.ParserData.ClassIndexes.GetVariable(),
                    index
                )
            ));
        }

        override public void GetSource(TranslateRule context, Element element, Location location)
        {
            element.SupportedType = new IndexedVar(
                null,
                Name + " root",
                true,
                Variable.C,
                new Element[] { element },
                context.VarCollection.WorkshopArrayBuilder,
                null
            )
            {
                Type = this
            };
        }
    }

    public class Constructor
    {
        public AccessLevel AccessLevel { get; }
        public BlockNode BlockNode { get; }
        public ParameterBase[] Parameters { get; }

        public Constructor(ParsingData parser, ConstructorNode constructorNode)
        {
            AccessLevel = constructorNode.AccessLevel;
            BlockNode = constructorNode.BlockNode;
            
            Parameters = ParameterDefineNode.GetParameters(parser, constructorNode.Parameters);
        }
    }

    public enum TypeKind
    {
        Class,
        Struct
    }

    public enum AccessLevel
    {
        Public,
        Private
    }
}