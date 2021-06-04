using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse.Functions.Builder.Virtual;

namespace Deltin.Deltinteger.Parse
{
    public class VariableInstance : IVariableInstance
    {
        public string Name => _var.Name;
        public CodeType CodeType { get; }
        public bool WholeContext => _var.WholeContext;
        public LanguageServer.Location DefinedAt => _var.DefinedAt;
        public AccessLevel AccessLevel => _var.AccessLevel;
        IVariable IVariableInstance.Provider => _var;
        public MarkupBuilder Documentation { get; set; }
        ICodeTypeSolver IScopeable.CodeType => CodeType;
        public IVariableInstanceAttributes Attributes { get; }

        readonly Var _var;
        readonly CodeType _definedIn;

        public VariableInstance(Var var, InstanceAnonymousTypeLinker instanceInfo, CodeType definedIn)
        {
            _var = var;
            CodeType = var.CodeType.GetRealType(instanceInfo);
            _definedIn = definedIn;
            Attributes = new VariableInstanceAttributes()
            {
                CanBeSet = var.StoreType != StoreType.None,
                StoreType = var.StoreType,
                UseDefaultVariableAssigner = !var.IsMacro
            };
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            IVariableInstance.Call(this, parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, _var.DefinedAt);
        }

        public IGettableAssigner GetAssigner(ActionSet actionSet) => CodeType.GetRealType(actionSet?.ThisTypeLinker).GetGettableAssigner(new AssigningAttributes() {
            Name = _var.Name,
            Extended = _var.InExtendedCollection,
            ID = _var.ID,
            IsGlobal = actionSet?.IsGlobal ?? true,
            StoreType = _var.StoreType,
            VariableType = _var.VariableType,
            DefaultValue = _var.InitialValue
        });

        public IWorkshopTree ToWorkshop(ActionSet actionSet)
        {
            if (!_var.IsMacro)
                return actionSet.IndexAssigner.Get(_var).GetVariable();
            else
                return ToMacro(actionSet);
        }

        IWorkshopTree ToMacro(ActionSet actionSet)
        {
            var allMacros = new List<VariableInstanceOption>();
            allMacros.Add(new VariableInstanceOption(this));

            // Get the class relation.
            if (_definedIn != null)
            {
                var relation = actionSet.ToWorkshop.ClassInitializer.RelationFromClassType((ClassType)_definedIn);

                // Extract the virtual functions.
                allMacros.AddRange(relation.ExtractOverridenElements<VariableInstance>(extender => extender.Name == Name)
                    .Select(extender => new VariableInstanceOption(extender)));
            }

            return new MacroContentBuilder(actionSet, allMacros).Value;
        }

        class VariableInstanceOption : IMacroOption
        {
            readonly VariableInstance _variableInstance;
            public VariableInstanceOption(VariableInstance variableInstance) => _variableInstance = variableInstance;
            public ClassType ContainingType() => (ClassType)_variableInstance._definedIn;
            public IWorkshopTree GetValue(ActionSet actionSet) => _variableInstance._var.InitialValue.Parse(actionSet);
        }
    }
}