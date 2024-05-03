#nullable enable

using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Workshop;

public class ValidateReferences
{
    readonly List<ValidatePointer> validatePoiners = [];

    public void Add(Element validatePointer, string? name, ScriptFile file, DocRange range) => validatePoiners.Add(new(validatePointer, name, file, range));

    public bool Any() => validatePoiners.Count != 0;

    public IEnumerable<ValidatePointer> Collect() => validatePoiners;
}

public readonly record struct ValidatePointer(Element Pointer, string? Name, ScriptFile File, DocRange Range);