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
            },
            hoverProvider: true
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
const defaultSettings = { maxNumberOfProblems: 1000, port1: 3000, port2: 3001 };
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
        let settings = yield getDocumentSettings(textDocument.uri);
        sendRequest(textDocument.uri, 'parse', textDocument.getText(), null, null, function callback(body) {
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
});
function getCompletion(pos) {
    let textDocument = documents.get(pos.textDocument.uri);
    let data = JSON.stringify({
        textDocument: textDocument.getText(),
        caret: pos.position
    });
    return new Promise(function (resolve, reject) {
        sendRequest(pos.textDocument.uri, 'completion', data, resolve, reject, function (body) {
            let completionItems = JSON.parse(body);
            return completionItems;
        });
    });
}
connection.onCompletionResolve((completionItem) => {
    return null;
});
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
        sendRequest(pos.textDocument.uri, 'signature', data, resolve, reject, function (body) {
            let signatureHelp = JSON.parse(body);
            return signatureHelp;
        });
    });
}
connection.onHover((pos) => {
    let textDocument = documents.get(pos.textDocument.uri);
    let data = JSON.stringify({
        textDocument: textDocument.getText(),
        caret: pos.position
    });
    return new Promise(function (resolve, reject) {
        sendRequest(pos.textDocument.uri, 'hover', data, resolve, reject, function (body) {
            let hover = JSON.parse(body);
            return hover;
        });
    });
});
function sendRequest(uri, path, data, resolve, reject, callback) {
    return __awaiter(this, void 0, void 0, function* () {
        let settings = yield getDocumentSettings(uri);
        request.post({ url: 'http://localhost:' + settings.port1 + '/' + path, body: data }, function (error, res, body) {
            if (!error && res.statusCode == 200) {
                let value = callback(body);
                if (resolve != null)
                    resolve(value);
            }
            else if (reject != null) {
                //reject(error);
                resolve(null);
            }
        });
    });
}
// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);
// Listen on the connection
connection.listen();
//# sourceMappingURL=server.js.map