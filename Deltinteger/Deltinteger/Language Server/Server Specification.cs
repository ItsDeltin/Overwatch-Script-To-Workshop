namespace Deltin.Deltinteger.LanguageServer
{
    class InitializeResult
    {
        public ServerCapabilities capabilities;

        public InitializeResult(ServerCapabilities capabilities)
        {
            this.capabilities = capabilities;
        }
    }

    class ServerCapabilities
    {
        /// <summary>
        /// Defines how text documents are synced. Is either a detailed structure defining each notification or
        /// for backwards compatibility the TextDocumentSyncKind number.
        /// 
        /// Either a TextDocumentSyncOptions object or
        /// 0 = None,
        /// 1 = Full,
        /// 2 = Incremental.
        /// </summary>
        public object textDocumentSync;
        /// <summary>
        /// The server provides hover support.
        /// </summary>
        public bool? hoverProvider;
        /// <summary>
        /// The server provides completion support.
        /// </summary>
        public CompletionOptions completionProvider;
        /// <summary>
        /// The server provides signature help support.
        /// </summary>
        public SignatureHelpOptions signatureHelpProvider;
        /// <summary>
        /// The server provides goto definition support.
        /// </summary>
        public bool? definitionProvider;

        /// <summary>
        /// The server provides find references support.
        /// </summary>
        public bool? referencesProvider;
        /// <summary>
        /// The server provides document highlight support.
        /// </summary>
        public bool? documentHighlightProvider;
        /// <summary>
        /// The server provides document symbol support.
        /// </summary>
        public bool? documentSymbolProvider;
        /// <summary>
        /// The server provides workspace symbol support.
        /// </summary>
        public bool? workspaceSymbolProvider;
        /// <summary>
        /// The server provides code actions. CodeActionOptions may only be
        /// specified if the client states that it supports
        /// `codeActionLiteralSupport` in its initial `initialize` request.
        /// 
        /// Either a boolean or CodeActionOptions.
        /// </summary>
        public object codeActionProvider;
        /// <summary>
        /// The server provides code lens.
        /// </summary>
        public CodeLensOptions codeLensProvider;
        /// <summary>
        /// The server provides document formatting.
        /// </summary>
        public bool? documentFormattingProvider;
        /// <summary>
        /// The server provides document range formatting.
        /// </summary>
        public bool? documentRangeFormattingProvider;
        /// <summary>
        /// The server provides document formatting on typing.
        /// </summary>
        public OnTypeFormattingOptions documentOnTypeFormattingProvider;
        /// <summary>
        /// The server provides rename support. RenameOptions may only be
        /// specified if the client states that it supports
        /// `prepareSupport` in its initial `initialize` request.
        /// 
        /// Should either be a boolean or RenameOptions.
        /// </summary>
        public object renameProvider;
        /// <summary>
        /// The server provides document link support.
        /// </summary>
        public DocumentLinkOptions documentLinkProvider;
        /// <summary>
        /// The server provides execute command support.
        /// </summary>
        public ExecuteCommandOptions executeCommandProvider;
        /// <summary>
        /// Experimental server capabilities.
        /// </summary>
        public object experimental;
    }

    class TextDocumentSyncOptions
    {
        /// <summary>
        /// Open and close notifications are sent to the server.
        /// </summary>
        public bool openClose;
        /// <summary>
        /// Change notifications are sent to the server.
        /// 
        /// 0 = None,
        /// 1 = Full,
        /// 2 = Incremental.
        /// </summary>
        public int change;
        /// <summary>
        /// Will save notifications are sent to the server.
        /// </summary>
        public bool willSave;
        /// <summary>
        /// Will save wait until requests are sent to the server.
        /// </summary>
        public bool willSaveWaitUntil;
        /// <summary>
        /// Save notifications are sent to the server.
        /// </summary>
        public bool save;
    }

    class CompletionOptions
    {
        /// <summary>
        /// Most tools trigger completion request automatically without explicitly requesting
        /// it using a keyboard shortcut (e.g. Ctrl+Space). Typically they do so when the user
        /// starts to type an identifier. For example if the user types `c` in a JavaScript file
        /// code complete will automatically pop up present `console` besides others as a
        /// completion item. Characters that make up identifiers don't need to be listed here.
        ///
        /// If code complete should automatically be trigger on characters not being valid inside
        /// an identifier (for example `.` in JavaScript) list them in `triggerCharacters`.
        /// </summary>
        public string[] triggerCharacters;
        /// <summary>
        /// The server provides support to resolve additional
        /// information for a completion item.
        /// </summary>
        public bool resolveProvider;
    }

    class SignatureHelpOptions
    {
        /// <summary>
        /// The characters that trigger signature help
        /// automatically.
        /// </summary>
        public string[] triggerCharacters;
    }

    class CodeActionOptions
    {
        /// <summary>
        /// CodeActionKinds that this server may return.
        /// 
        /// The list of kinds may be generic, such as `CodeActionKind.Refactor`, or the server
        /// may list out every specific kind they provide.
        /// 
        /// Kinds are a hierarchical list of identifiers separated by `.`, e.g. `"refactor.extract.function"`.
        /// 
        /// The set of kinds is open and client needs to announce the kinds it supports to the server during
        /// initialization.
        /// </summary>
        public string[] codeActionKinds;
    }

    class CodeLensOptions
    {
        /// <summary>
        /// Code lens has a resolve provider as well.
        /// </summary>
        public bool resolveProvider;
    }

    class OnTypeFormattingOptions
    {
        /// <summary>
        /// A character on which formatting should be triggered, like `}`.
        /// </summary>
        public string firstTriggerCharacter;
        /// <summary>
        /// More trigger characters.
        /// </summary>
        public string[] moreTriggerCharacter;
    }

    class RenameOptions
    {
        /// <summary>
        /// Renames should be checked and tested before being executed.
        /// </summary>
        public bool prepareProvider;
    }

    class DocumentLinkOptions
    {
        /// <summary>
        /// Document links have a resolve provider as well.
        /// </summary>
        public bool resolveProvider;
    }

    class ExecuteCommandOptions
    {
        /// <summary>
        /// The commands to be executed on the server
        /// </summary>
        public string[] commands;
    }
}