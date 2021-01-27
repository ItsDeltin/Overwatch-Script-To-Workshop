using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Parse.Types.Constructors;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Constructor : IParameterCallable, ICallable, ISymbolLink
    {
        public string Name => Type.Name;
        public AccessLevel AccessLevel { get; }
        public CodeParameter[] Parameters { get; protected set; }
        public LanguageServer.Location DefinedAt { get; }
        public CodeType Type { get; }
        public MarkupBuilder Documentation { get; set; }

        public Constructor(CodeType type, LanguageServer.Location definedAt, AccessLevel accessLevel)
        {
            Type = type;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public virtual void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) { }

        public virtual void Call(ParseInfo parseInfo, DocRange callRange) { }

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo)
        {
            var builder = new MarkupBuilder().StartCodeLine().Add("new " + Type.GetName());
            builder.Add(CodeParameter.GetLabels(deltinScript, Parameters)).EndCodeLine();

            if (labelInfo.IncludeDocumentation)
                builder.NewSection().Add(Documentation);

            return builder.EndCodeLine();
        }
    }

    interface ISubroutineSaver : IFunctionHandler
    {
        void SetSubroutineInfo(SubroutineInfo subroutineInfo);
    }

    class ConstructorDeterminer : IGroupDeterminer, IFunctionLookupTable, ISubroutineContext
    {
        private readonly ISubroutineSaver _function;

        public ConstructorDeterminer(ISubroutineSaver function)
        {
            _function = function;
        }
        public ConstructorDeterminer(DefinedConstructorInstance constructor) : this(new DefinedConstructorHandler(constructor)) { }

        // IGroupDeterminer
        public IFunctionLookupTable GetLookupTable() => this;
        public string GroupName() => _function.GetName();
        public bool IsObject() => true;
        public bool IsRecursive() => false;
        public bool IsVirtual() => false;
        public bool IsSubroutine() => _function.IsSubroutine();
        public SubroutineInfo GetSubroutineInfo() => _function.GetSubroutineInfo();
        public bool MultiplePaths() => false;
        public bool ReturnsValue() => false;
        public object GetStackIdentifier() => _function.UniqueIdentifier();
        public RecursiveStack GetExistingRecursiveStack(List<RecursiveStack> stack) => null;
        public IParameterHandler[] Parameters() => DefinedParameterHandler.GetDefinedParameters(_function.ParameterCount(), new IFunctionHandler[] { _function }, false);

        // IFunctionLookupTable
        public void Build(FunctionBuildController builder) => _function.ParseInner(builder.ActionSet);

        // ISubroutineContext
        public string RuleName() => "Constuctor " + GroupName();
        public string ElementName() => GroupName();
        public string ThisArrayName() => GroupName();
        public bool VariableGlobalDefault() => true;
        public CodeType ContainingType() => _function.ContainingType;
        public void Finish(Rule rule) { }
        public IGroupDeterminer GetDeterminer() => this;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _function.SetSubroutineInfo(subroutineInfo);
    }

    class DefinedConstructorHandler : ISubroutineSaver
    {
        private readonly DefinedConstructorInstance _constructor;

        public DefinedConstructorHandler(DefinedConstructorInstance constructor)
        {
            _constructor = constructor;
        }

        public CodeType ContainingType => _constructor.Type;
        public string GetName() => _constructor.Name;
        public bool DoesReturnValue() => false;
        public bool IsObject() => false;
        public bool IsRecursive() => false;
        public bool MultiplePaths() => false;
        public IVariableInstance GetParameterVar(int index) => _constructor.ParameterVars[index];
        public int ParameterCount() => _constructor.Parameters.Length;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _constructor.Provider.SubroutineInfo = subroutineInfo;

        public bool IsSubroutine() => _constructor.Provider.SubroutineName != null;
        public SubroutineInfo GetSubroutineInfo() => _constructor.Provider.GetSubroutineInfo();
        public void ParseInner(ActionSet actionSet) => _constructor.Provider.Block.Translate(actionSet.PackThis());

        public object UniqueIdentifier() => _constructor;
    }
}