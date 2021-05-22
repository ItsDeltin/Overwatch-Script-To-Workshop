using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.LanguageServer;
using Newtonsoft.Json.Linq;
using Command = OmniSharp.Extensions.LanguageServer.Protocol.Models.Command;

namespace Deltin.Deltinteger.Parse
{
    [Flags]
    public enum CodeLensSourceType
    {
        None = 0,
        Function = 1,
        Type = 2,
        EnumValue = 4,
        Variable = RuleVariable | ClassVariable | ScopedVariable | ParameterVariable,
        RuleVariable = 8,
        ClassVariable = 16,
        ScopedVariable = 32,
        ParameterVariable = 64,
        Constructor = 128
    }

    public abstract class CodeLensRange
    {
        public CodeLensSourceType SourceType { get; }
        public DocRange Range { get; }

        public CodeLensRange(CodeLensSourceType sourceType, DocRange range)
        {
            SourceType = sourceType;
            Range = range;
        }

        public virtual bool ShouldUse() => true;
        public abstract Command GetCommand();
    }

    class ReferenceCodeLensRange : CodeLensRange
    {
        public object DeclarationKey { get; }
        readonly ParseInfo _parseInfo;

        public ReferenceCodeLensRange(object declarationKey, ParseInfo parseInfo, CodeLensSourceType sourceType, DocRange range) : base(sourceType, range)
        {
            DeclarationKey = declarationKey;
            _parseInfo = parseInfo;
        }

        public override Command GetCommand()
        {
            var locations = new List<Location>();

            // Loop through each script.
            foreach (var script in _parseInfo.TranslateInfo.Importer.ScriptFiles)
                // Check if said script calls the declaration key.
                if (script.Elements.DeclarationCalls.TryGetValue(DeclarationKey, out var calls))
                    // If it does, add the calls.
                    locations.AddRange(calls.Select(c => script.GetLocation(c.CallRange)));

            return new Command {
                Name = "ostw.showReferences",
                Title = locations.Count.ToString() + " references",
                // todo: add declaration
                Arguments = new JArray {
                    // Uri
                    JToken.FromObject(_parseInfo.Script.Uri.ToString()),
                    // Range
                    JToken.FromObject(Range.Start),
                    // Locations
                    JToken.FromObject(locations)
                }
            };
        }
    }

    class ImplementsCodeLensRange : CodeLensRange
    {
        public IMethod Method { get; }
        private readonly ScriptFile _script;

        public ImplementsCodeLensRange(IMethod method, ScriptFile script, CodeLensSourceType sourceType, DocRange range) : base(sourceType, range)
        {
            Method = method;
            _script = script;
        }

        public override bool ShouldUse()
        {
            // Only show the codelens if the method is overriden.
            return Method.Attributes.Overriders.Length > 0;
        }

        // TODO
        // public override string GetTitle() => Method.Attributes.Overriders.Length + " implements";

        // public override JArray GetArguments() => new JArray {
        //     // Uri
        //     JToken.FromObject(_script.Uri.ToString()),
        //     // Range
        //     JToken.FromObject(Range.Start),
        //     // Locations
        //     JToken.FromObject(Method.Attributes.Overriders.Select(overrider => overrider.DefinedAt))
        // };

        public override Command GetCommand()
        {
            throw new NotImplementedException();
        }
    }

    public class ElementCountCodeLens : CodeLensRange
    {
        private int elementCount = -1;
        private int actionCount = -1;

        public ElementCountCodeLens(DocRange range) : base(CodeLensSourceType.None, range)
        {
        }

        public void RuleParsed(Rule rule)
        {
            elementCount = rule.ElementCount();
            actionCount = rule.Actions.Length;
        }

        public override Command GetCommand()
        {
            string title = elementCount == -1 ?
                "- actions, - elements" :
                actionCount + " actions, " + elementCount + " elements";

            return new Command {
                Name = null,
                Title = title,
                Arguments = new JArray()
            };
        }
    }
}