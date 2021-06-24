using System.Linq;

namespace Deltin.Deltinteger.Parse
{
    public class LabelInfo
    {
        public bool IncludeReturnType;
        public bool IncludeParameterTypes;
        public bool IncludeParameterNames;
        public bool IncludeDocumentation;
        public AnonymousLabelInfo AnonymousLabelInfo = AnonymousLabelInfo.Default;

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

        public MarkupBuilder MakeFunctionLabel(DeltinScript deltinScript, CodeType type, string name, IParameterLike[] parameters, AnonymousType[] typeArgs)
        {
            var builder = new MarkupBuilder().StartCodeLine();

            if (IncludeReturnType)
                builder.Add(AnonymousLabelInfo.NameFromSolver(deltinScript, type) + " ");
            
            builder.Add(name);

            if (typeArgs.Length != 0)
                builder.Add("<" + string.Join(", ", typeArgs.Select(typeArg => typeArg.GetDeclarationName())) + ">");
            
            builder.Add("(");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) builder.Add(", ");
                builder.Add(parameters[i].GetLabel(deltinScript, AnonymousLabelInfo));
            }
            
            return builder.Add(")").EndCodeLine();
        }
    }

    public class AnonymousLabelInfo
    {
        public static readonly AnonymousLabelInfo Default = new AnonymousLabelInfo();

        public AnonymousLabelInfo() {}
        public AnonymousLabelInfo(InstanceAnonymousTypeLinker typeLinker)
        {
            TypeLinker = typeLinker;
            MakeAnonymousTypesUnkown = true;
        }

        public InstanceAnonymousTypeLinker TypeLinker = null;
        public bool MakeAnonymousTypesUnkown = false;

        public string NameFromSolver(DeltinScript deltinScript, ICodeTypeSolver solver)
        {
            // null: return void
            if (solver == null)
                return "void";

            // Get the type from the type provider.
            var type = solver.GetCodeType(deltinScript);

            // If a type linker is provider, get the real type.
            if (TypeLinker != null)
                type = type.GetRealType(TypeLinker) ?? type;

            // Get the type name. If MakeAnonymousTypesUnkown and the type is an anonymous type, set the type name to 'unknown'.
            return type.GetName(new(MakeAnonymousTypesUnkown, TypeLinker));
        }
    }
}