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

        public ScopeGroup GetRootScope(Element root, VarCollection varCollection)
        {
            ScopeGroup methodScope = new ScopeGroup(varCollection);

            for (int i = 0; i < DefinedVars.Length; i++)
            {
                Element reference = Element.Part<V_ValueInArray>(root, new V_Number(i));
                ElementReferenceVar var = new ElementReferenceVar(DefinedVars[i].VariableName, methodScope, null, reference);
                methodScope.In(var);
            }

            for (int i = 0; i < MethodNodes.Length; i++)
            {
                UserMethod method = new UserMethod(methodScope, MethodNodes[i]);
                methodScope.In(method);
            }

            return methodScope;
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