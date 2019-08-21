using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType : ITypeRegister
    {
        public string Name { get; }
        public TypeKind TypeKind { get; }
        public InclassDefineNode[] DefinedVars { get; }
        public UserMethodNode[] MethodNodes { get; }
        public Constructor[] Constructors { get; private set; }
        private ConstructorNode[] ConstructorNodes { get; }

        public DefinedType(TypeDefineNode node)
        {
            if (EnumData.GetEnum(node.Name) != null)
                throw new SyntaxErrorException("A type cannot have the same name as a predefined enum in the Overwatch Workshop.", node.Location);

            Name = node.Name;
            TypeKind = node.TypeKind;
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
            typeScope.This = var;

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                IndexedVar newVar = var.CreateChild(typeScope, DefinedVars[i].VariableName, new int[] { i }, DefinedVars[i]);
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