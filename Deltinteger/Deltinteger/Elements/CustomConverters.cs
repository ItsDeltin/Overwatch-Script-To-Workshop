using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Elements
{
    static class CustomConverters
    {
        public static readonly Dictionary<string, Action<WorkshopBuilder, Element>> Converters = new Dictionary<string, Action<WorkshopBuilder, Element>>() {
            {"If-Then-Else", (builder, element) => {
                element.ParameterValues[0].ToWorkshop(builder, ToWorkshopContext.NestedValue);
                builder.Append(" ? ");
                element.ParameterValues[1].ToWorkshop(builder, ToWorkshopContext.NestedValue);
                builder.Append(" : ");
                element.ParameterValues[2].ToWorkshop(builder, ToWorkshopContext.NestedValue);
            }}
        };
    }
}