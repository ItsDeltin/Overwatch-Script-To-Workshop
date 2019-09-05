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
        public UserMethodNode[] MethodNodes { get; }
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

        public ScopeGroup GetRootScope(IndexedVar var, ParsingData parseData)
        {
            ScopeGroup typeScope = new ScopeGroup(parseData.VarCollection);
            IndexedVar root = GetRoot(var, parseData);
            typeScope.This = root;

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                IndexedVar newVar = root.CreateChild(typeScope, DefinedVars[i].VariableName, Element.IntToElement(i), DefinedVars[i]);
                if (DefinedVars[i].Type != null)
                    newVar.Type = parseData.GetDefinedType(DefinedVars[i].Type, DefinedVars[i].Location);
                newVar.AccessLevel = DefinedVars[i].AccessLevel;
            }

            for (int i = 0; i < MethodNodes.Length; i++)
            {
                UserMethod method = new UserMethod(typeScope, MethodNodes[i]);
                method.RegisterParameters(parseData);
                method.AccessLevel = MethodNodes[i].AccessLevel;
            }

            return typeScope;
        }

        abstract protected IndexedVar GetRoot(IndexedVar req, ParsingData context);
        
        public Element New(CreateObjectNode node, ScopeGroup scope, TranslateRule context)
        {
            IndexedVar store = GetStore(scope, context);
            store.Type = this;

            ScopeGroup typeScope = GetRootScope(store, context.ParserData);

            // Set the default variables in the struct
            for (int i = 0; i < DefinedVars.Length; i++)
            {
                if (DefinedVars[i].Value != null)
                    context.Actions.AddRange(
                        store.SetVariable(context.ParseExpression(typeScope, DefinedVars[i].Value), null, new V_Number(i))
                    );
            }

            Constructor constructor = Constructors.FirstOrDefault(c => c.Parameters.Length == node.Parameters.Length);
            if (constructor == null && !(node.Parameters.Length == 0 && Constructors.Length == 0))
                throw SyntaxErrorException.NotAConstructor(TypeKind, Name, node.Parameters.Length, node.Location);
            
            if (constructor != null)
            {
                ScopeGroup constructorScope = typeScope.Child();

                IWorkshopTree[] parameters = context.ParseParameters(
                    constructorScope,
                    constructor.Parameters,
                    node.Parameters,
                    node.TypeName,
                    node.Location
                );

                context.AssignParameterVariables(constructorScope, constructor.Parameters, parameters, node);
                context.ParseBlock(constructorScope, constructor.BlockNode, true, null);
                constructorScope.Out();
            }

            return ReferenceReturn(store);
        }

        abstract protected IndexedVar GetStore(ScopeGroup scope, TranslateRule context);

        abstract protected Element ReferenceReturn(IndexedVar var);

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

        override protected IndexedVar GetRoot(IndexedVar req, ParsingData context)
        {
            return req;
        }

        override protected IndexedVar GetStore(ScopeGroup scope, TranslateRule context)
        {
            return context.VarCollection.AssignVar(scope, Name + " store", context.IsGlobal, null);
        }

        override protected Element ReferenceReturn(IndexedVar var)
        {
            return var.GetVariable();
        }
    }

    public class DefinedClass : DefinedType
    {
        override public TypeKind TypeKind { get; } = TypeKind.Class;

        public DefinedClass(TypeDefineNode definedType) : base(definedType) {}

        override protected IndexedVar GetRoot(IndexedVar req, ParsingData context)
        {
            return new IndexedVar(
                null,
                Name + " root",
                true,
                Variable.C,
                new Element[] { req.GetVariable() },
                context.VarCollection.WorkshopArrayBuilder,
                null
            );
        }

        override protected IndexedVar GetStore(ScopeGroup scope, TranslateRule context)
        {
            IndexedVar store = new IndexedVar(
                scope,
                Name + " store",
                true,
                Variable.C,
                new Element[] { context.ParserData.ClassIndex.GetVariable() },
                context.VarCollection.WorkshopArrayBuilder,
                null
            );
            store.Index[0].SupportedType = store;
            context.Actions.AddRange(
                context.ParserData.ClassIndex.SetVariable(
                    Element.Part<V_Add>(
                        context.ParserData.ClassIndex.GetVariable(),
                        new V_Number(1)
                    )
                )
            );
            return store;
        }

        override protected Element ReferenceReturn(IndexedVar var)
        {
            return var.Index[0];
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