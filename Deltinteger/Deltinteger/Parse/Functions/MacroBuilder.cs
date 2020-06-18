using Deltin.Deltinteger;
using Deltin.Deltinteger.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Parse.Functions
{
    abstract class AbstractMacroBuilder
    {
        public static IWorkshopTree Call(ActionSet actionSet, MacroVar macro)
        {
            MacroVarBuilder builder = new MacroVarBuilder(actionSet, macro);
            builder.AssignParameters();
            return builder.ParseInner();
        }

        public static IWorkshopTree Call(ActionSet actionSet, DefinedMacro macro, MethodCall call)
        {
            MacroBuilder builder = new MacroBuilder(actionSet, macro, call);
            builder.AssignParameters();
            return builder.ParseInner();
        }

        public ActionSet ActionSet { get; set; }
        public AbstractMacroBuilder(ActionSet actionSet)
        {
            this.ActionSet = actionSet;
        }

        public virtual void AssignParameters()
        {
            ActionSet = ActionSet.PackThis();
        }

        public IWorkshopTree ParseInner()
        {
            if (WasOverriden) return ParseVirtual();
            else return ParseDefault();
        }

        public IWorkshopTree ParseVirtual()
        {
            IMacroOption[] options = AllOptions();

            //If there are no overrides, don't bother creating the lookup table
            if (options.Length == 1)
                return options[0].Parse(ActionSet);

            List<IWorkshopTree> expElements = new List<IWorkshopTree>();
            List<int> identifiers = new List<int>();

            foreach (var option in options)
            {
                var optionSet = ActionSet.New(ActionSet.IndexAssigner.CreateContained());
                option.Type().AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                expElements.Add(option.Parse(optionSet));
                identifiers.Add(((ClassType)option.Type()).Identifier);
            }

            var expArray = Element.CreateArray(expElements.ToArray());
            var identArray = Element.CreateArray(identifiers.Select(i => new V_Number(i)).ToArray());

            ClassData classData = ActionSet.Translate.DeltinScript.GetComponent<ClassData>();
            return expArray[Element.Part<V_IndexOfArrayValue>(identArray, Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), ActionSet.CurrentObject))];
        }

        protected abstract IWorkshopTree ParseDefault();
        protected abstract IMacroOption[] AllOptions();
        protected abstract bool WasOverriden { get; }
    }

    class MacroBuilder : AbstractMacroBuilder
    {
        private readonly DefinedMacro _macro;
        private readonly MethodCall _call;

        public MacroBuilder(ActionSet actionSet, DefinedMacro macro, MethodCall call) : base(actionSet)
        {
            _macro = macro;
            _call = call;
        }

        public override void AssignParameters()
        {
            _macro.AssignParameters(ActionSet, _call.ParameterValues);
            base.AssignParameters();
        }

        protected override IWorkshopTree ParseDefault() => _macro.Expression.Parse(ActionSet);
        protected override bool WasOverriden => _macro.Attributes.WasOverriden;
        protected override IMacroOption[] AllOptions()
        {
            List<IMacroOption> options = new List<IMacroOption>();
            options.Add(new ParameterMacroOption(_macro, _call));
            options.AddRange(_macro.Attributes.AllOverrideOptions().Select(option => new ParameterMacroOption((DefinedMacro)option, _call)));
            return options.ToArray();
        }
    }

    class MacroVarBuilder : AbstractMacroBuilder
    {
        private readonly MacroVar _macro;

        public MacroVarBuilder(ActionSet builderSet, MacroVar macro) : base(builderSet)
        {
            _macro = macro;
        }
        protected override IWorkshopTree ParseDefault() => _macro.Expression.Parse(ActionSet);
        protected override bool WasOverriden => _macro.Overriders.Count > 0;
        protected override IMacroOption[] AllOptions()
        {
            List<IMacroOption> options = new List<IMacroOption>();
            options.Add(new MacroVarOption(_macro));
            options.AddRange(_macro.AllMacroOverrideOptions().Select(option => new MacroVarOption(option)));
            return options.ToArray();
        }
    }

    // Macro Option resolver
    interface IMacroOption
    {
        IWorkshopTree Parse(ActionSet actionSet);
        CodeType Type(); 
    }

    class ParameterMacroOption : IMacroOption
    {
        private readonly DefinedMacro _macro;
        private readonly MethodCall _methodCall;

        public ParameterMacroOption(DefinedMacro macro, MethodCall methodCall)
        {
            _macro = macro;
            _methodCall = methodCall;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => _macro.Expression.Parse(actionSet);
        public CodeType Type() => _macro.Attributes.ContainingType;
    }

    class MacroVarOption : IMacroOption
    {
        private readonly MacroVar _macroVar;
        
        public MacroVarOption(MacroVar macroVar)
        {
            _macroVar = macroVar;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => _macroVar.Expression.Parse(actionSet);
        public CodeType Type() => _macroVar.ContainingType;
    }
}
