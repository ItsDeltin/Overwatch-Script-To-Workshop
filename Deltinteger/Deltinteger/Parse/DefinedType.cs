using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType
    {
        public string Name { get; }
        public TypeKind DefineType { get; }
        public InclassDefineNode[] DefinedVars { get; }
        public ConstructorNode[] Constructors { get; }
        public UserMethodNode[] MethodNodes { get; }

        public DefinedType(TypeDefineNode node)
        {
            Name = node.Name;
            DefineType = node.DefineType;
            DefinedVars = node.DefinedVars;
            Constructors = node.Constructors;
            MethodNodes = node.Methods;
        }

        public ScopeGroup GetRootScope(IndexedVar var, ParsingData parseData)
        {
            ScopeGroup typeScope = new ScopeGroup(parseData.VarCollection);
            typeScope.This = var;

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                IndexedVar newVar = var.CreateChild(typeScope, DefinedVars[i].VariableName, new int[] { i });
                newVar.Type = parseData.GetDefinedType(DefinedVars[i].Type, DefinedVars[i].Range);
                typeScope.In(newVar);
            }

            for (int i = 0; i < MethodNodes.Length; i++)
            {
                UserMethod method = new UserMethod(typeScope, MethodNodes[i]);
                typeScope.In(method);
            }

            return typeScope;
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