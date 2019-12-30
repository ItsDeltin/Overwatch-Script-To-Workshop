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
        public IExpression[] ParameterValues { get; }

        private DocRange NameRange { get; }

        public CallMethodAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.MethodContext methodContext, bool usedAsExpression, Scope getter)
        {
            this.translateInfo = parseInfo.TranslateInfo;
            string methodName = methodContext.PART().GetText();
            NameRange = DocRange.GetRange(methodContext.PART());

            var options = scope.GetMethodsByName(methodName);
            if (options.Length == 0)
                parseInfo.Script.Diagnostics.Error($"No method by the name of '{methodName}' exists in the current context.", NameRange);
            else
            {
                OverloadChooser = new OverloadChooser(options, parseInfo, getter, NameRange, DocRange.GetRange(methodContext), new OverloadError("method '" + methodName + "'"));

                if (methodContext.call_parameters() != null)
                    OverloadChooser.SetContext(methodContext.call_parameters());
                else if (methodContext.picky_parameters() != null)
                    OverloadChooser.SetContext(methodContext.picky_parameters());
                else
                    OverloadChooser.SetContext();
            
                CallingMethod = (IMethod)OverloadChooser.Overload;
                ParameterValues = OverloadChooser.Values;

                if (CallingMethod != null)
                {
                    if (CallingMethod is DefinedFunction)
                        ((DefinedFunction)CallingMethod).Call(parseInfo.Script, NameRange);
                    
                    parseInfo.Script.AddHover(DocRange.GetRange(methodContext), CallingMethod.GetLabel(true));
                    
                    if (usedAsExpression && !CallingMethod.DoesReturnValue())
                        parseInfo.Script.Diagnostics.Error("The chosen overload for " + methodName + " does not return a value.", NameRange);
                }
            }
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
            CallingMethod.Parse(actionSet.New(NameRange), GetParameterValuesAsWorkshop(actionSet), OverloadChooser.AdditionalParameterData);
        }

        // IExpression
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return CallingMethod.Parse(actionSet.New(NameRange), GetParameterValuesAsWorkshop(actionSet), OverloadChooser.AdditionalParameterData);
        }

        private IWorkshopTree[] GetParameterValuesAsWorkshop(ActionSet actionSet)
        {
            if (ParameterValues == null) return new IWorkshopTree[0];

            IWorkshopTree[] parameterValues = new IWorkshopTree[ParameterValues.Length];
            for (int i = 0; i < ParameterValues.Length; i++)
                parameterValues[i] = OverloadChooser.Overload.Parameters[i].Parse(actionSet, ParameterValues[i], OverloadChooser.Overload.Parameters[i].Type == null);
            return parameterValues;
        }
    }
}