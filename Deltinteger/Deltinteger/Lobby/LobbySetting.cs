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

        public LobbySetting(string name)
        {
            Name = name;
        }

        public abstract RootSchema GetSchema();
    }

    /// <summary>Enum lobby setting.</summary>
    class SelectValue : LobbySetting
    {
        public string[] Values { get; }

        public SelectValue(string name, params string[] values) : base(name)
        {
            Values = values;
        }

        public override RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema();
            schema.Type = SchemaObjectType.String;
            schema.Enum = Values;
            schema.Default = Values[0];
            return schema;
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

        public override RootSchema GetSchema()
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

        public override RootSchema GetSchema()
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