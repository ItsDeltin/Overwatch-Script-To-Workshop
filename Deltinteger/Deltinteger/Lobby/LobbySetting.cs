using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Deltin.Deltinteger.Lobby
{
    public abstract class LobbySetting
    {
        public string Name { get; }

        public LobbySetting(string name)
        {
            Name = name;
        }

        public abstract string[] Options();
    }

    /// <summary>Enum lobby setting.</summary>
    class SelectValue : LobbySetting
    {
        public string[] Values { get; }

        public SelectValue(string name, params string[] values) : base(name)
        {
            Values = values;
        }

        public override string[] Options() => Values;
    }

    /// <summary>Boolean lobby setting.</summary>
    class SwitchValue : LobbySetting
    {
        public SwitchValue(string name) : base(name) {}

        public override string[] Options() => new string[] { "true", "false" };
    }

    /// <summary>Number range lobby setting.</summary>
    class RangeValue : LobbySetting
    {
        public double Min { get; }
        public double Max { get; }
        public double Default { get; }

        public RangeValue(string name, double min, double max, double defaultValue) : base(name)
        {
            Min = min;
            Max = max;
            Default = defaultValue;
        }

        public override string[] Options() => new string[] { Default.ToString() };
    }
}