using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Constructor : IParameterCallable, ICallable
    {
        public string Name => Type.Name;
        public AccessLevel AccessLevel { get; }
        public CodeParameter[] Parameters { get; protected set; }
        public LanguageServer.Location DefinedAt { get; }
        public CodeType Type { get; }
        public string Documentation { get; set; }

        public Constructor(CodeType type, LanguageServer.Location definedAt, AccessLevel accessLevel)
        {
            Type = type;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public virtual void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) {}

        public virtual void Call(ParseInfo parseInfo, DocRange callRange) {}

        public string GetLabel(bool markdown) => HoverHandler.GetLabel("new " + Type.Name, Parameters, markdown, Documentation);
    }

    public class DefinedConstructor : Constructor, IApplyBlock
    {
        public Var[] ParameterVars { get; private set; }
        public Scope ConstructorScope { get; }
        public BlockAction Block { get; private set; }

        private ParseInfo parseInfo { get; }
        private ConstructorContext context { get; }
        public string SubroutineName { get; }

        public CallInfo CallInfo { get; }

        private readonly RecursiveCallHandler _recursiveCallHandler;
        public SubroutineInfo SubroutineInfo { get; set; }

        public DefinedConstructor(ParseInfo parseInfo, Scope scope, CodeType type, ConstructorContext context) : base(
            type,
            new LanguageServer.Location(parseInfo.Script.Uri, context.LocationToken.Range),
            context.Attributes.GetAccessLevel())
        {
            this.parseInfo = parseInfo;
            this.context = context;
            _recursiveCallHandler = new RecursiveCallHandler(this, "constructor");
            CallInfo = new CallInfo(_recursiveCallHandler, parseInfo.Script);
            SubroutineName = context.SubroutineName?.Text.RemoveQuotes();

            ConstructorScope = scope.Child();

            if (Type is DefinedType)
                ((DefinedType)Type).AddLink(DefinedAt);
            
            parseInfo.TranslateInfo.ApplyBlock(this);
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Constructor, DefinedAt.range));
        }

        public void SetupParameters()
        {
            var parameterInfo = CodeParameter.GetParameters(parseInfo, ConstructorScope, context.Parameters, false);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;

            parseInfo.Script.AddHover(context.LocationToken.Range, GetLabel(true));
        }

        public void SetupBlock()
        {
            Block = new BlockAction(parseInfo.SetCallInfo(CallInfo), ConstructorScope, context.Block);
            foreach (var listener in listeners) listener.Applied();
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            var builder = new FunctionBuildController(actionSet.PackThis(), new CallHandler(parameterValues), new ConstructorDeterminer(this));
            builder.Call();
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.CurrentCallInfo?.Call(_recursiveCallHandler, callRange);
            ((DefinedType)Type).AddLink(parseInfo.GetLocation(callRange));
        }

        private List<IOnBlockApplied> listeners = new List<IOnBlockApplied>();
        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            listeners.Add(onBlockApplied);
        }

        public SubroutineInfo GetSubroutineInfo()
        {
            if (SubroutineInfo == null)
            {
                var determiner = new ConstructorDeterminer(this);
                var builder = new SubroutineBuilder(parseInfo.TranslateInfo, determiner);
                builder.SetupSubroutine();
            }
            return SubroutineInfo;
        }
    }

    interface ISubroutineSaver : IClassFunctionHandler
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
        public ConstructorDeterminer(DefinedConstructor constructor) : this(new DefinedConstructorHandler(constructor)) {}

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
        public object GetStackIdentifier() => _function.StackIdentifier();
        public NewRecursiveStack GetExistingRecursiveStack(List<NewRecursiveStack> stack) => null;
        public IParameterHandler[] Parameters() => DefinedParameterHandler.GetDefinedParameters(_function.ParameterCount(), new IFunctionHandler[] { _function }, false);

        // IFunctionLookupTable
        public void Build(FunctionBuildController builder) => _function.ParseInner(builder.ActionSet);

        // ISubroutineContext
        public string RuleName() => "Constuctor " + GroupName();
        public string ElementName() => GroupName();
        public string ThisArrayName() => GroupName();
        public bool VariableGlobalDefault() => true;
        public CodeType ContainingType() => _function.ContainingType;
        public void Finish(Rule rule) {}
        public IGroupDeterminer GetDeterminer() => this;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _function.SetSubroutineInfo(subroutineInfo);
    }

    class DefinedConstructorHandler : ISubroutineSaver
    {
        private readonly DefinedConstructor _constructor;

        public DefinedConstructorHandler(DefinedConstructor constructor)
        {
            _constructor = constructor;
        }

        public CodeType ContainingType => _constructor.Type;
        public string GetName() => _constructor.Name;
        public bool DoesReturnValue() => false;
        public bool IsObject() => false;
        public bool IsRecursive() => false;
        public bool MultiplePaths() => false;
        public IIndexReferencer GetParameterVar(int index) => _constructor.ParameterVars[index];
        public int ParameterCount() => _constructor.Parameters.Length;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _constructor.SubroutineInfo = subroutineInfo;

        public bool IsSubroutine() => _constructor.SubroutineName != null;
        public SubroutineInfo GetSubroutineInfo() => _constructor.GetSubroutineInfo();
        public void ParseInner(ActionSet actionSet) => _constructor.Block.Translate(actionSet.PackThis());

        public object StackIdentifier() => _constructor;
    }
}