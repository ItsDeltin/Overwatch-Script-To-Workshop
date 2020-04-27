using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.CustomMethods;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayType : CodeType
    {
        public CodeType ArrayOfType { get; }
        private readonly Scope _scope = new Scope();
        private readonly InternalVar _length = new InternalVar("Length", CompletionItemKind.Property);

        public ArrayType(CodeType arrayOfType) : base(arrayOfType.Name + "[]")
        {
            ArrayOfType = arrayOfType;
            _scope.AddNativeVariable(_length);
            AddConditionalFunction<V_FilteredArray>("FilteredArray", "A copy of the specified array with any values that do not match the specified condition removed.", this, "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array.");
            AddConditionalFunction<V_SortedArray>("SortedArray", "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.", this, "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order.");
            AddConditionalFunction<V_IsTrueForAny>("IsTrueForAny", "Whether the specified condition evaluates to true for any value in the specified array.", null);
            AddConditionalFunction<V_IsTrueForAll>("IsTrueForAll", "Whether the specified condition evaluates to true for every value in the specified array.", null);
        }

        private void AddConditionalFunction<T>(string name, string description, CodeType returnType, string parameterDescription = "The condition that is evaluated for each element of the specified array.") where T: Element, new()
        {
            _scope.AddNativeMethod(new ConditionalArrayFunction<T>(
                name,
                description,
                this,
                parameterDescription,
                returnType
            ));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(_length, Element.Part<V_CountOf>(reference));
        }

        public override Scope GetObjectScope() => _scope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();

        private abstract class BaseArrayFunction : IMethod
        {
            public string Name { get; }
            public CodeType ReturnType { get; protected set; }
            public MethodAttributes Attributes { get; }
            public CodeParameter[] Parameters { get; protected set; }
            public string Documentation { get; }
            public bool Static => false;
            public bool WholeContext => true;
            public Location DefinedAt => null;
            public AccessLevel AccessLevel => AccessLevel.Public;
            protected ArrayType ArrayType { get; }

            public BaseArrayFunction(string name, string documentation, ArrayType arrayType)
            {
                Name = name;
                Documentation = documentation;
                ArrayType = arrayType;
                Attributes = new MethodAttributes() { ContainingType = arrayType };
            }

            public bool DoesReturnValue() => true;
            public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
            public string GetLabel(bool markdown) => HoverHandler.GetLabel(ReturnType?.Name ?? "define", Name, Parameters, markdown, Documentation);

            public abstract IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall);
        }

        private class ConditionalArrayFunction<T> : BaseArrayFunction where T: Element, new()
        {
            public ConditionalArrayFunction(string name, string description, ArrayType arrayType, string parameterDescription, CodeType returnType) : base(name, description, arrayType)
            {
                ReturnType = returnType;
                Parameters = new CodeParameter[] {
                    new CodeParameter("conditionLambda", parameterDescription, new MacroLambda(null, arrayType.ArrayOfType))
                };
            }

            public override IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
            {
                var lambda = (LambdaAction)methodCall.ParameterValues[0];
                return Element.Part<T>(actionSet.CurrentObject, lambda.Invoke(actionSet, new V_ArrayElement()));
            }
        }
    }
}