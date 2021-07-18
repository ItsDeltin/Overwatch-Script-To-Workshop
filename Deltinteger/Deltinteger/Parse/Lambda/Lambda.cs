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
        void GetLambdaContent(PortableLambdaType expecting);
        void GetLambdaContent();
        void Finalize(PortableLambdaType expecting);
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

    public class ExpectingLambdaInfo
    {
        public PortableLambdaType Type { get; }
        public bool RegisterOccursLater { get; }
        private readonly List<ILambdaApplier> _apply = new List<ILambdaApplier>();

        public ExpectingLambdaInfo()
        {
            RegisterOccursLater = true;
        }

        public ExpectingLambdaInfo(PortableLambdaType type)
        {
            RegisterOccursLater = false;
            Type = type;
        }

        public void Apply(ILambdaApplier applier)
        {
            if (!RegisterOccursLater)
                // The arrow registration occurs now, parse the statement.
                applier.GetLambdaContent(Type);
            else
                // Otherwise, add it to the _apply list so we can apply it later.
                _apply.Add(applier);
        }

        public void FirstPass(PortableLambdaType type)
        {
            foreach (var apply in _apply) apply.GetLambdaContent(type);
        }

        public void FirstPass()
        {
            foreach (var apply in _apply) apply.GetLambdaContent();
        }

        public void SecondPass(PortableLambdaType type)
        {
            foreach (var apply in _apply) apply.Finalize(type);
        }

        public void SecondPass()
        {
            foreach (var apply in _apply) apply.Finalize(null);
        }
    }

    public class CheckLambdaContext
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
            if (ParseInfo.ExpectingLambda == null)
            {
                // Parameter data is known.
                if (ParameterState == ParameterState.CountAndTypesKnown)
                    Applier.GetLambdaContent();
                else
                    ParseInfo.Script.Diagnostics.Error(ErrorMessage, Range);
            }
            else
                ParseInfo.ExpectingLambda.Apply(Applier);
        }
    }

    public enum ParameterState
    {
        Unknown,
        CountKnown,
        CountAndTypesKnown
    }
}