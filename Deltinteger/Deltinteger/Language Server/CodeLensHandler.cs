using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ICodeLensHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.ICodeLensHandler;
using CodeLensCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.CodeLensCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class CodeLensHandler : ICodeLensHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public CodeLensHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<CodeLensContainer> Handle(CodeLensParams request, CancellationToken cancellationToken)
        {
            _languageServer.DocumentHandler.WaitForNextUpdate();

            var codeLenses = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri)?.GetCodeLensRanges();
            if (codeLenses == null) return new CodeLensContainer();

            List<CodeLens> finalLenses = new List<CodeLens>();
            foreach (var lens in codeLenses)
                // Do not show references for scoped variables and parameters.
                if (lens.SourceType != CodeLensSourceType.ScopedVariable && lens.SourceType != CodeLensSourceType.ParameterVariable
                    // Check if the lens should be used.
                    && lens.ShouldUse() 
                    // Check if the code lens type is enabled.
                    && ((_languageServer.ConfigurationHandler.ReferencesCodeLens && lens is ReferenceCodeLensRange)
                    || (_languageServer.ConfigurationHandler.ImplementsCodeLens && lens is ImplementsCodeLensRange))
                    )
                    // Create the CodeLens.
                    finalLenses.Add(new CodeLens() {
                        Command = new Command() {
                            Title = lens.GetTitle(),
                            Name = lens.Command,
                            Arguments = lens.GetArguments()
                        },
                        Range = lens.Range.ToLsRange()
                    });
            
            return finalLenses;
        }

        public CodeLensRegistrationOptions GetRegistrationOptions()
        {
            return new CodeLensRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                ResolveProvider = false 
            };
        }

        private CodeLensCapability _capability;
        public void SetCapability(CodeLensCapability capability)
        {
            _capability = capability;
        }
    }
}