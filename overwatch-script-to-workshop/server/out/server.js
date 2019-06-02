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
            colorProvider: true
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
const defaultSettings = { maxNumberOfProblems: 1000 };
let globalSettings = defaultSettings;
// Cache the settings of all open documents
let documentSettings = new Map();
connection.onDidChangeConfiguration(change => {
    if (hasConfigurationCapability) {
        // Reset all cached document settings
        documentSettings.clear();
    }
    else {
        globalSettings = ((change.settings.languageServerExample || defaultSettings));
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
            section: 'languageServerExample'
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
            connection.console.log('Recieved: ' + httpResponse);
            let errors = JSON.parse(body);
            for (var i = 0; i < errors.length && problems < settings.maxNumberOfProblems; i++) {
                problems++;
                let diagnostic = {
                    severity: vscode_languageserver_1.DiagnosticSeverity.Error,
                    range: {
                        start: textDocument.positionAt(errors[i].Start),
                        end: textDocument.positionAt(errors[i].Stop)
                    },
                    //message: `${m[0]} is all uppercase.`,
                    message: errors[i].Message,
                    source: 'ex'
                };
                diagnostics.push(diagnostic);
            }
            connection.sendDiagnostics({ uri: textDocument.uri, diagnostics });
        });
        // In this simple example we get the settings for every validate run.
        // The validator creates diagnostics for all uppercase words length 2 and more
        /*
        let text = textDocument.getText();
        let pattern = /\b[A-Z]{2,}\b/g;
        let m: RegExpExecArray | null;
    
        let problems = 0;
        let diagnostics: Diagnostic[] = [];
            while ((m = pattern.exec(text)) && problems < settings.maxNumberOfProblems) {
            problems++;
            let diagnostic: Diagnostic = {
                severity: DiagnosticSeverity.Warning,
                range: {
                    start: textDocument.positionAt(m.index),
                    end: textDocument.positionAt(m.index + m[0].length)
                },
                message: `${m[0]} is all uppercase.`,
                source: 'ex'
            };
            if (hasDiagnosticRelatedInformationCapability) {
                diagnostic.relatedInformation = [{
                    location: {
                        uri: textDocument.uri,
                        range: Object.assign({}, diagnostic.range)
                    },
                    message: 'Spelling matters'
                },
                {
                    location: {
                        uri: textDocument.uri,
                        range: Object.assign({}, diagnostic.range)
                    },
                    message: 'Particularly for names'
                }];
            }
            diagnostics.push(diagnostic);
        }
    
        // Send the computed diagnostics to VS Code.
        connection.sendDiagnostics({ uri: textDocument.uri, diagnostics });
        */
    });
}
connection.onDidChangeWatchedFiles(_change => {
    // Monitored files have change in VS Code
    connection.console.log('We received an file change event');
});
// This handler provides the initial list of the completion items.
connection.onCompletion((_textDocumentPosition) => {
    // The pass parameter contains the position of the text document in
    // which code complete got requested. For the example we ignore this
    // info and always provide the same completion items.
    return [
        {
            label: 'AbortIf',
            kind: vscode_languageserver_1.CompletionItemKind.Method,
            data: 1,
        }
    ];
});
// This handler resolves additional information for the item selected in
// the completion list.
connection.onCompletionResolve((item) => {
    if (item.data == 1) {
        item.detail = "AbortIf(condition)";
        item.documentation = "AbortIf will abort the rule if the condition is true.";
    }
    return item;
});
connection.onDocumentColor((documentColor) => {
    let textDocument = documents.get(documentColor.textDocument.uri);
    return request.post({ url: 'http://localhost:3000/color', body: textDocument.getText() }, function callback(err, httpResponse, body) {
        let colorInformations = [];
        let colors = JSON.parse(body);
        for (var i = 0; i < colors.length; i++) {
            let color = {
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
        return colorInformations;
    });
});
connection.onColorPresentation((params) => {
    let colorPresentations = [];
    let cp = {
        label: 'test'
    };
    colorPresentations.push(cp);
    return colorPresentations;
});
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