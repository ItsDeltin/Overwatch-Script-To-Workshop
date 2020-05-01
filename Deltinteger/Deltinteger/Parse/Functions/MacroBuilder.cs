using Deltin.Deltinteger;
using Deltin.Deltinteger.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Parse.Functions
{
    class MacroBuilder
    {
        public static IWorkshopTree Call(MacroVar macro, ActionSet callerSet)
        {
            MacroBuilder builder = new MacroBuilder(null, callerSet);
            // Normal  
            builder.BuilderSet = builder.BuilderSet.PackThis();
            return builder.ParseInnerMacroVar(macro);
        }

        public static IWorkshopTree Call(DefinedMacro macro, MethodCall call, ActionSet callerSet)
        {
            MacroBuilder builder = new MacroBuilder(macro, callerSet);
            // Normal
            builder.BuilderSet = builder.BuilderSet.PackThis();
            builder.AssignParameters(call);
            return builder.ParseInner();
        }

        public DefinedMacro Macro { get; }
        public ActionSet BuilderSet { get; set; }


        public MacroBuilder(DefinedMacro macro, ActionSet builderSet)
        {
            Macro = macro;
            this.BuilderSet = builderSet;
        }

        public void AssignParameters(MethodCall methodCall)
        {
            Macro.AssignParameters(BuilderSet, methodCall.ParameterValues);
        }

        public IWorkshopTree ParseInner()
        {
            if (Macro.Attributes.WasOverriden) return ParseVirtual();
            else return Macro.Expression.Parse(BuilderSet);
        }

        public IWorkshopTree ParseInnerMacroVar(MacroVar macro)
        {
            if (macro.Attributes.WasOverriden) return ParseVirtualMacro(macro);
            else return macro.Expression.Parse(BuilderSet);
        }


        public IWorkshopTree ParseVirtual()
        {
            List<DefinedMacro> options = new List<DefinedMacro>();
            options.Add(Macro);

            options.AddRange(Array.ConvertAll(Macro.Attributes.AllOverrideOptions(), iMethod => (DefinedMacro)iMethod));

            List<IExpression> expressions = options.Select(m => m.Expression).ToList();
            //List<int> identifiers = options.Select(m =>((ClassType)m.Attributes.ContainingType).Identifier).ToList();

            List<IWorkshopTree> expElements = new List<IWorkshopTree>();

            foreach (var option in options)
            {
                var optionSet = BuilderSet.New(BuilderSet.IndexAssigner.CreateContained());
                option.Attributes.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                expElements.Add(option.Expression.Parse(optionSet));
            }

            var expArray = Element.CreateArray(expElements.ToArray());
            //var identArray = Element.CreateArray(identifiers.Select(i => new V_Number(i)).ToArray());

            ClassData classData = BuilderSet.Translate.DeltinScript.GetComponent<ClassData>();

            return Element.Part<V_ValueInArray>(expArray, BuilderSet.CurrentObject);
        }

        public IWorkshopTree ParseVirtualMacro(MacroVar macro)
        {
            List<MacroVar> options = new List<MacroVar>();
            options.Add(macro);

            options.AddRange(macro.Attributes.AllMacroOverrideOptions());

            List<IExpression> expressions = options.Select(m => m.Expression).ToList();
            List<int> identifiers = options.Select(m => ((ClassType)m.Attributes.ContainingType).Identifier).ToList();

            List<IWorkshopTree> expElements = new List<IWorkshopTree>();

            foreach (var option in options)
            {
                var optionSet = BuilderSet.New(BuilderSet.IndexAssigner.CreateContained());
                option.Attributes.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                expElements.Add(option.Expression.Parse(optionSet));
            }

            var expArray = Element.CreateArray(expElements.ToArray());
            //var identArray = Element.CreateArray(identifiers.Select(i => new V_Number(i)).ToArray());

            ClassData classData = BuilderSet.Translate.DeltinScript.GetComponent<ClassData>();

            return Element.Part<V_ValueInArray>(expArray, BuilderSet.CurrentObject);
        }
    }
}
