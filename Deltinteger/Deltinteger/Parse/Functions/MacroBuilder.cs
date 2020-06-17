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

            //If there are no overrides, don't bother creating the lookup table
            if (options.Count == 1)
            {
                return Macro.Expression.Parse(BuilderSet);
            }

            List<IExpression> expressions = new List<IExpression>();
            List<int> identifiers = new List<int>();

            List<IWorkshopTree> expElements = new List<IWorkshopTree>();

            foreach (var option in options)
            {
                var optionSet = BuilderSet.New(BuilderSet.IndexAssigner.CreateContained());
                option.Attributes.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                expElements.Add(option.Expression.Parse(optionSet));
                identifiers.Add(((ClassType)option.Attributes.ContainingType).Identifier);

            }

            var expArray = Element.CreateArray(expElements.ToArray());
            var identArray = Element.CreateArray(identifiers.Select(i => new V_Number(i)).ToArray());

            ClassData classData = BuilderSet.Translate.DeltinScript.GetComponent<ClassData>();

            return expArray[Element.Part<V_IndexOfArrayValue>(identArray, Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), BuilderSet.CurrentObject))];
        }

        public IWorkshopTree ParseVirtualMacro(MacroVar macro)
        {
            List<MacroVar> options = new List<MacroVar>();
            options.Add(macro);

            options.AddRange(macro.Attributes.AllMacroOverrideOptions());


            List<IWorkshopTree> expElements = new List<IWorkshopTree>();
            List<int> identifiers = new List<int>();

            //If there are no overrides, don't bother creating the lookup table
            if (options.Count == 1)
            {
                return macro.Expression.Parse(BuilderSet);
            }

            foreach (var option in options)
            {
                var optionSet = BuilderSet.New(BuilderSet.IndexAssigner.CreateContained());
                option.Attributes.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                expElements.Add(option.Expression.Parse(optionSet));
                identifiers.Add(((ClassType)option.Attributes.ContainingType).Identifier);

            }

            var expArray = Element.CreateArray(expElements.ToArray());
            var identArray = Element.CreateArray(identifiers.Select(i => new V_Number(i)).ToArray());

            ClassData classData = BuilderSet.Translate.DeltinScript.GetComponent<ClassData>();

            return expArray[Element.Part<V_IndexOfArrayValue>(identArray, Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), BuilderSet.CurrentObject))];
        }
    }
}
