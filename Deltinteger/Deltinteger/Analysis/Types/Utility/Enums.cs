namespace DS.Analysis.Types.Utility;
using Components;
using Generics;
using Scopes;
using Expressions.Identifiers;
using System.Linq;

static class Enums
{
    public static ICodeTypeProvider CreateRecord(string name, string[] values) =>
        Utility2.CreateProviderAndDirector(name, TypeArgCollection.Empty, null, factory =>
        {
            factory.SetType(CodeType.Create(
                content: new CodeTypeContent(
                    values.Select(v => ICodeTypeElement.New(ScopedElement.CreateVariable(v, new IdentifierInfo(factory.Director)))).ToArray()
                ),
                comparison: ITypeComparison.Create(),
                getIdentifier: null
            ));
        });
}