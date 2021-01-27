using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Deltin.Deltinteger.Elements
{
    public class ElementRoot
    {
        public static ElementRoot Instance { get; } = Get(File.ReadAllText(Path.Combine(Program.ExeFolder, "Elements.json")));

        [JsonProperty("values")]
        public ElementJsonValue[] Values;

        [JsonProperty("actions")]
        public ElementJsonAction[] Actions;

        [JsonProperty("enumerators")]
        public ElementEnum[] Enumerators;

        public ElementBaseJson GetFunction(string name)
        {
            foreach (var value in Values) if (value.Name == name) return value;
            foreach (var action in Actions) if (action.Name == name) return action;
            throw new KeyNotFoundException("The function '" + name + "' was not found.");
        }

        public bool TryGetFunction(string name, out ElementBaseJson function)
        {
            foreach (var value in Values) if (value.Name == name)
            {
                function = value;
                return true;
            }
            foreach (var action in Actions) if (action.Name == name)
            {
                function = action;
                return true;
            }
            function = null;
            return false;
        }

        public ElementEnum GetEnum(string name) => Enumerators.FirstOrDefault(e => e.Name == name) ?? throw new KeyNotFoundException("The enum '" + name + "' was not found.");
        public bool TryGetEnum(string name, out ElementEnum enumerator)
        {
            enumerator = Enumerators.FirstOrDefault(e => e.Name == name);
            return enumerator != null;
        }
        public ElementEnumMember GetEnumValue(string enumName, string alias) => GetEnum(enumName).GetMemberFromAlias(alias);
        public ElementEnumMember GetEnumValueFromWorkshop(string enumName, string workshopName) => GetEnum(enumName).GetMemberFromWorkshop(workshopName);

        public static ElementRoot Get(string json)
            => JsonConvert.DeserializeObject<ElementRoot>(json, new EnumeratorConverter(), new ParameterConverter());
        
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings() {
                // Set converters
                Converters = new JsonConverter[] {
                    new EnumeratorConverter(),
                    new ParameterConverter()
                },
                // Ignore null
                NullValueHandling = NullValueHandling.Ignore
            });
    }

    public abstract class ElementBaseJson
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("documentation")]
        public string Documentation;

        [JsonProperty("parameters")]
        public ElementParameter[] Parameters;

        [JsonProperty("hidden")]
        public bool IsHidden;

        [JsonProperty("alias")]
        public string Alias;

        [JsonProperty("restricted")]
        public string Restricted;

        public bool ShouldSerializeIsHidden() => IsHidden;

        public override string ToString() => Name + (Parameters == null ? "" : "(" + string.Join(", ", Parameters.Select(v => v.ToString())) + ")");

        public string CodeName() => Alias ?? Name.Replace(" ", "").Replace("-", "");
    }

    public class ElementJsonValue : ElementBaseJson
    {
        [JsonProperty("type")]
        public string ReturnType;
    }

    public class ElementJsonAction : ElementBaseJson
    {
        [JsonProperty("return-value")]
        public string ReturnValue;

        [JsonProperty("indent")]
        public string Indentation;
    }

    public class ElementParameter
    {
        [JsonIgnore]
        public string Name;

        [JsonProperty("documentation")]
        public string Documentation;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("defaultValue")]
        public object DefaultValue;

        [JsonProperty("var-ref-global")]
        public bool? VariableReferenceIsGlobal;

        [JsonIgnore]
        public bool HasDefaultValue;

        public override string ToString() => Type + " " + Name;

        public bool IsVariableReference => VariableReferenceIsGlobal != null;

        public IWorkshopTree GetDefaultValue()
        {
            // No default value.
            if (!HasDefaultValue) throw new Exception("Parameter has no default value.");

            // Enumerator default value
            if (ElementRoot.Instance.TryGetEnum(Type, out ElementEnum enumerator)) return enumerator.GetMemberFromWorkshop((string)DefaultValue);
            // Null
            else if (DefaultValue == null) return Element.Null();
            // Boolean
            else if (DefaultValue is bool boolean) return boolean ? Element.True() : Element.False();
            // Number
            else if (DefaultValue is double d) return Element.Num(d);
            else if (DefaultValue is Int32 i32) return Element.Num(i32);
            else if (DefaultValue is Int64 i64) return Element.Num(i64);
            else if (DefaultValue is float f) return Element.Num(f);
            // String
            else if (DefaultValue is string str)
            {
                // Localized string default
                if (str == "@") return new StringElement(Constants.Strings[0], true);
                // Function
                else if (ElementRoot.Instance.TryGetFunction(str, out var function)) return Element.Part(function);
            }

            throw new Exception("Could not get default value for parameter '" + Name + "'. Value: '" + DefaultValue.ToString() + "'");
        }
    }

    public class ElementEnum
    {
        public string Name;
        public ElementEnumMember[] Members;
        public bool Hidden;


        public override string ToString() => Name + " [" + Members.Length + " members]";

        public ElementEnumMember GetMemberFromAlias(string name) => Members.FirstOrDefault(m => m.CodeName() == name) ?? throw new KeyNotFoundException("The enum member '" + name + "' was not found.");
        public ElementEnumMember GetMemberFromWorkshop(string name) => Members.FirstOrDefault(m => m.Name == name) ?? throw new KeyNotFoundException("The enum member '" + name + "' was not found.");

        public bool ConvertableToElement() => new string[] { "Map", "GameMode", "Team", "Hero", "Button", "Color" }.Contains(Name);
    }

    public class ElementEnumMember : IWorkshopTree
    {
        public string Name;
        public string Alias;
        public ElementEnum Enum;

        public override string ToString() => Name;

        public void ToWorkshop(WorkshopBuilder builder, ToWorkshopContext context) => builder.AppendKeyword(Name);

        public string CodeName() => Alias ?? Name.Replace(" ", "");
        public string DecompileName() => Name.Replace("(", "").Replace(")", "");

        public bool EqualTo(IWorkshopTree other) => other is ElementEnumMember enumMember && Enum == enumMember.Enum && Name == enumMember.Name;

        public Element ToElement()
        {
            switch (Enum.Name)
            {
                case "Map": return Element.Part("Map", this);
                case "GameMode": return Element.Part("Game Mode", this);
                case "Team": return Element.Part("Team", this);
                case "Hero": return Element.Part("Hero", this);
                case "Button": return Element.Part("Button", this);
                case "Color": return Element.Part("Color", this);
                default: throw new NotImplementedException(Enum.Name + " cannot be converted to element.");
            }
        }

        public Operation GetOperation()
        {
            if (Enum.Name != "Operation") throw new Exception("Enum is not 'Operation'.");
            return System.Enum.Parse<Operation>(CodeName());
        }

        public static ElementEnumMember Event(RuleEvent eventType) => ElementRoot.Instance.GetEnumValue("Event", eventType.ToString());
        public static ElementEnumMember Team(Team team) => ElementRoot.Instance.GetEnumValue("Team", team.ToString());
        public static ElementEnumMember Player(PlayerSelector player) => ElementRoot.Instance.GetEnumValue("Player", player.ToString());

        public T ToEnum<T>() where T: Enum
        {
            var values = System.Enum.GetValues(typeof(T));
            foreach (var value in values)
                if (value.ToString() == CodeName())
                    return (T)value;

            throw new Exception("Failed to get enum value from '" + CodeName() + "' to '" + typeof(T).Name + "'");
        }
    }
}