using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby
{
    public abstract class LobbySetting
    {
        public string Name { get; }
        public string ReferenceName { get; set; }
        public SettingNameResolver TitleResolver { get; set; }
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

        protected abstract RootSchema GetSchema();

        public abstract string GetValue(WorkshopBuilder builder, object value);

        public string ResolveName(WorkshopBuilder builder)
        {
            if (TitleResolver == null) return builder.Translate(Name);
            else return TitleResolver.ResolveName(builder);
        }

        public virtual string[] AdditionalKeywords() => new string[0];

        public abstract void CheckValue(SettingValidation validation, JToken value);
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
                    validation.Error($"Expected one of the following values for property '{Name}': {string.Join(", ", Values.Select(v => "'" + v + "'"))}");
            }
            catch
            {
                validation.IncorrectType(Name, "string");
            }
        }
    }

    /// <summary>Boolean lobby setting.</summary>
    class SwitchValue : LobbySetting
    {
        public bool Default { get; }

        public SwitchValue(string name, bool defaultValue) : base(name)
        {
            Default = defaultValue;
        }

        public override string GetValue(WorkshopBuilder builder, object value) => builder.Translate((bool)value ? "On" : "Off");

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
    }

    /// <summary>Number range lobby setting.</summary>
    class RangeValue : LobbySetting
    {
        public double Min { get; }
        public double Max { get; }
        public double Default { get; }
        public bool Integer { get; }

        public RangeValue(string name, double min, double max, double defaultValue = 100) : base(name)
        {
            Min = min;
            Max = max;
            Default = defaultValue;
        }

        public override string GetValue(WorkshopBuilder builder, object value)
        {
            if (Integer) return value.ToString();
            else return value.ToString() + "%";
        }

        public RangeValue(bool integer, string name, double min, double max, double defaultValue = 100) : this(name, min, max, defaultValue)
        {
            Integer = integer;
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
                if (number < Min) validation.Error($"The property '{Name}' requires a number above {Min}, got {number}.");
                if (number > Max) validation.Error($"The property '{Name}' requires a number below {Max}, got {number}.");
            }
            catch
            {
                validation.IncorrectType(Name, "number");
            }
        }
    }
}