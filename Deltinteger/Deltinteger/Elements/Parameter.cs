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

        public override IWorkshopTree GetDefault()
        {
            if (DefaultType == null)
                return null;
            return (IWorkshopTree)Activator.CreateInstance(DefaultType);
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return ReturnType.ToString() + " " + Name;
            else
                return "**" + ReturnType.ToString() + "** " + Name;
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
                return Type.Name + " " + Name;
            else
                return "**" + Type.Name + "** " + Name;
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

        public override IWorkshopTree GetDefault()
        {
            return EnumData.Members[0];
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return EnumData.CodeName + " " + Name;
            else
                return "**" + EnumData.CodeName + "** " + Name;
        }
    }

    class VarRefParameter : ParameterBase 
    {
        public VarRefParameter(string name) : base(name) {}

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return "ref " + Name;
            else
                return "**ref** " + Name;
        }
    }
}