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
            if (ConstructorNodes.Length != 0)
            {
                Constructors = new Constructor[ConstructorNodes.Length];
                for (int i = 0; i < Constructors.Length; i++)
                {
                    if (ConstructorNodes[i].Name != Name)
                        throw SyntaxErrorException.ConstructorName(ConstructorNodes[i].Location);
                    Constructors[i] = new Constructor(parser, ConstructorNodes[i]);
                }
            }
            else
            {
                Constructors = new Constructor[] 
                {
                    new Constructor(AccessLevel.Public, new Parameter[0], null)
                };
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
            if (constructor == null)
                throw SyntaxErrorException.NotAConstructor(TypeKind, Name, node.Parameters.Length, node.Location);
            
            if (context.MethodStackNotRecursive.Contains(constructor))
                throw new SyntaxErrorException("Constructors cannot be recursive.", node.Location);
            context.MethodStackNotRecursive.Add(constructor);
            
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
            context.MethodStackNotRecursive.Remove(constructor);
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
            context.ParserData.SetupClasses();

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
                Element.Part<V_Subtract>(Element.Part<V_CountOf>(context.ParserData.ClassArray.GetVariable()), new V_Number(1))
            );

            context.Actions.AddRange(index.SetVariable(firstFree));

            context.Actions.AddRange(
                index.SetVariable(
                    Element.TernaryConditional(
                        // If the index equals -1, use the length of the class array instead.
                        new V_Compare(index.GetVariable(), Operators.Equal, new V_Number(-1)),
                        Element.Part<V_CountOf>(context.ParserData.ClassArray.GetVariable()),
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
            IndexedVar store = context.ParserData.ClassArray.CreateChild(scope, Name + " root", new Element[] { index.GetVariable() }, null);
            store.Index[0].SupportedType = store;
            store.Type = this;

            ScopeGroup typeScope = GetRootScope(store, context.ParserData);

            SetupNew(getter, scope, store, typeScope, context, node);

            return index.GetVariable();
        }

        override protected IndexedVar GetRoot(IndexedVar req, ParsingData context, Element target)
        {
            if (req.Name == Name + " root") return req;
            return context.ClassArray.CreateChild(null, Name + " root", new Element[] { req.GetVariable(target) }, null);
        }

        public static void Delete(Element index, TranslateRule context)
        {
            context.Actions.AddRange(
                context.ParserData.ClassArray.SetVariable(new V_Null(), null, index)
            );

            context.Actions.AddRange(context.ParserData.ClassIndexes.SetVariable(
                Element.Part<V_RemoveFromArray>(
                    context.ParserData.ClassIndexes.GetVariable(),
                    index
                )
            ));
        }

        override public void GetSource(TranslateRule context, Element element, Location location)
        {
            IndexedVar supportedType = context.ParserData.ClassArray.CreateChild(null, Name + " root", new Element[] { element }, null);
            supportedType.Type = this;
            element.SupportedType = supportedType;
        }
    }

    public class Constructor : ICallable
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

        public Constructor(AccessLevel accessLevel, ParameterBase[] parameters, BlockNode block)
        {
            AccessLevel = accessLevel;
            Parameters = parameters;
            BlockNode = block;
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

    [CustomMethod("ClassMemoryRemaining", CustomMethodType.Value)]
    public class ClassMemoryRemaining : CustomMethodBase
    {
        override protected MethodResult Get()
        {
            Element result = Element.Part<V_Subtract>(
                new V_Number(Constants.MAX_ARRAY_LENGTH),
                Element.Part<V_CountOf>(TranslateContext.ParserData.ClassIndexes.GetVariable())
            );
            return new MethodResult(null, result);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Gets the remaining number of classes that can be created.");
        }
    }

    [CustomMethod("ClassMemoryCreated", CustomMethodType.Value)]
    public class ClassMemoryCreated : CustomMethodBase
    {
        override protected MethodResult Get()
        {
            Element result = Element.Part<V_CountOf>(TranslateContext.ParserData.ClassIndexes.GetVariable());
            return new MethodResult(null, result);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Gets the number of classes that were created.");
        }
    }

    [CustomMethod("ClassMemoryPercentage", CustomMethodType.Value)]
    public class ClassMemoryPercentage : CustomMethodBase
    {
        override protected MethodResult Get()
        {
            Element result = Element.Part<V_Multiply>(
                Element.Part<V_Divide>(
                    new V_Number(Constants.MAX_ARRAY_LENGTH),
                    Element.Part<V_CountOf>(TranslateContext.ParserData.ClassIndexes.GetVariable())
                ),
                new V_Number(100)
            );
            return new MethodResult(null, result);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Gets the percentage of class memory taken.");
        }
    }
}