namespace DS.Analysis.Std.Workshop;
using DS.Analysis.ModuleSystem;
using DS.Analysis.Methods;
using DS.Analysis.Types;
using DS.Analysis.Types.Utility;
using Deltin.Deltinteger.Elements;
using System.Linq;

class WorkshopModule
{
    public static Module CreateWorkshopModule(ModuleManager manager)
    {
        return ModuleMaker.CreateInternalModule(manager, "Workshop", maker =>
        {
            // Actions
            foreach (var action in ElementRoot.Instance.Actions)
                maker.AddMethod(MethodFromAction(action));
            // Values
            foreach (var value in ElementRoot.Instance.Values)
                maker.AddMethod(MethodFromValue(value));
            // Enumerators
            foreach (var enumerator in ElementRoot.Instance.Enumerators)
                maker.AddType(TypeFromEnum(enumerator));
        }).Item1;
    }

    static ICodeTypeProvider TypeFromEnum(ElementEnum enumerator) => Enums.CreateRecord(
        enumerator.Name,
        enumerator.Members.Select(m => m.CodeName()).ToArray()
    );

    static Method MethodFromAction(ElementJsonAction action) => MethodFromElement(action);

    static Method MethodFromValue(ElementJsonValue value) => MethodFromElement(value);

    static Method MethodFromElement(ElementBaseJson expression)
    {
        return Method.Create(
            name: expression.CodeName(),
            doc: expression.Documentation,
            parameters: ElementToAnalysisParameter(expression.Parameters));
    }

    static Parameter[] ElementToAnalysisParameter(ElementParameter[] parameters) => parameters.Select(p => new Parameter(
        p.Name, p.Documentation, ElementTypeToCodeType(p.Type)
    )).ToArray();

    static CodeType ElementTypeToCodeType(string typeName)
    {
        
    }
}