using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Deltin.Deltinteger.Decompiler.TextToElement;

namespace Deltin.Deltinteger.Lobby
{
    public abstract class LobbySetting
    {
        public string Name { get; }
        public string ReferenceName { get; set; }
        public SettingNameResolver TitleResolver { get; set; }
        public string Workshop => Name.Replace("(", "").Replace(")", "");
        private RootSchema Reference;

        public LobbySetting(string name)
        {
            Name = name;
            ReferenceName = Name;
        }

        public RootSchema GetSchema(SchemaGenerate generate)
        {
            if (Reference == null)
            {
                RootSchema definition = GetSchema();
                generate.Definitions.Add(ReferenceName, definition);

                Reference = new RootSchema();
                Reference.Ref = "#/definitions/" + ReferenceName;
            }

            return Reference;
        }

        /// <summary>The schema of the lobby setting.</summary>
        protected abstract RootSchema GetSchema();

        /// <summary>Adds the value to the workshop output.</summary>
        public abstract string GetValue(WorkshopBuilder builder, object value);

        /// <summary>Resolves the actual name of the setting that will be used in the workshop output.</summary>
        public string ResolveName(WorkshopBuilder builder)
        {
            if (TitleResolver == null) return builder.Translate(Name);
            else return TitleResolver.ResolveName(builder);
        }

        /// <summary>The keywords that the setting's value could be.</summary>
        public virtual string[] AdditionalKeywords() => new string[0];

        /// <summary>Validates the json value.</summary>
        public abstract void CheckValue(SettingValidation validation, JToken value);

        /// <summary>Decompiles the workshop value.</summary>
        public abstract bool Match(ConvertTextToElement parser, out object value);
    }

    /// <summary>Enum lobby setting.</summary>
    class SelectValue : LobbySetting
    {
        public string[] Values { get; }

        public SelectValue(string name, params string[] values) : base(name)
        {
            Values = values;
        }

        protected override RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema();
            schema.Type = SchemaObjectType.String;
            schema.Enum = Values;
            schema.Default = Values[0];
            return schema;
        }

        public override string GetValue(WorkshopBuilder builder, object value) => builder.Translate(value.ToString());

        public override string[] AdditionalKeywords() => Values;

        public override void CheckValue(SettingValidation validation, JToken value)
        {
            try
            {
                string str = value.ToObject<string>();
                if (!Values.Contains(str))
                    validation.Error($"Expected one of the following values for property '{Name}': {string.Join(", ", Values.Select(v => "'" + v + "'"))}", false);
            }
            catch
            {
                validation.IncorrectType(Name, "string");
            }
        }

        public override bool Match(ConvertTextToElement parser, out object value)
        {
            // Match the enumerator.
            foreach (string enumerator in Values)
                if (parser.Match(parser.Kw(enumerator), false))
                {
                    // Return true if it is found.
                    value = enumerator;
                    return true;
                }

            // The value was not found.
            value = null;
            return false;
        }

        public override string ToString() => "[Select] " + Name;
    }

    /// <summary>Boolean lobby setting.</summary>
    class SwitchValue : LobbySetting
    {
        public bool Default { get; }
        public SwitchType SwitchType { get; }

        public SwitchValue(string name, bool defaultValue, SwitchType switchType = SwitchType.OnOff) : base(name)
        {
            Default = defaultValue;
            SwitchType = switchType;
        }

        public override string GetValue(WorkshopBuilder builder, object value) => builder.Translate((bool)value ? EnabledKey() : DisabledKey());

        protected override RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema();
            schema.Type = SchemaObjectType.Boolean;
            schema.Default = Default;
            return schema;
        }

        public override void CheckValue(SettingValidation validation, JToken value)
        {
            if (value.ToObject<object>() is bool == false)
                validation.IncorrectType(Name, "boolean");
        }

        public override bool Match(ConvertTextToElement parser, out object value)
        {
            // Match enabled
            if (parser.Match(parser.Kw(EnabledKey()), false))
            {
                value = true;
                return true;
            }
            // Match disabled
            else if (parser.Match(parser.Kw(DisabledKey()), false))
            {
                value = false;
                return true;
            }

            // Unknown
            value = null;
            return false;
        }

        private string EnabledKey()
        {
            if (SwitchType == SwitchType.OnOff) return "On";
            else if (SwitchType == SwitchType.YesNo) return "Yes";
            else return "Enabled";
        }

        private string DisabledKey()
        {
            if (SwitchType == SwitchType.OnOff) return "Off";
            else if (SwitchType == SwitchType.YesNo) return "No";
            else return "Disabled";
        }

        public override string ToString() => "[Switch] " + Name;
    }

    /// <summary>Number range lobby setting.</summary>
    class RangeValue : LobbySetting
    {
        public double Min { get; }
        public double Max { get; }
        public double Default { get; }
        public bool Integer { get; }
        public bool Percentage { get; }

        public RangeValue(string name, double min, double max, double defaultValue = 100) : base(name)
        {
            Min = min;
            Max = max;
            Default = defaultValue;
        }

        public RangeValue(bool integer, bool percentage, string name, double min, double max, double defaultValue = 100) : this(name, min, max, defaultValue)
        {
            Integer = integer;
            Percentage = percentage;
        }

        public override string GetValue(WorkshopBuilder builder, object value)
        {
            if (!Percentage) return value.ToString();
            else return value.ToString() + "%";
        }

        protected override RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema();
            if (Integer) schema.Type = SchemaObjectType.Integer;
            else schema.Type = SchemaObjectType.Number;
            schema.Minimum = Min;
            schema.Maximum = Max;
            schema.Default = Default;
            return schema;
        }

        public override void CheckValue(SettingValidation validation, JToken value)
        {
            try
            {
                double number = value.ToObject<double>();
                if (number < Min) validation.Error($"The property '{Name}' requires a number above {Min}, got {number}.", false);
                if (number > Max) validation.Error($"The property '{Name}' requires a number below {Max}, got {number}.", false);
            }
            catch
            {
                validation.IncorrectType(Name, "number");
            }
        }

        public override bool Match(ConvertTextToElement parser, out object value)
        {
            // If the setting is an integer, match an integer.
            if (Integer)
            {
                if (parser.Integer(out int i))
                {
                    // Integer matched.
                    value = i;
                    return true;
                }
                else
                {
                    // Integer not matched.
                    value = null;
                    return false;
                }
            }
            else
            {
                // Match a double.
                if (parser.Double(out double d))
                {
                    parser.Match("%");
                    // Double matched.
                    value = d;
                    return true;
                }
                else
                {
                    // Double not matched.
                    value = null;
                    return false;
                }
            }
        }

        public static RangeValue NewPercentage(string name, double min, double max, double defaultValue = 100) => new RangeValue(false, true, name, min, max, defaultValue);
        public static RangeValue NewPercentage(string name, AbilityNameResolver title, double min, double max, double defaultValue = 100) => new RangeValue(false, true, name, min, max, defaultValue)
        {
            TitleResolver = title
        };

        public override string ToString() => "[Range] " + Name;
    }

    enum SwitchType
    {
        OnOff,
        YesNo,
        EnabledDisabled
    }
}
