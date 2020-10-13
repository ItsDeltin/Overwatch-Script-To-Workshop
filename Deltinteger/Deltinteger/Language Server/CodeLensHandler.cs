using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ICodeLensHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.ICodeLensHandler;
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
            await _languageServer.DocumentHandler.WaitForParse();
            var codeLenses = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.GetCodeLensRanges();
            if (codeLenses == null) return new CodeLensContainer();

            List<CodeLens> finalLenses = new List<CodeLens>();
            foreach (var lens in codeLenses)
                // Do not show references for scoped variables and parameters.
                if (lens.SourceType != CodeLensSourceType.ScopedVariable && lens.SourceType != CodeLensSourceType.ParameterVariable
                    // Check if the lens should be used.
                    && lens.ShouldUse() 
                    // Check if the code lens type is enabled.
                    && LensIsEnabled(lens))
                    // Create the CodeLens.
                    finalLenses.Add(new CodeLens() {
                        Command = new Command() {
                            Title = lens.GetTitle(),
                            Name = lens.Command,
                            Arguments = lens.GetArguments()
                        },
                        Range = lens.Range
                    });
            
            return finalLenses;
        }

        public bool LensIsEnabled(CodeLensRange lens)
        {
            return
                (_languageServer.ConfigurationHandler.ReferencesCodeLens   && lens is ReferenceCodeLensRange ) ||
                (_languageServer.ConfigurationHandler.ImplementsCodeLens   && lens is ImplementsCodeLensRange) ||
                (_languageServer.ConfigurationHandler.ElementCountCodeLens && lens is ElementCountCodeLens);
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