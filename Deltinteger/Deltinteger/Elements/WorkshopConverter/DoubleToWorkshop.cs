using System;

namespace Deltin.Deltinteger.Elements.WorkshopConverter;

static class DoubleToWorkshop
{
    const double MAX = 999999999999999;
    const int NUMBER_LENGTH_MAX = 16;

    public static string ToWorkshop(double value)
    {
        return Math.Clamp(value, -MAX, MAX).ToString("0.##########");
    }
}