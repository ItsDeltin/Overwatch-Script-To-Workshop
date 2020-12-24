using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using ColorInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ColorInformation;
using DocumentColor = OmniSharp.Extensions.LanguageServer.Protocol.Models.DocumentColor;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod CustomColor(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "CustomColor",
            Documentation = "Custom color with specified rgb and alpha values.",
            ReturnType = deltinScript.Types.EnumType("Color"),
            Parameters = new CodeParameter[] {
                new CustomColorParameter(0, "red", "The red component of a color.", deltinScript.Types.Number()),
                new CustomColorParameter(1, "green", "The green component of a color.", deltinScript.Types.Number()),
                new CustomColorParameter(2, "blue", "The blue component of a color.", deltinScript.Types.Number()),
                new CustomColorParameter(3, "alpha", "The alpha component of a color.", deltinScript.Types.Number()),
            },
            OnCall = (parseInfo, callRange) => new CustomColorApplier(parseInfo.Script, callRange),
            Action = (actionSet, methodCall) => Element.Part("Custom Color", methodCall.Get(0), methodCall.Get(1), methodCall.Get(2), methodCall.Get(3))
        };
    }

    class CustomColorApplier
    {
        private readonly ScriptFile _script;
        private readonly DocRange _range;
        private readonly bool[] _set = new bool[4];
        private readonly double[] _value = new double[4] { 0, 0, 0, 255 };
        private bool _discard;
        private bool _saved;

        public CustomColorApplier(ScriptFile script, DocRange range)
        {
            _script = script;
            _range = range;
        }

        public void Set(int component) => Set(component, 0);

        public void Set(int component, double value)
        {
            if (_saved) throw new Exception("Document color already saved, can't set component.");
            _set[component] = true;
            _value[component] = value;

            // All color components were set.
            if (!_discard && _set[0] && _set[1] && _set[2] && _set[3])
            {
                // Add the document color.
                _script.AddColorRange(new ColorInformation() {
                    Range = _range,
                    Color = new DocumentColor() {
                        Red = _value[0] / 255,
                        Green = _value[1] / 255,
                        Blue = _value[2] / 255,
                        Alpha = _value[3] / 255
                    }
                });
                _saved = true;
            }
        }

        public void Discard() => _discard = true;
    }

    class CustomColorParameter : CodeParameter
    {
        // 0=r, 1=b, 2=g, 3=a
        private readonly int _component;

        public CustomColorParameter(int component, string name, string documentation, CodeType type)
            : base(name, documentation, type, new ExpressionOrWorkshopValue(Element.Num(0)))
        {
            _component = component;
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            base.Validate(parseInfo, value, valueRange, additionalData);

            // Get the custom color applier.
            var applier = (CustomColorApplier)additionalData;

            // If there is no value.
            if (value == null)
                applier.Set(_component);
            else
                // Resolve the expression.
                ConstantExpressionResolver.Resolve(value, value => {
                    // If the expression is a number, set the component.
                    if (value is NumberAction numberAction)
                        applier.Set(_component, numberAction.Value);
                    // Parameter default value.
                    else if (value is ExpressionOrWorkshopValue expressionOrWorkshop && expressionOrWorkshop.WorkshopValue is NumberElement numberElement)
                        applier.Set(_component, numberElement.Value);
                    // Otherwise, then number isn't a constant, so we won't show the document color.
                    // Discard the custom color applier.
                    else
                        applier.Discard();
                });
            
            return null;
        }
    }
}