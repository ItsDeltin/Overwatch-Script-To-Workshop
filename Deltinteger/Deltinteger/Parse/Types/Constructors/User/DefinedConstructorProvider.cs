using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class DefinedConstructorProvider : IConstructorProvider<DefinedConstructorInstance>, IApplyBlock, ISymbolLink
    {
        public string Name => TypeProvider.Name;
        public string SubroutineName { get; }
        public CallInfo CallInfo { get; }
        public Location DefinedAt { get; }
        public ICodeTypeInitializer TypeProvider { get; }
        public ParameterProvider[] ParameterProviders { get; private set; }
        public CodeType[] ParameterTypes { get; private set; }
        public BlockAction Block { get; private set; }
        public SubroutineInfo SubroutineInfo { get; set; }

        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly ConstructorContext _context;
        private readonly RecursiveCallHandler _recursiveCallHandler;
        private readonly ApplyBlock _applyBlock = new ApplyBlock();

        public DefinedConstructorProvider(ICodeTypeInitializer provider, ParseInfo parseInfo, Scope scope, ConstructorContext context)
        {
            _parseInfo = parseInfo;
            _scope = scope.Child();
            _context = context;
            TypeProvider = provider;

            _recursiveCallHandler = new RecursiveCallHandler(this, "constructor");
            CallInfo = new CallInfo(_recursiveCallHandler, parseInfo.Script);
            SubroutineName = context.SubroutineName?.Text.RemoveQuotes();
            DefinedAt = parseInfo.Script.GetLocation(context.LocationToken.Range);

            // TODO: Type provider link!
            // type.AddLink(DefinedAt);
            
            parseInfo.TranslateInfo.ApplyBlock(this);
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Constructor, DefinedAt.range));
        }

        public void SetupBlock()
        {
            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(_parseInfo, _scope, _context.Parameters, SubroutineName != null);
            ParameterTypes = ParameterProviders.Select(p => p.Type).ToArray();

            Block = new BlockAction(_parseInfo.SetCallInfo(CallInfo), _scope, _context.Block);
            _applyBlock.Apply();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied) => _applyBlock.OnBlockApply(onBlockApplied);
        
        public SubroutineInfo GetSubroutineInfo()
        {
            if (SubroutineInfo == null)
            {
                // TODO
                var determiner = new ConstructorDeterminer(GetInstance(null, InstanceAnonymousTypeLinker.Empty));
                var builder = new SubroutineBuilder(_parseInfo.TranslateInfo, determiner);
                builder.SetupSubroutine();
            }
            return SubroutineInfo;
        }
    
        public DefinedConstructorInstance GetInstance(CodeType typeInstance, InstanceAnonymousTypeLinker genericsLinker)
            => new DefinedConstructorInstance(typeInstance, this, genericsLinker, DefinedAt);

        MarkupBuilder ILabeled.GetLabel(DeltinScript deltinScript, LabelInfo labelInfo)
        {
            var builder = new MarkupBuilder();
            builder.StartCodeLine().Add("new " + Name + "(");

            for (int i = 0; i < ParameterProviders.Length; i++)
            {
                if (i != 0) builder.Add(", ");

                // Add the parameter type.
                if (labelInfo.IncludeParameterTypes)
                {
                    builder.Add(ParameterProviders[i].Type.GetName());

                    // Add a space if the name is also included.
                    if (labelInfo.IncludeParameterNames) builder.Add(" ");
                }

                // Add the parameter name.
                if (labelInfo.IncludeParameterNames)
                    builder.Add(ParameterProviders[i].Name);
            }

            return builder.Add(")").EndCodeLine();
        }
    }
}