#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Deltin.Deltinteger.Model;

static class Variant
{
    public static Variant<TA, TB> AElseB<TA, TB>(TA? a, TB b)
    {
        return a is not null ? a : b;
    }
}

public readonly struct Variant<TA, TB>
{
    readonly bool isA = false;
    public readonly TA? A = default;
    public readonly TB? B = default;

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

    public static bool operator ==(Variant<TA, TB> lhs, Variant<TA, TB> rhs) => lhs.Equals(rhs);
    public static bool operator !=(Variant<TA, TB> lhs, Variant<TA, TB> rhs) => !lhs.Equals(rhs);

    public override bool Equals(object? obj)
    {
        if (isA && obj is TA a)
            return A!.Equals(a);

        if (!isA && obj is TB b)
            return B!.Equals(b);

        if (obj is Variant<TA, TB> variant)
            return isA == variant.isA && (isA ? A!.Equals(variant.A) : B!.Equals(variant.B));

        return false;
    }

    public override int GetHashCode() => (isA, A, B).GetHashCode();

    public override string? ToString() => isA ? A?.ToString() : B?.ToString();
}