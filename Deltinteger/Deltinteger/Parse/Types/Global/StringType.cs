using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringType : CodeType, IGetMeta
    {
        readonly ITypeSupplier _typeSupplier;
        readonly Scope _scope = new Scope();
        InternalVar _length;

        public StringType(DeltinScript deltinScript, ITypeSupplier typeSupplier) : base("String")
        {
            _typeSupplier = typeSupplier;
            deltinScript.StagedInitiation.On(this);
        }

        public void GetMeta()
        {
            _length = new InternalVar("Length", _typeSupplier.Number(), CompletionItemKind.Property);

            Operations.AddTypeOperation(new ITypeOperation[] {
                new StringAddOperation(_typeSupplier)
            });

            _scope.AddNativeMethod(FormatFunction(_typeSupplier));
            _scope.AddNativeMethod(ContainsFunction(_typeSupplier));
            _scope.AddNativeMethod(SliceFunction(_typeSupplier));
            _scope.AddNativeMethod(CharInStringFunction(_typeSupplier));
            _scope.AddNativeMethod(IndexOfFunction(_typeSupplier));
            _scope.AddNativeMethod(SplitFunction(_typeSupplier));
            _scope.AddNativeMethod(ReplaceFunction(_typeSupplier));
            _scope.AddNativeVariable(_length);
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference reference, VarIndexAssigner assigner)
        {
            assigner.Add(_length, Element.Part("String Length", reference.Value));
        }

        public override Scope GetObjectScope() => _scope;
        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;

        // String Contains function
        FuncMethod ContainsFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "Contains",
            Documentation = "Determines if the string contains the specified value.",
            ReturnType = supplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("value", "The substring that will be searched for.", supplier.String())
            },
            Action = (actionSet, methodCall) => Element.Part("String Contains", actionSet.CurrentObject, methodCall.Get(0))
        };

        // String Slice function
        FuncMethod SliceFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "Slice",
            Documentation = "Gets a substring from the string by a specified length.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new CodeParameter("start", "The starting index of the substring.", supplier.Number()),
                new CodeParameter("length", "The length of the substring.", supplier.Number())
            },
            Action = (actionSet, methodCall) => Element.Part("String Slice", actionSet.CurrentObject, methodCall.Get(0), methodCall.Get(1))
        };

        // String Slice function
        FuncMethod CharInStringFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "CharAt",
            Documentation = "The character found at a specified index of a String.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new CodeParameter("index", "The index of the character.", supplier.Number()),
            },
            Action = (actionSet, methodCall) => Element.Part("Char In String", actionSet.CurrentObject, methodCall.Get(0))
        };

        // String Slice function
        FuncMethod IndexOfFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "IndexOf",
            Documentation = "The index of a character within a String or -1 of no such character can be found.",
            ReturnType = supplier.Number(),
            Parameters = new CodeParameter[] {
                new CodeParameter("character", "The character for which to search.", supplier.String()),
            },
            Action = (actionSet, methodCall) => Element.Part("Index Of String Char", actionSet.CurrentObject, methodCall.Get(0))
        };

        // String Split function
        FuncMethod SplitFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "Split",
            Documentation = "Results in an Array of String Values. These String Values will be built from the specified String Value, split around the seperator String.",
            ReturnType = supplier.Array(supplier.String()),
            Parameters = new CodeParameter[] {
                new CodeParameter("seperator", "The seperator String with which to split the String Value.", supplier.String()),
            },
            Action = (actionSet, methodCall) => Element.Part("String Split", actionSet.CurrentObject, methodCall.Get(0))
        };

        // String Replace function
        FuncMethod ReplaceFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "Replace",
            Documentation = "Results in a String Value. This String Value will be built from the specified String Value, where all occurrences of the pattern String are replaced with the replacement String.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new CodeParameter("pattern", "The String pattern to be replaced.", supplier.String()),
                new CodeParameter("replacement", "The String Value in which to replace the pattern String.", supplier.String())
            },
            Action = (actionSet, methodCall) => Element.Part("String Replace", actionSet.CurrentObject, methodCall.Get(0), methodCall.Get(1))
        };

        // String Format function
        FuncMethod FormatFunction(ITypeSupplier supplier) => new FuncMethodBuilder()
        {
            Name = "Format",
            Documentation = "Inserts an array of objects into a string.",
            ReturnType = supplier.String(),
            Parameters = new CodeParameter[] {
                new StringFormatArrayParameter(supplier)
            },
            OnCall = (parseInfo, range) =>
            {
                // Resolve the source usage with StringFormat.
                // This will tell the source string to not add an error for incorrect formats.
                parseInfo.SourceUsageResolver?.Resolve(UsageType.StringFormat);

                // The source string will be resolved after this function returns.
                var sourceResolver = new StringFormatSourceResolver();

                parseInfo.SourceExpression.OnResolve(expr => ConstantExpressionResolver.Resolve(expr, expr =>
                {
                    // Make sure the resolved source expression is a string.
                    if (expr is StringAction stringAction)
                        // Resolve the sourceResolver with the obtained string.
                        sourceResolver.ResolveString(stringAction);
                    else // Expression is not a constant string, add an error.
                        parseInfo.Script.Diagnostics.Error("Source expression must be a string constant", range);
                }));

                return sourceResolver;
            },
            Action = (actionSet, methodCall) =>
            {
                // This will get the source resolver instance that was initialized above.
                var sourceResolver = (StringFormatSourceResolver)methodCall.AdditionalData;

                // Create and return the string.
                return sourceResolver.ParseString(actionSet);
            }
        };
    }

    class StringFormatArrayParameter : CodeParameter
    {
        public StringFormatArrayParameter(ITypeSupplier types) : base("args", types.AnyArray()) { }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            if (additionalData is StringFormatSourceResolver stringData)
                // Resolve the parameter expression.
                ConstantExpressionResolver.Resolve(value, value =>
                {
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
            if (stringAction.StringParseInfo == null)
                return;

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