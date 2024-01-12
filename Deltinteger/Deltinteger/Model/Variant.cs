#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Deltin.Deltinteger.Model;

readonly struct Variant<TA, TB>
{
    readonly bool isA = false;
    readonly TA? A = default;
    readonly TB? B = default;

    public Variant(TA a) { A = a; isA = true; }
    public Variant(TB b) => B = b;

    public void Match(Action<TA>? onA, Action<TB>? onB)
    {
        if (isA) onA?.Invoke(A!);
        else onB?.Invoke(B!);
    }

    public T Match<T>(Func<TA, T> onA, Func<TB, T> onB) => isA ? onA(A!) : onB(B!);

    public bool Get([NotNullWhen(true)] out TA? a, [NotNullWhen(false)] out TB? b)
    {
        if (isA)
        {
            a = A!;
            b = default;
            return true;
        }
        else
        {
            a = default;
            b = B!;
            return false;
        }
    }

    public static implicit operator Variant<TA, TB>(TA value) => new(value);
    public static implicit operator Variant<TA, TB>(TB value) => new(value);
}