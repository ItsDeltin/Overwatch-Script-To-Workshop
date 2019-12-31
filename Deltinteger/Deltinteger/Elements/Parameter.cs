using System;
using System.Linq;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ParameterBase : Attribute
    {
        public string Name { get; }

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
    }

    class Parameter : ParameterBase
    {
        public ValueType ReturnType { get; }
        public Type DefaultType { get; }

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
    }

    class EnumParameter : ParameterBase
    {
        public Type EnumType { get; }
        public EnumData EnumData { get; }

        public EnumParameter(string name, Type enumType) : base (name)
        {
            EnumType = enumType;
            EnumData = EnumData.GetEnum(enumType);
        }

        public override IWorkshopTree GetDefault()
        {
            return EnumData.Members[0];
        }
    }

    class VarRefParameter : ParameterBase 
    {
        public bool IsGlobal { get; }

        public VarRefParameter(string name, bool isGlobal) : base(name)
        {
            IsGlobal = isGlobal;
        }
    }
}