using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class DefinedConstructorProvider : IConstructorProvider<DefinedConstructorInstance>, IApplyBlock, IDeclarationKey, IGetContent
    {
        public string Name => TypeProvider.Name;
        public string SubroutineName { get; }
        public CallInfo CallInfo { get; }
        public Location DefinedAt { get; }
        public IDefinedTypeInitializer TypeProvider { get; }
        public ParameterProvider[] ParameterProviders { get; private set; }
        public CodeType[] ParameterTypes { get; private set; }
        public BlockAction Block { get; private set; }
        public ValueSolveSource ContentReady { get; } = new ValueSolveSource();

        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly ConstructorContext _context;
        private readonly RecursiveCallHandler _recursiveCallHandler;
        private readonly ApplyBlock _applyBlock = new ApplyBlock();

        public DefinedConstructorProvider(IDefinedTypeInitializer provider, ParseInfo parseInfo, Scope scope, ConstructorContext context)
        {
            _parseInfo = parseInfo;
            _scope = scope.Child(true);
            _context = context;
            TypeProvider = provider;

            _recursiveCallHandler = new RecursiveCallHandler(this, context.SubroutineName);
            CallInfo = new CallInfo(_recursiveCallHandler, parseInfo.Script, ContentReady);
            SubroutineName = context.SubroutineName?.Text.RemoveQuotes();
            DefinedAt = parseInfo.Script.GetLocation(context.ConstructorToken.Range);

            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(_parseInfo, _scope, _context.Parameters, SubroutineName != null);
            ParameterTypes = ParameterProviders.Select(p => p.Type).ToArray();
            
            parseInfo.TranslateInfo.StagedInitiation.On(this);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Constructor, DefinedAt.range));
            parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(context.ConstructorToken.Range, true));

            // Add the CallInfo to the recursion check.
            parseInfo.TranslateInfo.GetComponent<RecursionCheckComponent>().AddCheck(CallInfo);
        }

        public void GetContent()
        {
            Block = new BlockAction(_parseInfo.SetCallInfo(CallInfo).SetThisType(TypeProvider), _scope, _context.Block);
            _applyBlock.Apply();
            ContentReady.Set();
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