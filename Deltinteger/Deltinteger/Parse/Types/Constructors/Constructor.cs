using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class Constructor : IParameterCallable, ICallable
    {
        public string Name => Type.Name;
        public AccessLevel AccessLevel { get; }
        public CodeParameter[] Parameters { get; protected set; }
        public LanguageServer.Location DefinedAt { get; }
        public CodeType Type { get; }
        public MarkupBuilder Documentation { get; set; }

        public Constructor(CodeType type, LanguageServer.Location definedAt, AccessLevel accessLevel)
        {
            Type = type;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public virtual void Parse(ActionSet actionSet, WorkshopParameter[] parameters) { }

        public virtual void Call(ParseInfo parseInfo, DocRange callRange) { }

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo)
        {
            var builder = new MarkupBuilder().StartCodeLine().Add("new " + Type.GetName());
            builder.Add(CodeParameter.GetLabels(deltinScript, labelInfo.AnonymousLabelInfo, Parameters)).EndCodeLine();

            if (labelInfo.IncludeDocumentation)
                builder.NewSection().Add(Documentation);

            return builder;
        }
    }
}