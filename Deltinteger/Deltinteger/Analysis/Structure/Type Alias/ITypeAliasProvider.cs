using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Types.Generics;
using DS.Analysis.Scopes;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.TypeAlias
{
    interface ITypeAliasProvider
    {
        TypeAliasSetup Setup(ContextInfo context);
    }

    struct TypeAliasSetup
    {
        public readonly string Name;
        public readonly IDisposableTypeDirector TypeReference;
        public readonly TypeArgCollection TypeArgs;

        public TypeAliasSetup(string name, IDisposableTypeDirector typeReference, TypeArgCollection typeArgs)
        {
            Name = name;
            TypeReference = typeReference;
            TypeArgs = typeArgs;
        }
    }


    class TypeAliasProvider : ITypeAliasProvider
    {
        readonly TypeAliasContext typeAliasSyntax;

        public TypeAliasProvider(TypeAliasContext typeAliasSyntax)
        {
            this.typeAliasSyntax = typeAliasSyntax;
        }

        public TypeAliasSetup Setup(ContextInfo context)
        {
            var source = new ScopeSource();

            // Contain type arguments.
            context = context.AddAppendableSource(source);

            // Get the type arguments.
            var typeArgs = TypeArgCollection.FromSyntax(typeAliasSyntax.Generics);

            // Add the type arguments to the scope.
            typeArgs.AddToScope(source);

            return new TypeAliasSetup(
                name: typeAliasSyntax.NewTypeName.Text,
                typeReference: TypeFromContext.TypeReferenceFromSyntax(context, typeAliasSyntax.OtherType),
                typeArgs: typeArgs
            );
        }
    }
}