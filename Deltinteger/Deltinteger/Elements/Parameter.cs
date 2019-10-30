using System;
using System.Linq;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ParameterBase : Attribute, ILanguageServerInfo
    {
        public string Name { get; private set; }

        protected ParameterBase(string name)
        {
            Name = name;
        }

        public abstract IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node);
        public virtual IWorkshopTree GetDefault()
        {
            return null;
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract string GetLabel(bool markdown);

        public static string ParameterGroupToString(ParameterBase[] parameters, bool markdown)
        {
            return string.Join(", ", parameters.Select(p => p.GetLabel(markdown)));
        }
    }

    class Parameter : ParameterBase
    {
        public ValueType ReturnType { get; private set; }
        public Type DefaultType { get; private set; } // The value that the variable is set to use by default

        public Parameter(string name, ValueType returnType, Type defaultType) : base (name)
        {
            ReturnType = returnType;
            DefaultType = defaultType;
        }

        public override IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node)
        {
            return context.ParseExpression(getter, scope, node);
        }
        public override IWorkshopTree GetDefault()
        {
            if (DefaultType == null)
                return null;
            return (IWorkshopTree)Activator.CreateInstance(DefaultType);
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return ReturnType.ToString() + ": " + Name;
            else
                return "**" + ReturnType.ToString() + "**: " + Name;
        }
    }

    class TypeParameter : ParameterBase
    {
        public DefinedType Type { get; }

        public TypeParameter(string name, DefinedType type) : base(name)
        {
            Type = type;
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return Type.Name + ": " + Name;
            else
                return "**" + Type.Name + "**: " + Name;
        }

        public override IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node)
        {
            Element result = context.ParseExpression(getter, scope, node);

            if (result.SupportedType?.Type != Type)
                throw SyntaxErrorException.InvalidValueType(Type.Name, result.SupportedType?.Type.Name ?? "any", node.Location);
            
            return result;
        }
    }

    class EnumParameter : ParameterBase
    {
        public Type EnumType { get; private set; }
        public EnumData EnumData { get; private set; }

        public EnumParameter(string name, Type enumType) : base (name)
        {
            EnumType = enumType;
            EnumData = EnumData.GetEnum(enumType);
        }

        public override IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node)
        {
            // Parse the enum
            if (node is EnumNode)
            {
                EnumNode enumNode = (EnumNode)node;

                if (enumNode.EnumMember.Enum != EnumData)
                    throw SyntaxErrorException.IncorrectEnumType(EnumData.CodeName, enumNode.EnumMember.Enum.CodeName, node.Location);

                return (IWorkshopTree)EnumData.ToElement(enumNode.EnumMember) ?? (IWorkshopTree)enumNode.EnumMember;
            }
            else if (node is VariableNode)
            {
                Var var = scope.GetVar(getter, ((VariableNode)node).Name, null);
                
                if (var is ElementReferenceVar && ((ElementReferenceVar)var).Reference is EnumMember)
                {
                    EnumMember member = (EnumMember)((ElementReferenceVar)var).Reference;
                    if (member.Enum != EnumData)
                        throw SyntaxErrorException.IncorrectEnumType(EnumData.CodeName, member.Enum.CodeName, node.Location);

                    return (IWorkshopTree)EnumData.ToElement(member) ?? (IWorkshopTree)member;
                }
                else
                    throw SyntaxErrorException.ExpectedEnumGotValue(EnumData.CodeName, node.Location);
            }
            else
                throw SyntaxErrorException.ExpectedEnumGotValue(EnumData.CodeName, node.Location);
        }
        public override IWorkshopTree GetDefault()
        {
            return EnumData.Members[0];
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return EnumData.CodeName + ": " + Name;
            else
                return "**" + EnumData.CodeName + "**: " + Name;
        }
    }

    class VarRefParameter : ParameterBase 
    {
        public VarRefParameter(string name) : base(name) {}

        public override IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node)
        {
            var varData = new TranslateRule.ParseExpressionTree(context, getter, scope, node);
                    
            // A VarRef parameter must be a variable
            if (varData.ResultingVariable == null)
                throw SyntaxErrorException.ExpectedVariable(node.Location);
            
            return new VarRef(varData.ResultingVariable, varData.VariableIndex, varData.Target);
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return "ref: " + Name;
            else
                return "**ref**: " + Name;
        }
    }

    class ConstantParameter : ParameterBase // where T: IConvertible
    {
        public Type Type { get; }
        public object Default { get; }

        public ConstantParameter(string name, Type type, object def = null) : base(name)
        {
            Type = type;
            Default = def;
        }

        public override IWorkshopTree Parse(TranslateRule context, ScopeGroup getter, ScopeGroup scope, Node node)
        {
            if (node is IConstantSupport == false)
                throw new SyntaxErrorException("Parameter must be a " + Type.Name + " constant.", node.Location);
            
            object value = ((IConstantSupport)node).GetValue();

            if (!IsValid(value))
                throw new SyntaxErrorException("Parameter must be a " + Type.Name + ".", node.Location);

            return new ConstantObject(value);
        }

        public override string GetLabel(bool markdown)
        {
            string label;
            if (!markdown)
                label = "const " + Type.Name + ": " + Name;
            else
                label = "**const " + Type.Name + "**: " + Name;
            
            if (Default != null)
                label += " = " + Default.ToString();

            return label;
        }

        public bool IsValid(object obj)
        {
            return obj.GetType() == Type;
        }

        public override IWorkshopTree GetDefault()
        {
            if (Default == null)
                return null;
            return new ConstantObject(Default);
        }
    }

    class ConstantObject : IWorkshopTree
    {
        public object Value { get; }

        public ConstantObject(object value)
        {
            Value = value;
        }

        public string ToWorkshop()
        {
            throw new NotImplementedException();
        }
        public void DebugPrint(Log log, int depth)
        {
            throw new NotImplementedException();
        }
    }
}