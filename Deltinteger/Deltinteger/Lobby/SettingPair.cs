using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;

namespace Deltin.Deltinteger.Lobby
{
    public class SettingPair
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public SettingPair(string name, object value)
        {
            Name = name;
            Value = value;
        }

        ///<summary>Writes the workshop setting to a json writer.</summary>
        public void Write(JsonWriter writer)
        {
            writer.WritePropertyName(Name);
            writer.WriteValue(Value);
        }

        ///<summary>Writes the workshop setting to the workshop output as a custom setting.</summary>
        public void ToWorkshopCustom(WorkshopBuilder builder)
        {
            string value = Value.ToString();
            if (Value is bool boolean)
                value = boolean ? "On" : "Off";
            builder.AppendLine($"{Name}: {value}");
        }

        ///<summary>Writes the workshop setting to the workshop output as an official setting.</summary>
        public void ToWorkshop(WorkshopBuilder builder, IReadOnlyCollection<LobbySetting> allSettings)
        {
            // Get the related setting.
            LobbySetting relatedSetting = allSettings.FirstOrDefault(ls => ls.Name == Name);

            string name = relatedSetting.ResolveName(builder);
            string value = relatedSetting.GetValue(builder, Value);

            builder.AppendLine($"{name}: {value}");
        }

        ///<summary>Gets a value from a collection of settings by the setting's name.</summary>
        public static bool TryGetValue(SettingPair[] collection, string name, out object value)
        {
            foreach (var item in collection)
                if (item.Name == name)
                {
                    value = item.Value;
                    return true;
                }
            
            value = null;
            return false;
        }

        public static void ToWorkshop(SettingPair[] collection, WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            foreach (var item in collection)
                item.ToWorkshop(builder, allSettings);
        }
    }

    public class SettingPairCollection : IReadOnlyList<SettingPair>
    {
        private readonly IList<SettingPair> _list;

        public SettingPairCollection() => _list = new List<SettingPair>();
        public SettingPairCollection(IList<SettingPair> list) => _list = list;

        public SettingPair this[int index] => _list[index];
        public int Count => _list.Count;

        public IEnumerator<SettingPair> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public bool TryGetValue(string name, out object value)
        {
            foreach (var item in _list)
                if (item.Name == name)
                {
                    value = item.Value;
                    return true;
                }
            
            value = null;
            return false;
        }

        public void ToWorkshop(WorkshopBuilder builder, IReadOnlyCollection<LobbySetting> allSettings)
        {
            foreach (var item in _list)
                item.ToWorkshop(builder, allSettings);
        }
    }
}