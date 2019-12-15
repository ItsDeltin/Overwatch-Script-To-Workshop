using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CallMethodAction : IExpression, IStatement
    {
        private DeltinScript translateInfo { get; }
        public IMethod CallingMethod { get; }
        private OverloadChooser OverloadChooser { get; }
        private IExpression[] ParameterValues { get; }

        private DocRange NameRange { get; }

        public CallMethodAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.MethodContext methodContext)
        {
            this.translateInfo = translateInfo;
            string methodName = methodContext.PART().GetText();
            NameRange = DocRange.GetRange(methodContext.PART());

            var options = scope.GetMethodsByName(methodName);
            
            OverloadChooser = new OverloadChooser(options, script, translateInfo, scope, NameRange, DocRange.GetRange(methodContext), new OverloadError("method '" + methodName + "'"));

            if (methodContext.call_parameters() != null)
                OverloadChooser.SetContext(methodContext.call_parameters());
            else if (methodContext.picky_parameters() != null)
                OverloadChooser.SetContext(methodContext.picky_parameters());
            else
                OverloadChooser.SetContext();
            
            CallingMethod = (IMethod)OverloadChooser.Overload;
            ParameterValues = OverloadChooser.Values;
        }

        public Scope ReturningScope()
        {
            if (CallingMethod == null) return null;

            if (CallingMethod.ReturnType == null)
                return translateInfo.PlayerVariableScope;
            else
                return CallingMethod.ReturnType.GetObjectScope();
        }

        public CodeType Type() => CallingMethod?.ReturnType;
    
        // IStatement
        public void Translate(ActionSet actionSet)
        {
            CallingMethod.Parse(actionSet.New(NameRange), GetParameterValuesAsWorkshop(actionSet));
        }

        // IExpression
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return CallingMethod.Parse(actionSet.New(NameRange), GetParameterValuesAsWorkshop(actionSet));
        }

        private IWorkshopTree[] GetParameterValuesAsWorkshop(ActionSet actionSet)
        {
            if (ParameterValues == null) return new IWorkshopTree[0];

            IWorkshopTree[] parameterValues = new IWorkshopTree[ParameterValues.Length];
            for (int i = 0; i < ParameterValues.Length; i++)
                parameterValues[i] = ParameterValues[i].Parse(actionSet);
            return parameterValues;
        }
    }
}