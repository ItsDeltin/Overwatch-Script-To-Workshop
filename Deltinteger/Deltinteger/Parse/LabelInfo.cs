namespace Deltin.Deltinteger.Parse
{
    public class LabelInfo
    {
        public bool IncludeReturnType;
        public bool IncludeParameterTypes;
        public bool IncludeParameterNames;
        public bool IncludeDocumentation;

        public static readonly LabelInfo Hover = new LabelInfo() {
            IncludeDocumentation = true,
            IncludeReturnType = true,
            IncludeParameterTypes = true,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo SignatureOverload = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = true,
            IncludeParameterTypes = true,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo OverloadError = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = false,
            IncludeParameterTypes = false,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo RecursionError = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = false,
            IncludeParameterTypes = true,
            IncludeParameterNames = false
        };

        public MarkupBuilder MakeVariableLabel(CodeType type, string name)
        {
            var builder = new MarkupBuilder().StartCodeLine();

            if (IncludeReturnType)
                builder.Add(type.GetName()).Add(" ");
            
            return builder.Add(name).EndCodeLine();
        }

        public MarkupBuilder MakeFunctionLabel(DeltinScript deltinScript, CodeType type, string name, IParameterLike[] parameters)
        {
            var builder = new MarkupBuilder().StartCodeLine();

            if (IncludeReturnType)
                builder.Add(type.GetName() + " ");
            
            builder.Add(name + "(");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) builder.Add(", ");
                builder.Add(parameters[i].GetLabel(deltinScript));
            }
            
            return builder.Add(")").EndCodeLine();
        }
    }
}