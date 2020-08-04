using Newtonsoft.Json;

namespace Deltin.Deltinteger.Decompiler.Json
{
    public class JsonFunction
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("parameters")]
        public JsonParameter[] Parameters;
        [JsonProperty("type")]
        public JsonType ReturnType;
        [JsonProperty("block")]
        public JsonBlock Block;
    }

    public class JsonParameter
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("type")]
        public JsonType Type;
        [JsonProperty("ref")]
        public bool Ref;
    }

    public class JsonCallFunction
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("values")]
        public JsonExpression[] Expressions;
    }

    public class JsonType
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("generics")]
        public JsonType[] Generics;
        [JsonProperty("array")]
        public int ArrayCount;
    }

    public class JsonBlock
    {
        [JsonProperty("actions")]
        public JsonAction[] Actions;
    }

    public class JsonAction
    {
        [JsonProperty("if")]
        public JsonIfAction If;
        [JsonProperty("def")]
        public JsonDefineAction Define;
        [JsonProperty("func")]
        public JsonCallFunction Function;
    }

    public class JsonDefineAction
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("type")]
        public JsonType Type;
        [JsonProperty("ext")]
        public bool Extended;
        [JsonProperty("init")]
        public JsonExpression InitialValue;
    }

    public class JsonIfAction
    {
        [JsonProperty("expr")]
        public JsonExpression Expression;
        [JsonProperty("block")]
        public JsonBlock Block;
        [JsonProperty("elif")]
        public JsonIfAction[] ElseIfs;
        [JsonProperty("el")]
        public JsonBlock Else;
    }

    public class JsonExpression
    {
        [JsonProperty("num")]
        public double Number;
        [JsonProperty("tern")]
        public JsonTernary Ternary;
        [JsonProperty("op")]
        public JsonOp Op;
        [JsonProperty("bool")]
        public bool? Boolean;
        [JsonProperty("func")]
        public JsonCallFunction Function;
    }

    public class JsonTernary
    {
        [JsonProperty("cnd")]
        public JsonExpression Condition;
        [JsonProperty("con")]
        public JsonExpression Consequent;
        [JsonProperty("alt")]
        public JsonExpression Alternative;
    }

    public class JsonOp
    {
        [JsonProperty("r")]
        public JsonExpression Right;
        [JsonProperty("l")]
        public JsonExpression Left;
        [JsonProperty("op")]
        public string Operator;
    }
}