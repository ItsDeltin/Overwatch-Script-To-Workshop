#nullable enable

namespace Deltin.Deltinteger.Parse.Workshop;

public class TempAssign(IGettable reference)
{
    public bool WasUsed { get; private set; }
    readonly IGettable reference = reference;

    public IndexReference? TryTakeNonIndexedReference()
    {
        if (!WasUsed && reference is IndexReference indexReference && indexReference.Index?.Length is null or 0)
        {
            WasUsed = true;
            return indexReference;
        }
        return null;
    }
}