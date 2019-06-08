"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const vscode_languageserver_1 = require("vscode-languageserver");
//import { request } from 'http';
// Create a connection for the server. The connection uses Node's IPC as a transport.
// Also include all preview / proposed LSP features.
let connection = vscode_languageserver_1.createConnection(vscode_languageserver_1.ProposedFeatures.all);
// Create a simple text document manager. The text document manager
// supports full document sync only
let documents = new vscode_languageserver_1.TextDocuments();
let hasConfigurationCapability = false;
let hasWorkspaceFolderCapability = false;
let hasDiagnosticRelatedInformationCapability = false;
connection.onInitialize((params) => {
    let capabilities = params.capabilities;
    // Does the client support the `workspace/configuration` request?
    // If not, we will fall back using global settings
    hasConfigurationCapability =
        capabilities.workspace && !!capabilities.workspace.configuration;
    hasWorkspaceFolderCapability =
        capabilities.workspace && !!capabilities.workspace.workspaceFolders;
    hasDiagnosticRelatedInformationCapability =
        capabilities.textDocument &&
            capabilities.textDocument.publishDiagnostics &&
            capabilities.textDocument.publishDiagnostics.relatedInformation;
    return {
        capabilities: {
            textDocumentSync: documents.syncKind,
            // Tell the client that the server supports code completion
            completionProvider: {
                resolveProvider: true
            },
            signatureHelpProvider: {
                triggerCharacters: ['(', ',']
            }
        }
    };
});
connection.onInitialized(() => {
    if (hasConfigurationCapability) {
        // Register for all configuration changes.
        connection.client.register(vscode_languageserver_1.DidChangeConfigurationNotification.type, undefined);
    }
    if (hasWorkspaceFolderCapability) {
        connection.workspace.onDidChangeWorkspaceFolders(_event => {
            connection.console.log('Workspace folder change event received.');
        });
    }
});
// The global settings, used when the `workspace/configuration` request is not supported by the client.
// Please note that this is not the case when using this server with the client provided in this example
// but could happen with other clients.
const defaultSettings = { maxNumberOfProblems: 1000, port: 3000 };
let globalSettings = defaultSettings;
// Cache the settings of all open documents
let documentSettings = new Map();
connection.onDidChangeConfiguration(change => {
    if (hasConfigurationCapability) {
        // Reset all cached document settings
        documentSettings.clear();
    }
    else {
        globalSettings = ((change.settings.ostw || defaultSettings));
    }
    // Revalidate all open text documents
    documents.all().forEach(validateTextDocument);
});
function getDocumentSettings(resource) {
    if (!hasConfigurationCapability) {
        return Promise.resolve(globalSettings);
    }
    let result = documentSettings.get(resource);
    if (!result) {
        result = connection.workspace.getConfiguration({
            scopeUri: resource,
            section: 'ostw'
        });
        documentSettings.set(resource, result);
    }
    return result;
}
// Only keep settings for open documents
documents.onDidClose(e => {
    documentSettings.delete(e.document.uri);
});
// The content of a text document has changed. This event is emitted
// when the text document first opened or when its content has changed.
documents.onDidChangeContent(change => {
    validateTextDocument(change.document);
});
const request = require('request');
function validateTextDocument(textDocument) {
    return __awaiter(this, void 0, void 0, function* () {
        let problems = 0;
        let settings = yield getDocumentSettings(textDocument.uri);
        let diagnostics = [];
        request.post({ url: 'http://localhost:3000/parse', body: textDocument.getText() }, function callback(err, httpResponse, body) {
            let diagnostics = JSON.parse(body);
            connection.sendDiagnostics({ uri: textDocument.uri, diagnostics });
        });
    });
}
connection.onDidChangeWatchedFiles(_change => {
    // Monitored files have change in VS Code
    connection.console.log('We received an file change event');
});
// This handler provides the initial list of the completion items.
connection.onCompletion((_textDocumentPosition) => {
    return getCompletion(_textDocumentPosition);
    // The pass parameter contains the position of the text document in
    // which code complete got requested. For the example we ignore this
    // info and always provide the same completion items.
    /*
    return [
        {
            label: 'AbortIf',
            kind: CompletionItemKind.Method,
            data: 1,
        }
    ];
    */
});
function getCompletion(pos) {
    let textDocument = documents.get(pos.textDocument.uri);
    let data = JSON.stringify({
        textDocument: textDocument.getText(),
        caret: pos.position
    });
    return new Promise(function (resolve, reject) {
        request.post({ url: 'http://localhost:3000/completion', body: data }, function (error, res, body) {
            if (!error && res.statusCode == 200) {
                let completionItems = JSON.parse(body);
                resolve(completionItems);
            }
            else {
                reject(error);
            }
        });
    });
}
connection.onSignatureHelp((pos) => {
    return getSignatureHelp(pos);
});
function getSignatureHelp(pos) {
    let textDocument = documents.get(pos.textDocument.uri);
    let data = JSON.stringify({
        textDocument: textDocument.getText(),
        caret: pos.position
    });
    return new Promise(function (resolve, reject) {
        request.post({ url: 'http://localhost:3000/signature', body: data }, function (error, res, body) {
            if (!error && res.statusCode == 200) {
                let signatureHelp = JSON.parse(body);
                resolve(signatureHelp);
            }
            else {
                reject(error);
            }
        });
    });
}
/*
// This handler resolves additional information for the item selected in
// the completion list.
connection.onCompletionResolve(
    (item: CompletionItem): CompletionItem => {
        if (item.data == 1) {
            item.detail = "AbortIf(condition)";
            item.documentation = "AbortIf will abort the rule if the condition is true.";
            item.textEdit
        }
        return item;
    }
);
*/
/*
connection.onDocumentColor(
    (documentColor: DocumentColorParams) => {

        let colors = getColors(documentColor);
        return colors;
    }
);
*/
/*
function getColors(documentColor: DocumentColorParams) {

    let textDocument = documents.get(documentColor.textDocument.uri);
    
    return new Promise<ColorInformation[]>(function (resolve, reject) {
      request.post({url:'http://localhost:3000/color', body: textDocument.getText()}, function (error, res, body) {
        if (!error && res.statusCode == 200) {

            let colorInformations: ColorInformation[] = [];
            let colors = JSON.parse(body);
            for (var i = 0; i < colors.length; i++) {
                let color: ColorInformation =
                {
                    range: {
                        start: textDocument.positionAt(colors[i].start),
                        end: textDocument.positionAt(colors[i].end),
                    },
                    color: {
                        red: colors[i].r,
                        green: colors[i].g,
                        blue: colors[i].b,
                        alpha: colors[i].a
                    }
                };
                colorInformations.push(color);
            }

            resolve(colorInformations);
        } else {
            reject(error);
        }
      });
    });
  }

connection.onColorPresentation(
    (params: ColorPresentationParams) => {
        
        let colorPresentations: ColorPresentation[] = [];

        let cp: ColorPresentation =
        {
            label: '????'
        };

        colorPresentations.push(cp);

        return colorPresentations;
    }
);
*/
/*
connection.onHover((event) => {
});
*/
/*
connection.onDidOpenTextDocument((params) => {
    // A text document got opened in VS Code.
    // params.uri uniquely identifies the document. For documents store on disk this is a file URI.
    // params.text the initial full content of the document.
    connection.console.log(`${params.textDocument.uri} opened.`);
});
connection.onDidChangeTextDocument((params) => {
    // The content of a text document did change in VS Code.
    // params.uri uniquely identifies the document.
    // params.contentChanges describe the content changes to the document.
    connection.console.log(`${params.textDocument.uri} changed: ${JSON.stringify(params.contentChanges)}`);
});
connection.onDidCloseTextDocument((params) => {
    // A text document got closed in VS Code.
    // params.uri uniquely identifies the document.
    connection.console.log(`${params.textDocument.uri} closed.`);
});
*/
// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);
// Listen on the connection
connection.listen();
//# sourceMappingURL=server.js.map