using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MethodGroup : IVariable
    {
        public string Name { get; }
        public bool WholeContext => true;
        public bool CanBeIndexed => false;
        public bool Static => false; // Doesn't matter.
        public Location DefinedAt => null; // Doesn't matter.
        public AccessLevel AccessLevel => AccessLevel.Public; // Doesn't matter.
        public List<IMethod> Functions { get; } = new List<IMethod>();

        public MethodGroup(string name)
        {
            Name = name;
        }

        public bool MethodIsValid(IMethod method) => method.Name == Name;
        public bool MethodIsValid() => false;
        public void AddMethod(IMethod method) => Functions.Add(method);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Function,
            Documentation = new MarkupBuilder()
                .StartCodeLine()
                .Add(
                    (Functions[0].DoesReturnValue ? (Functions[0].ReturnType == null ? "define" : Functions[0].ReturnType.GetName()) : "void") + " " +
                    Functions[0].GetLabel(false) + (Functions.Count == 1 ? "" : " (+" + (Functions.Count - 1) + " overloads)")
                ).EndCodeLine().ToMarkup()
        };
    }

    public class CallMethodGroup : IExpression, ILambdaApplier
    {
        private readonly ParseInfo _parseInfo;
        private readonly DocRange _range;
        private readonly MethodGroup _group;
        private PortableLambdaType _type;
        private IMethod _chosenFunction;
        private int _identifier;

        public CallMethodGroup(ParseInfo parseInfo, DocRange range, MethodGroup group)
        {
            _parseInfo = parseInfo;
            _range = range;
            _group = group;
            _type = new PortableLambdaType(LambdaKind.Anonymous);
            parseInfo.Script.AddToken(range, TokenType.Function);
        }

        public void GetLambdaStatement(PortableLambdaType expecting)
        {
            bool found = false;
            _type = expecting;
            foreach (var func in _group.Functions)
            {
                if (func.Parameters.Length == expecting.Parameters.Length)
                {
                    // Make sure the method implements the target lambda.
                    for (int i = 0; i < func.Parameters.Length; i++)
                        if (!func.Parameters[i].Type.Implements(expecting.Parameters[i]))
                            continue;
                    
                    _chosenFunction = func;
                    found = true;
                    break;
                }
            }

            // If a compatible function was found, get the handler.
            if (found)
                _identifier = _parseInfo.TranslateInfo.GetComponent<LambdaGroup>().Add(GetLambdaHandler(_chosenFunction));
            else
                _parseInfo.Script.Diagnostics.Error("No overload for '" + _group.Name + "' implements " + expecting.GetName(), _range);
        }

        private static ILambdaHandler GetLambdaHandler(IMethod function)
        {
            // If the chosen function is a DefinedMethod, use the DefinedFunctionHandler.
            if (function is DefinedMethod definedMethod)
                return new DefinedFunctionHandler(definedMethod);
            
            // Otherwise, use the generic function handler.
            return new GenericMethodHandler(function);
        }

        public IWorkshopTree Parse(ActionSet actionSet) => Element.CreateArray(new V_Number(_identifier), actionSet.This ?? new V_Null());

        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
    }

    class MethodGroupType : CodeType
    {
        public MethodGroupType() : base("method group") {}
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }

    class GenericMethodHandler : IFunctionHandler, ILambdaHandler
    {
        public int Identifier { get; set; }
        public CodeType ContainingType => null;
        private readonly IMethod _method;
        private readonly IIndexReferencer[] _parameterSavers;

        public GenericMethodHandler(IMethod method)
        {
            _method = method;

            _parameterSavers = new IIndexReferencer[_method.Parameters.Length];
            for (int i = 0; i < _parameterSavers.Length; i++)
                _parameterSavers[i] = new IndexReferencer(null);
        }

        public string GetName() => _method.Name;
        public bool DoesReturnValue() => _method.DoesReturnValue;
        public IIndexReferencer GetParameterVar(int index) => _parameterSavers[index];
        public SubroutineInfo GetSubroutineInfo() => throw new NotImplementedException();
        public bool IsObject() => false;
        public bool IsRecursive() => false;
        public bool IsSubroutine() => false;
        public bool MultiplePaths() => false;
        public int ParameterCount() => _method.Parameters.Length;

        public void ParseInner(ActionSet actionSet)
        {
            var parameterValues = new IWorkshopTree[_parameterSavers.Length];
            for (int i = 0; i < _parameterSavers.Length; i++)   
                parameterValues[i] = actionSet.IndexAssigner[_parameterSavers[i]].GetVariable();

            var result = _method.Parse(actionSet, new MethodCall(parameterValues, new object[parameterValues.Length]));
            if (_method.DoesReturnValue)
                actionSet.ReturnHandler.ReturnValue(result);
        }
        public object StackIdentifier() => throw new NotImplementedException();
    }
}