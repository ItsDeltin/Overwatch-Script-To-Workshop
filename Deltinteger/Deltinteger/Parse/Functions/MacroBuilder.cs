using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    abstract class AbstractMacroBuilder
    {
        public static IWorkshopTree Call(ActionSet actionSet, MacroVarInstance macro)
        {
            MacroVarBuilder builder = new MacroVarBuilder(actionSet, macro);
            builder.AssignParameters();
            return builder.ParseInner();
        }

        public static IWorkshopTree Call(ActionSet actionSet, DefinedMacroInstance macro, MethodCall call)
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
            List<int> resolves = new List<int>();
            List<int> identifiers = new List<int>();
            bool needsResolve = false;

            foreach (var option in options)
            {
                var optionSet = ActionSet.New(ActionSet.IndexAssigner.CreateContained());
                option.Type().AddObjectVariablesToAssigner(optionSet.ToWorkshop, optionSet.CurrentObject, optionSet.IndexAssigner);

                int currentIndex = expElements.Count;
                expElements.Add(option.Parse(optionSet));
                resolves.Add(currentIndex);
                identifiers.Add(((ClassType)option.Type()).Identifier);

                // Iterate through every type.
                foreach (CodeType type in ActionSet.Translate.DeltinScript.Types.AllTypes)
                    // If 'type' does not equal the current virtual option's containing class...
                    if (option.Type() != type
                        // ... and 'type' is 'ClassType'...
                        && type is ClassType
                        // ...and 'type' implements the containing class...
                        && type.Implements(option.Type())
                        )// TODO ...and 'type' does not have their own function implementation...
                        // && Deltin.Deltinteger.Parse.FunctionBuilder.VirtualLookupTable.AutoImplemented(option.Type(), options.Select(option => option.Type()).ToArray(), type))
                    // ...then add an additional case for 'type's class identifier.
                    {
                        needsResolve = true;
                        resolves.Add(currentIndex);
                        identifiers.Add(((ClassType)type).Identifier);
                    }
            }

            Element expArray = Element.CreateArray(expElements.ToArray());
            Element resolveArray = Element.CreateArray(resolves.Select(i => Element.Num(i)).ToArray());
            Element identArray = Element.CreateArray(identifiers.Select(i => Element.Num(i)).ToArray());

            ClassData classData = ActionSet.Translate.DeltinScript.GetComponent<ClassData>();
            Element classIdentifier = classData.ClassIndexes.Get()[ActionSet.CurrentObject];

            /*
            class A // Class identifier: 5
            {
                virtual define Macro: 2;
            }
            class B : A // Class identifier: 6
            {
                override define Macro: 3;
            }
            * In this case, the macro can be resolved like so:
            [2, 3][Index Of([5, 6], class id)]
            * This output will not work in this case:
            class A // Class identifier: 5
            {
                virtual define Macro: 2;
            }
            class B : A // Class identifier: 6
            {
                override define Macro: 3;
            }
            class C : B // Class identifier: 7
            {
            }
            * C does not implement 'Macro'. This will cause the output to be the same as above.
            * This is not correct. Since C implements B, it should return 3. Since Index Of returns -1, the macro will return 0 if 'new C().Macro' is called.
            * If this case happens, 'needsResolve' will be true. When it is true, do this instead:
            [2, 3][0, 1, 1][IndexOf([5, 6, 7], class id)]
            * '[0, 1, 1]' will map the index to the correct macro value.
            */

            if (needsResolve)
                return expArray[resolveArray[Element.IndexOfArrayValue(identArray, classIdentifier)]];
            else
                return expArray[Element.IndexOfArrayValue(identArray, classIdentifier)];
        }

        protected abstract IWorkshopTree ParseDefault();
        protected abstract IMacroOption[] AllOptions();
        protected abstract bool WasOverriden { get; }
    }

    class MacroBuilder : AbstractMacroBuilder
    {
        private readonly DefinedMacroInstance _macro;
        private readonly MethodCall _call;

        public MacroBuilder(ActionSet actionSet, DefinedMacroInstance macro, MethodCall call) : base(actionSet)
        {
            _macro = macro;
            _call = call;
        }

        public override void AssignParameters()
        {
            for (int i = 0; i < _macro.Parameters.Length; i++)
            {
                IGettable result = ActionSet.IndexAssigner.Add(_macro.Provider.ParameterProviders[i].Var, _call.ParameterValues[i]);

                // TODO: Virtual var group
                // foreach (Var virtualParameterOption in VirtualVarGroup(i))
                //     actionSet.IndexAssigner.Add(virtualParameterOption, result);
            }
            base.AssignParameters();
        }

        protected override IWorkshopTree ParseDefault() => _macro.Provider.Expression.Parse(ActionSet);
        protected override bool WasOverriden => _macro.Attributes.WasOverriden;
        protected override IMacroOption[] AllOptions()
        {
            List<IMacroOption> options = new List<IMacroOption>();
            options.Add(new ParameterMacroOption(_macro, _call));
            options.AddRange(_macro.Attributes.AllOverrideOptions().Select(option => new ParameterMacroOption((DefinedMacroInstance)option, _call)));
            return options.ToArray();
        }
    }

    class MacroVarBuilder : AbstractMacroBuilder
    {
        private readonly MacroVarInstance _macro;

        public MacroVarBuilder(ActionSet builderSet, MacroVarInstance macro) : base(builderSet)
        {
            _macro = macro;
        }
        protected override IWorkshopTree ParseDefault() => _macro.Provider.Value.Parse(ActionSet);
        // TODO
        protected override bool WasOverriden => false;
        // protected override bool WasOverriden => _macro.Overriders.Count > 0;
        protected override IMacroOption[] AllOptions()
        {
            List<IMacroOption> options = new List<IMacroOption>();
            options.Add(new MacroVarOption(_macro));
            // TODO
            // options.AddRange(_macro.AllMacroOverrideOptions().Select(option => new MacroVarOption(option)));
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
        private readonly DefinedMacroInstance _macro;
        private readonly MethodCall _methodCall;

        public ParameterMacroOption(DefinedMacroInstance macro, MethodCall methodCall)
        {
            _macro = macro;
            _methodCall = methodCall;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => _macro.Provider.Expression.Parse(actionSet);
        public CodeType Type() => _macro.Attributes.ContainingType;
    }

    class MacroVarOption : IMacroOption
    {
        private readonly MacroVarInstance _macroVar;
        
        public MacroVarOption(MacroVarInstance macroVar)
        {
            _macroVar = macroVar;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => _macroVar.Provider.Value.Parse(actionSet);
        public CodeType Type() => _macroVar.ContainingType;
    }
}
