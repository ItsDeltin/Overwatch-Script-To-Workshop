using System;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ElementData : Attribute
    {
        // No value type == action
        public ElementData(string elementName)
        {
            IsValue = false;
            ElementName = elementName;
        }

        // Value type == value
        public ElementData(string elementName, ValueType elementType)
        {
            IsValue = true;
            ElementName = elementName;
            ValueType = elementType;
        }

        public string ElementName { get; private set; }

        public bool IsValue { get; private set; }
        public ValueType ValueType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class HideElement : Attribute {}

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RestrictedAttribute : Attribute
    {
        public RestrictedCallType Type { get; }

        public RestrictedAttribute(RestrictedCallType type)
        {
            Type = type;
        }
    }
}