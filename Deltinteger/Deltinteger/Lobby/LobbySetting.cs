using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Deltin.Deltinteger.Lobby
{
    public abstract class LobbySetting
    {
        public string Name { get; }
        public string ReferenceName { get; set; }
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

        public virtual string[] AdditionalKeywords() => null;
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

        public override string[] AdditionalKeywords() => Values;
    }

    /// <summary>Boolean lobby setting.</summary>
    class SwitchValue : LobbySetting
    {
        public bool Default { get; }

        public SwitchValue(string name, bool defaultValue) : base(name)
        {
            Default = defaultValue;
        }

        protected override RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema();
            schema.Type = SchemaObjectType.Boolean;
            schema.Default = Default;
            return schema;
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
    }
}