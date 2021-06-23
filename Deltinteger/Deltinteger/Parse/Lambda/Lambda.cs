using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public interface IVariableTracker
    {
        void LocalVariableAccessed(IVariableInstance variable);
    }

    public interface ILambdaApplier : ILabeled
    {
        CallInfo CallInfo { get; }
        IRecursiveCallHandler RecursiveCallHandler { get; }
        IBridgeInvocable[] InvokedState { get; }
        bool ResolvedSource { get; }
        void GetLambdaStatement(PortableLambdaType expecting);
        void GetLambdaStatement();
        IEnumerable<RestrictedCallType> GetRestrictedCallTypes();
    }

    public interface ILambdaInvocable
    {
        IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues);
    }

    public interface IBridgeInvocable
    {
        bool Invoked { get; }
        void WasInvoked();
        void OnInvoke(Action onInvoke);
    }

    public class SubLambdaInvoke : IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        public List<Action> Actions { get; } = new List<Action>();

        public void WasInvoked() => Invoked = true;
        public void OnInvoke(Action onInvoke) => Actions.Add(onInvoke);
    }

    public class LambdaContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly LambdaParameter _parameter;

        public LambdaContextHandler(ParseInfo parseInfo, LambdaParameter parameter)
        {
            ParseInfo = parseInfo;
            _parameter = parameter;
        }

        public void GetComponents(VariableComponentCollection componentCollection) {}
        public IParseType GetCodeType() => _parameter.Type;
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());
        public string GetName() => _parameter.Identifier.GetText();
        public DocRange GetNameRange() => _parameter.Identifier.GetRange(_parameter.Range);
        public DocRange GetTypeRange() => _parameter.Type?.Range;
    }

    public class CheckLambdaContext : IExpectingTypeReady
    {
        public ParseInfo ParseInfo;
        public ILambdaApplier Applier;
        public string ErrorMessage;
        public DocRange Range;
        public ParameterState ParameterState;

        public CheckLambdaContext(ParseInfo parseInfo, ILambdaApplier applier, string errorMessage, DocRange range, ParameterState parameterState)
        {
            ParseInfo = parseInfo;
            Applier = applier;
            ErrorMessage = errorMessage;
            Range = range;
            ParameterState = parameterState;
        }

        public void Check()
        {
            // If no lambda was expected, throw an error since the parameter types can not be determined.
            if (ParseInfo.ExpectingTypeRegistry == null)
            {
                // Parameter data is known.
                if (ParameterState == ParameterState.CountAndTypesKnown)
                    Applier.GetLambdaStatement();
                else
                    ParseInfo.Script.Diagnostics.Error(ErrorMessage, Range);
            }
            else
                ParseInfo.ExpectingTypeRegistry.Apply(this);
        }

        public void NoTypeReady() => Applier.GetLambdaStatement();
        public void TypeReady(CodeType type)
        {
            if (type is PortableLambdaType portableLambdaType)
                Applier.GetLambdaStatement(portableLambdaType);
            else
                NoTypeReady();
        }
    }

    public enum ParameterState
    {
        Unknown,
        CountKnown,
        CountAndTypesKnown
    }
}