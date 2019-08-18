import {
	createConnection,
	TextDocuments,
	TextDocument,
	Diagnostic,
	DiagnosticSeverity,
	ProposedFeatures,
	InitializeParams,
	DidChangeConfigurationNotification,
	CompletionItem,
	CompletionItemKind,
	TextDocumentPositionParams,
	RequestHandler,
	Hover,
	DocumentColorParams,
	Color,
	ColorInformation,
	ColorPresentation,
	ColorPresentationParams,
	SignatureHelp,
	TextEdit,
	Position,
	PublishDiagnosticsParams
} from 'vscode-languageserver';
import { connect } from 'tls';
import { cpus } from 'os';
import { realpath } from 'fs';
import { stringify } from 'querystring';
//import { request } from 'http';

// Create a connection for the server. The connection uses Node's IPC as a transport.
// Also include all preview / proposed LSP features.
let connection = createConnection(ProposedFeatures.all);

// Create a simple text document manager. The text document manager
// supports full document sync only
let documents: TextDocuments = new TextDocuments();

let hasConfigurationCapability: boolean | undefined = false;
let hasWorkspaceFolderCapability: boolean | undefined = false;
let hasDiagnosticRelatedInformationCapability: boolean | undefined = false;

connection.onInitialize((params: InitializeParams) => {
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
		connection.client.register(DidChangeConfigurationNotification.type, undefined);
	}
	if (hasWorkspaceFolderCapability) {
		connection.workspace.onDidChangeWorkspaceFolders(_event => {
		connection.console.log('Workspace folder change event received.');
		});
	}
});

// The example settings
interface Settings {
	maxNumberOfProblems: number;
	port1: number;
	port2: number;
}

// The global settings, used when the `workspace/configuration` request is not supported by the client.
// Please note that this is not the case when using this server with the client provided in this example
// but could happen with other clients.
const defaultSettings: Settings = { maxNumberOfProblems: 1000, port1: 3000, port2: 3001 };
let globalSettings: Settings = defaultSettings;

// Cache the settings of all open documents
let documentSettings: Map<string, Thenable<Settings>> = new Map();

connection.onDidChangeConfiguration(change => {
if (hasConfigurationCapability) {
	// Reset all cached document settings
	documentSettings.clear();
} else {
	globalSettings = <Settings>(
	(change.settings.ostw || defaultSettings)
	);
}

// Revalidate all open text documents
documents.all().forEach(validateTextDocument);
});

function getDocumentSettings(resource: string): Thenable<Settings> {
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

async function validateTextDocument(textDocument: TextDocument): Promise<void> {

	let settings = await getDocumentSettings(textDocument.uri);
	let data = JSON.stringify({
		uri: textDocument.uri,
		content: textDocument.getText()
	});

	sendRequest(textDocument.uri, 'parse', data, null, null, function callback(body) {

		let diagnostics: PublishDiagnosticsParams[] = JSON.parse(body);

		for (var i = 0; i < diagnostics.length; i++)
			connection.sendDiagnostics(diagnostics[i]);
	});
}

connection.onDidChangeWatchedFiles(_change => {
// Monitored files have change in VS Code
	connection.console.log('We received an file change event');
});

// This handler provides the initial list of the completion items.
connection.onCompletion((_textDocumentPosition: TextDocumentPositionParams) => {

	return getCompletion(_textDocumentPosition);
});

function getCompletion(pos: TextDocumentPositionParams) {

	let data = JSON.stringify(pos);
	
	return new Promise<CompletionItem[]>(function (resolve, reject) {
		sendRequest(pos.textDocument.uri, 'completion', data, resolve, reject, function(body) {
			let completionItems: CompletionItem[] = JSON.parse(body);
			return completionItems;
		});
	});
}

connection.onCompletionResolve((completionItem: CompletionItem) => {
	return null;
});

connection.onSignatureHelp((pos: TextDocumentPositionParams) => {
	return getSignatureHelp(pos);
});

function getSignatureHelp(pos: TextDocumentPositionParams) {

	let data = JSON.stringify(pos);
	
	return new Promise<SignatureHelp>(function (resolve, reject) {
		sendRequest(pos.textDocument.uri, 'signature', data, resolve, reject, function(body) {
			let signatureHelp: SignatureHelp = JSON.parse(body);
			return signatureHelp;
		});
	});
}

connection.onHover((pos: TextDocumentPositionParams) => {

	let data = JSON.stringify(pos);

	return new Promise<Hover>(function (resolve, reject) {
		sendRequest(pos.textDocument.uri, 'hover', data, resolve, reject, function(body) {
			let hover: Hover = JSON.parse(body);
			return hover;
		});
	});
});

const request = require('request');
async function sendRequest(uri, path, data, resolve, reject, callback) {
	
	let settings = await getDocumentSettings(uri);

	request.post({url:'http://localhost:' + settings.port1 + '/' + path, body: data}, function (error, res, body) 
	{
		if (!error && res.statusCode == 200) {

			let value = null;
			if (body != "")
				value = callback(body);

			if (resolve != null)
				resolve(value);
		}
		else if (reject != null) {
			//reject(error);
			resolve(null);
		}
	});
}

// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);

// Listen on the connection
connection.listen();