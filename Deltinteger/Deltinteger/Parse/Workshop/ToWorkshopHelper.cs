namespace Deltin.Deltinteger.Parse;

using Elements;

static class ToWorkshopHelper
{
    /// <summary>
    /// Many operations in the Overwatch Workshop will execute on each value of an array
    /// rather than the array itself. The fix for this is to wrap the array inside another
    /// array.
    /// </summary>
    /// <param name="value">The input value that may need protection.</param>
    /// <param name="type">If type.NeedsArrayProtection, the input value is wrapped.</param>
    /// <returns>Returns `value` wrapped in a workshop `Array()` if the input type requires it.</returns>
    public static IWorkshopTree GuardArrayModifiedValue(IWorkshopTree value, CodeType type)
    {
        if (type.NeedsArrayProtection)
        {
            return Element.CreateArray(value);
        }
        // Protection is determined on individual parallel values.
        else if (type.Attributes.IsStruct && value is IStructValue structValue)
        {
            // Bridging modifies each value in the struct.
            return structValue.Bridge(args =>
            {
                var innerType = StructHelper.GetVariableTypeFromPath(args.Path, type);
                // Inner struct value needs protection.
                if (innerType != null && innerType.NeedsArrayProtection)
                    return Element.CreateArray(args.Value);
                // Otherwise, do not change.
                return args.Value;
            });
        }
        return value;
    }
}