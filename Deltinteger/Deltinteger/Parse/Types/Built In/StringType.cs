using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringType : CodeType, IResolveElements
    {
        private readonly ITypeSupplier _typeSupplier;
        private readonly Scope _scope = new Scope();
        private readonly InternalVar _length = new InternalVar("Length", CompletionItemKind.Property);

        public StringType(ITypeSupplier typeSupplier) : base("String")
        {
            _typeSupplier = typeSupplier;
            CanBeExtended = false;
            Kind = "struct";
        }

        public override void ResolveElements()
        {
            Operations = new ITypeOperation[] {
                new StringAddOperation(_typeSupplier)
            };

            _length.CodeType = _typeSupplier.Number();

            _scope.AddNativeMethod(FormatFunction(_typeSupplier));
            _scope.AddNativeMethod(ContainsFunction(_typeSupplier));
            _scope.AddNativeMethod(SliceFunction(_typeSupplier));
            _scope.AddNativeVariable(_length);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(_length, Element.Part("String Length", reference));
        }

        public override Scope GetObjectScope() => _scope;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;

        // String Contains function
        FuncMethod ContainsFunction(ITypeSupplier supplier) => new FuncMethodBuilder() {
            Name = "Contains",
            Documentation = "Determines if the string contains the specified value.",
            ReturnType = supplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("value", "The substring that will be searched for.", supplier.String())
            },
            Action = (actionSet, methodCall) => Element.Part("String Contains", actionSet.CurrentObject, methodCall.Get(0))
        };

        // String Slice function
        FuncMethod SliceFunction(ITypeSupplier supplier) => new FuncMethodBuilder() {
            Name = "Slice",
            Documentation = "Gets a substring from the string by a specified length.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new CodeParameter("start", "The starting index of the substring.", supplier.Number()),
                new CodeParameter("length", "The length of the substring.", supplier.Number())
            },
            Action = (actionSet, methodCall) => Element.Part("String Slice", actionSet.CurrentObject, methodCall.Get(0), methodCall.Get(1))
        };

        // String Format function
        FuncMethod FormatFunction(ITypeSupplier supplier) => new FuncMethodBuilder() {
            Name = "Format",
            Documentation = "Inserts an array of objects into a string.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new StringFormatArrayParameter()
            },
            OnCall = (parseInfo, range) => {
                // Resolve the source usage with StringFormat.
                // This will tell the source string to not add an error for incorrect formats.
                parseInfo.SourceUsageResolver?.Resolve(UsageType.StringFormat);

                // The source string will be resolved after this function returns.
                var sourceResolver = new StringFormatSourceResolver();

                parseInfo.SourceExpression.OnResolve(expr => ConstantExpressionResolver.Resolve(expr, expr => {
                    // Make sure the resolved source expression is a string.
                    if (expr is StringAction stringAction)
                        // Resolve the sourceResolver with the obtained string.
                        sourceResolver.ResolveString(stringAction);
                    else // Expression is not a constant string, add an error.
                        parseInfo.Script.Diagnostics.Error("Source expression must be a string constant", range);
                }));

                return sourceResolver;
            },
            Action = (actionSet, methodCall) => {
                // This will get the source resolver instance that was initialized above.
                var sourceResolver = (StringFormatSourceResolver)methodCall.AdditionalData;

                // Create and return the string.
                return sourceResolver.ParseString(actionSet);
            }
        };
    }

    class StringFormatArrayParameter : CodeParameter
    {
        public StringFormatArrayParameter() : base("args") {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            if (additionalData is StringFormatSourceResolver stringData)
                // Resolve the parameter expression.
                ConstantExpressionResolver.Resolve(value, value => {
                    // Make sure the resolved expression is an array and the arg count matches.
                    if (value is CreateArrayAction array && array.Values.Length == stringData.ArgCount)
                        // Resolve the array values.
                        stringData.ResolveFormats(array.Values);
                    else // If the resolved expression is not an array or the array length is not equal to the format arg count, add an error.
                        parseInfo.Script.Diagnostics.Error("Expected an array with " + stringData.ArgCount + " elements", valueRange);
                });
            // If additionalData is not StringFormatSourceResolver, we can assume an error was added elsewhere.
            return null;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    class StringFormatSourceResolver
    {
        public StringAction StringAction { get; private set; }
        public IExpression[] Formats { get; private set; }
        public int ArgCount { get; private set; }

        public void ResolveString(StringAction stringAction)
        {
            StringAction = stringAction;
            ArgCount = stringAction.StringParseInfo.ArgCount;
        }

        public void ResolveFormats(IExpression[] formats) => Formats = formats;

        public IWorkshopTree ParseString(ActionSet actionSet)
        {
            var parameters = new IWorkshopTree[Formats.Length];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Formats[i].Parse(actionSet);

            return StringAction.StringParseInfo.Parse(actionSet, parameters);
        }
    }

    class StringAddOperation : ITypeOperation
    {
        public TypeOperator Operator => TypeOperator.Add;
        public CodeType Right { get; }
        public CodeType ReturnType { get; }

        public StringAddOperation(ITypeSupplier typeSupplier)
        {
            Right = typeSupplier.Any();
            ReturnType = typeSupplier.String();
        }

        public IWorkshopTree Resolve(ActionSet actionSet, IExpression left, IExpression right)
        {
            // If we are adding strings like ["1" + "2" + "3"], we want to use one custom string, like:
            //    "{0}{1}{2}".format("1", "2", "3")
            // rather than:
            //    "{0}{1}".format("{0}{1}".format("1", "2"), "3")
            //
            // To do this, we add every element in a row of string operators to a single list by recursively calling Flatten.
            // example: (("1", "2"), "3") -> ("1", "2", "3")
            var expressions = new List<IExpression>();
            Flatten(expressions, left);
            Flatten(expressions, right);

            // Now convert every element to the workshop.
            var elements = new IWorkshopTree[expressions.Count];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = expressions[i].Parse(actionSet);
            
            // Finally, join all the elements into a string.
            return Elements.StringElement.Join(elements);
        }

        private void Flatten(List<IExpression> list, IExpression expression)
        {
            // If the expression is an operator whose operation is a StringAddOperation, recursively flatten the operator's Left and Right.
            if (expression is OperatorAction operatorAction && operatorAction.Operation is StringAddOperation)
            {
                Flatten(list, operatorAction.Left);
                Flatten(list, operatorAction.Right);
            }
            else // Otherwise, add it to the list.
                list.Add(expression);
        }
    }
}