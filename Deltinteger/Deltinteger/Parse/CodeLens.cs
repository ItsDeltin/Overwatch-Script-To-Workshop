using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Parse
{
    [Flags]
    public enum CodeLensSourceType
    {
        Function = 1,
        Type = 2,
        EnumValue = 4,
        Variable = RuleVariable | ClassVariable | ScopedVariable | ParameterVariable,
        RuleVariable = 8,
        ClassVariable = 16,
        ScopedVariable = 32,
        ParameterVariable = 64,
    }

    public abstract class CodeLensRange
    {
        public CodeLensSourceType SourceType { get; }
        public DocRange Range { get; }
        public string Command { get; }

        public CodeLensRange(CodeLensSourceType sourceType, DocRange range, string command)
        {
            SourceType = sourceType;
            Range = range;
            Command = command;
        }

        public abstract string GetTitle();

        public virtual bool ShouldUse() => true;

        public virtual JArray GetArguments() => new JArray();
    }

    class ReferenceCodeLensRange : CodeLensRange
    {
        public ICallable Callable { get; }
        private readonly ParseInfo _parseInfo;

        public ReferenceCodeLensRange(ICallable callable, ParseInfo parseInfo, CodeLensSourceType sourceType, DocRange range) : base(sourceType, range, "ostw.showReferences")
        {
            Callable = callable;
            _parseInfo = parseInfo;
        }

        public override string GetTitle() => (_parseInfo.TranslateInfo.GetSymbolLinks(Callable).Count - 1).ToString() + " references";

        public override JArray GetArguments() => new JArray {
            // Uri
            JToken.FromObject(_parseInfo.Script.Uri.ToString()),
            // Range
            JToken.FromObject(Range.start),
            // Locations
            JToken.FromObject(_parseInfo.TranslateInfo.GetSymbolLinks(Callable).GetSymbolLinks(false).Select(sl => sl.Location))
        };
    }

    class ImplementsCodeLensRange : CodeLensRange
    {
        public IMethod Method { get; }
        private readonly ScriptFile _script;

        public ImplementsCodeLensRange(IMethod method, ScriptFile script, CodeLensSourceType sourceType, DocRange range) : base(sourceType, range, "ostw.showReferences")
        {
            Method = method;
            _script = script;
        }

        public override bool ShouldUse()
        {
            // Only show the codelens if the method is overriden.
            return Method.Attributes.Overriders.Length > 0;
        }

        public override string GetTitle() => Method.Attributes.Overriders.Length + " implements";

        public override JArray GetArguments() => new JArray {
            // Uri
            JToken.FromObject(_script.Uri.ToString()),
            // Range
            JToken.FromObject(Range.start),
            // Locations
            JToken.FromObject(Method.Attributes.Overriders.Select(overrider => overrider.DefinedAt))
        };
    }
}