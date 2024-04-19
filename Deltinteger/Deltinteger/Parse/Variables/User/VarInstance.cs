using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse.Functions.Builder.Virtual;

namespace Deltin.Deltinteger.Parse
{
    public class VariableInstance : IVariableInstance
    {
        public string Name => Var.Name;
        public CodeType CodeType { get; }
        public bool WholeContext => Var.WholeContext;
        public LanguageServer.Location DefinedAt => Var.DefinedAt;
        public AccessLevel AccessLevel => Var.AccessLevel;
        IVariable IVariableInstance.Provider => Var;
        public MarkupBuilder Documentation => Var.Documentation;
        ICodeTypeSolver IScopeable.CodeType => CodeType;
        public IVariableInstanceAttributes Attributes { get; }

        public Var Var { get; }
        readonly CodeType _definedIn;

        public VariableInstance(Var var, InstanceAnonymousTypeLinker instanceInfo, CodeType definedIn)
        {
            Var = var;
            CodeType = var.CodeType.GetRealType(instanceInfo);
            _definedIn = definedIn;
            Attributes = new VariableInstanceAttributes()
            {
                CanBeSet = var.StoreType != StoreType.None,
                StoreType = var.StoreType,
                UseDefaultVariableAssigner = !var.IsMacro,
                ContainingType = definedIn
            };
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            IVariableInstance.Call(this, parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, Var.DefinedAt);
        }

        public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner) => CodeType.GetRealType(getAssigner.TypeLinker).GetGettableAssigner(new AssigningAttributes()
        {
            Name = (getAssigner.Tag ?? string.Empty) + Var.Name,
            Extended = Var.InExtendedCollection,
            ID = Var.ID,
            IsGlobal = getAssigner.IsGlobal,
            StoreType = Var.StoreType,
            VariableType = Var.VariableType,
            DefaultValue = IVariableDefault.FromExpression(Var.InitialValue),
            Persist = Var.Persist || getAssigner.Persist,
            TargetVariable = getAssigner.TargetVariable ?? Var.LinkTargetVanilla
        });

        public IWorkshopTree ToWorkshop(ActionSet actionSet)
        {
            if (!Var.IsMacro)
                return actionSet.IndexAssigner.Get(Var).GetVariable();
            else
            {
                if (actionSet.IndexAssigner.TryGet(Var, out IGettable gettable))
                    return gettable.GetVariable();
                else
                    return ToMacro(actionSet);
            }
        }

        IWorkshopTree ToMacro(ActionSet actionSet)
        {
            var allMacros = new List<VariableInstanceOption>();
            allMacros.Add(new VariableInstanceOption(this));

            // Get the class relation.
            if (_definedIn is not null && _definedIn.GetRealType(actionSet.ThisTypeLinker) is ClassType definedInClassType)
            {
                var relation = actionSet.ToWorkshop.ClassInitializer.RelationFromClassType(definedInClassType);

                // Extract the virtual functions.
                allMacros.AddRange(relation.ExtractOverridenElements<VariableInstance>(extender => extender.Name == Name)
                    .Select(extender => new VariableInstanceOption(extender)));
            }

            return new MacroContentBuilder(actionSet.PackThis(), allMacros).Value;
        }

        class VariableInstanceOption : IMacroOption
        {
            readonly VariableInstance _variableInstance;
            public VariableInstanceOption(VariableInstance variableInstance) => _variableInstance = variableInstance;
            public ClassType ContainingType() => (ClassType)_variableInstance._definedIn;
            public IWorkshopTree GetValue(ActionSet actionSet) => _variableInstance.Var.InitialValue.Parse(actionSet);
        }
    }
}