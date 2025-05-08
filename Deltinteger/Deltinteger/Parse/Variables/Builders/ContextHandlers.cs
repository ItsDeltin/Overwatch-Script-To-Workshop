using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public interface IVarContextHandler
    {
        ParseInfo ParseInfo { get; }
        Location GetDefineLocation();
        string GetName();
        DocRange GetNameRange();
        void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind);
        IParseType GetCodeType();
        DocRange GetTypeRange();
        MarkupBuilder Documentation() => null;
    }

    class DefineContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly VariableDeclaration _defineContext;
        private readonly MarkupBuilder _documentation;

        public DefineContextHandler(ParseInfo parseInfo, VariableDeclaration defineContext, MarkupBuilder externalDocumentation = null)
        {
            _defineContext = defineContext;
            ParseInfo = parseInfo;
            _documentation = externalDocumentation ?? defineContext.Comment?.GetContents();
        }

        public Location GetDefineLocation() => ParseInfo.Script.GetLocation(GetNameRange());
        public string GetName() => _defineContext.Identifier.GetText();
        public DocRange GetNameRange() => _defineContext.Identifier.GetRange(_defineContext.Range);
        public IParseType GetCodeType() => _defineContext.Type;
        public DocRange GetTypeRange() => _defineContext.Type.Range;
        public MarkupBuilder Documentation() => _documentation;

        public void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind)
        {
            // Add attribute components.
            AttributesGetter.GetAttributes(ParseInfo.Script.Diagnostics, _defineContext.Attributes, componentCollection);

            // Add workshop ID
            if (_defineContext.ID)
                componentCollection.AddComponent(new WorkshopIndexComponent((int)double.Parse(_defineContext.ID.Text), _defineContext.ID.Range));

            // Extended collection
            if (_defineContext.Extended)
                componentCollection.AddComponent(new ExtendedCollectionComponent(_defineContext.Range));

            // Initial value
            if (_defineContext.InitialValue != null)
                componentCollection.AddComponent(new InitialValueComponent(_defineContext.InitialValue, _defineContext.StartToken, _defineContext.EndToken));

            // Macro
            if (_defineContext.MacroSymbol)
                componentCollection.AddComponent(new MacroComponent(_defineContext.MacroSymbol.Range));

            // Target workshop variable
            if (_defineContext.Target.HasValue)
                componentCollection.AddComponent(new TargetWorkshopComponent(_defineContext.Target.Value, variableSetKind));
        }
    }
}