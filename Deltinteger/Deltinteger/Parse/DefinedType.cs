using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType
    {
        public string Name { get; }
        public TypeKind TypeKind { get; }
        public InclassDefineNode[] DefinedVars { get; }
        public UserMethodNode[] MethodNodes { get; }
        public Constructor[] Constructors { get; }

        public DefinedType(TypeDefineNode node)
        {
            Name = node.Name;
            TypeKind = node.TypeKind;
            DefinedVars = node.DefinedVars;
            MethodNodes = node.Methods;

            Constructors = new Constructor[node.Constructors.Length];
            for (int i = 0; i < Constructors.Length; i++)
                Constructors[i] = new Constructor(node.Constructors[i]);
        }

        public ScopeGroup GetRootScope(IndexedVar var, ParsingData parseData)
        {
            ScopeGroup typeScope = new ScopeGroup(parseData.VarCollection);
            typeScope.This = var;

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                IndexedVar newVar = var.CreateChild(typeScope, DefinedVars[i].VariableName, new int[] { i });
                if (DefinedVars[i].Type != null)
                    newVar.Type = parseData.GetDefinedType(DefinedVars[i].Type, DefinedVars[i].Range);
                newVar.AccessLevel = DefinedVars[i].AccessLevel;
                typeScope.In(newVar);
            }

            for (int i = 0; i < MethodNodes.Length; i++)
            {
                UserMethod method = new UserMethod(typeScope, MethodNodes[i]);
                method.AccessLevel = MethodNodes[i].AccessLevel;
                typeScope.In(method);
            }

            return typeScope;
        }
    }

    public class Constructor
    {
        public AccessLevel AccessLevel { get; }
        public BlockNode BlockNode { get; }
        public ParameterBase[] Parameters { get; }

        public Constructor(ConstructorNode constructorNode)
        {
            AccessLevel = constructorNode.AccessLevel;
            BlockNode = constructorNode.BlockNode;
            
            Parameters = new ParameterBase[constructorNode.Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = new Parameter(constructorNode.Parameters[i], Elements.ValueType.Any, null);
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