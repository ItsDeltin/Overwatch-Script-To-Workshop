#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Functions.Builder;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Parse.Workshop;

public class ValidateReferencesWorkshop
{
    // Variables are used by ValidateInSubroutine 
    bool didInitSubroutine;
    IndexReference? input;
    IndexReference? counter;
    ValidateFunctionController? functionController;
    SubroutineBuilder? subroutineBuilder;
    readonly List<string?> files = [null];

    public void Validate(ActionSet inserterSet, Element[] references, string? file, int line)
    {
        switch (inserterSet.DeltinScript.Settings.ReferenceValidationType)
        {
            // Inline
            case Settings.ReferenceValidationType.Inline:
                ValidateInline(inserterSet, references, file, line);
                break;

            // Subroutine
            case Settings.ReferenceValidationType.Subroutine:
                ValidateInSubroutine(inserterSet, references, file, line);
                break;
        }
    }

    void ValidateInline(ActionSet inserterSet, Element[] references, string? file, int line)
    {
        var classData = inserterSet.DeltinScript.GetComponent<ClassData>();
        IfBuilder.If(inserterSet, Not(
            Aggregate(references.Select(classData.IsReferenceValid), And)), () =>
            {
                string trace = "";
                if (file is not null)
                    trace = $" in {file} at line {line}";
                inserterSet.Log($"[Error] Accessed invalid reference{trace}");
                inserterSet.AbortOnError();
            }).Ok();
    }

    void ValidateInSubroutine(ActionSet inserterSet, Element[] references, string? file, int line)
    {
        if (!didInitSubroutine)
        {
            didInitSubroutine = true;
            var varCollection = inserterSet.DeltinScript.VarCollection;
            input = varCollection.Assign("_classValidationInput", true, false);
            counter = varCollection.Assign("_classValidationCounter", true, false);
            functionController = new(inserterSet.DeltinScript, input, counter, this);
        }

        // Get ID of input file. Add it if it does not exist.
        int fileIndex = files.IndexOf(file);
        if (fileIndex == -1)
        {
            fileIndex = files.Count;
            files.Add(file);
        }

        // Set parameters
        input!.Set(inserterSet, CreateArray([Num(fileIndex), Num(line), .. references]));
        // Call subroutine
        inserterSet.AddAction(CallSubroutine(functionController!.GetSubroutine().Subroutine));
        // Abort if counter is truthy
        if (inserterSet.DeltinScript.Settings.AbortOnError)
            inserterSet.AddAction(AbortIf(Not(counter!.Get())));
    }

    Element GetFileArray() => CreateArray([..files.Select(
        file => file is null ? Num(0) : CustomString(file)
    )]);

    public void Complete() => subroutineBuilder?.Complete();

    class ValidateFunctionController(
        DeltinScript deltinScript,
        IndexReference inputArray,
        IndexReference counterAndConfirmation,
        ValidateReferencesWorkshop owner) : IWorkshopFunctionController
    {
        public WorkshopFunctionControllerAttributes Attributes => new();

        public void Build(ActionSet actionSet)
        {
            var classData = deltinScript.GetComponent<ClassData>();

            // [0] is file, [1] is line, start at [2].
            var forBuilder = new ForBuilder(actionSet, counterAndConfirmation, CountOf(inputArray.Get()), 2);
            forBuilder.Init();
            IfBuilder.If(actionSet, !classData.IsReferenceValid(inputArray.Get()[counterAndConfirmation.Get()]), () =>
            {
                // Log error
                actionSet.AddAction(LogToInspector(CustomString(
                    "[Error] Accessed invalid reference in {0} at line {1}",
                    owner.GetFileArray()[FirstOf(inputArray.Get())],
                    inputArray.Get()[1]
                )));
                // Error state
                counterAndConfirmation.Set(actionSet, 0);
                // Break out of for
                actionSet.AddAction(Break());
            }).Ok();
            forBuilder.End();
        }

        public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters) => DefaultParameterHandler.Instance;
        public ReturnHandler? GetReturnHandler(ActionSet actionSet) => null;
        public SubroutineCatalogItem GetSubroutine()
        {
            return deltinScript.WorkshopConverter.SubroutineCatalog.GetSubroutine(this, () =>
            {
                owner.subroutineBuilder = new SubroutineBuilder(deltinScript, new()
                {
                    RuleName = "[ostw internal] validate class references",
                    ElementName = "_validateClassReferences",
                    Controller = this
                });
                return new SetupSubroutine(owner.subroutineBuilder.Initiate(), () => { });
            });
        }
        public object? StackIdentifier() => null;

        class DefaultParameterHandler : IParameterHandler
        {
            public static readonly DefaultParameterHandler Instance = new();

            private DefaultParameterHandler() { }

            public void AddParametersToAssigner(VarIndexAssigner assigner) { }
            public void Pop(ActionSet actionSet) { }
            public void Push(ActionSet actionSet, IWorkshopTree[] parameterValues) { }
            public void Set(ActionSet actionSet, IWorkshopTree[] parameterValues) { }
        }
    }
}

public class ValidateReferences
{
    readonly List<ValidatePointer> validatePoiners = [];

    public void Add(Element validatePointer, string? name, ScriptFile file, DocRange range) => validatePoiners.Add(new(validatePointer, name, file, range));

    public bool Any() => validatePoiners.Count != 0;

    public IEnumerable<ValidatePointer> Collect() => validatePoiners;
}

public readonly record struct ValidatePointer(Element Pointer, string? Name, ScriptFile File, DocRange Range);