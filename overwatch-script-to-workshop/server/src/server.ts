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
	Position
} from 'vscode-languageserver';
import { connect } from 'tls';
import { cpus } from 'os';
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
			}
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
interface ExampleSettings {
	maxNumberOfProblems: number;
}

// The global settings, used when the `workspace/configuration` request is not supported by the client.
// Please note that this is not the case when using this server with the client provided in this example
// but could happen with other clients.
const defaultSettings: ExampleSettings = { maxNumberOfProblems: 1000 };
let globalSettings: ExampleSettings = defaultSettings;

// Cache the settings of all open documents
let documentSettings: Map<string, Thenable<ExampleSettings>> = new Map();

connection.onDidChangeConfiguration(change => {
if (hasConfigurationCapability) {
	// Reset all cached document settings
	documentSettings.clear();
} else {
	globalSettings = <ExampleSettings>(
	(change.settings.languageServerExample || defaultSettings)
	);
}

// Revalidate all open text documents
documents.all().forEach(validateTextDocument);
});

function getDocumentSettings(resource: string): Thenable<ExampleSettings> {
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

async function validateTextDocument(textDocument: TextDocument): Promise<void> {

	let problems = 0;
	let settings = await getDocumentSettings(textDocument.uri);
	let diagnostics: Diagnostic[] = [];

	request.post({url:'http://localhost:3000/parse', body: textDocument.getText()}, function callback(err, httpResponse, body) {
		connection.console.log('Recieved: ' + httpResponse);

		let errors = JSON.parse(body);

		for (var i = 0; i < errors.length && problems < settings.maxNumberOfProblems; i++) {     
			problems++;

			let diagnostic: Diagnostic = {
				severity: DiagnosticSeverity.Error,
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
}

connection.onDidChangeWatchedFiles(_change => {
// Monitored files have change in VS Code
	connection.console.log('We received an file change event');
});

// This handler provides the initial list of the completion items.
connection.onCompletion((_textDocumentPosition: TextDocumentPositionParams) => {

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

function getCompletion(pos: TextDocumentPositionParams) {

	let textDocument = documents.get(pos.textDocument.uri);
	let data = JSON.stringify({
		textDocument: textDocument.getText(),
		caret: pos.position
	});
	
	return new Promise<CompletionItem[]>(function (resolve, reject) {

	  	request.post({url:'http://localhost:3000/completion', body: data}, function (error, res, body) {
			if (!error && res.statusCode == 200) {

				let completionItems: CompletionItem[] = JSON.parse(body);
				resolve(completionItems);
			}
			else {
		  		reject(error);
			}
	  	});
	});
}

connection.onSignatureHelp((pos: TextDocumentPositionParams) => {
	return getSignatureHelp(pos);
});

function getSignatureHelp(pos: TextDocumentPositionParams) {

	let textDocument = documents.get(pos.textDocument.uri);
	let data = JSON.stringify({
		textDocument: textDocument.getText(),
		caret: pos.position
	});
	
	return new Promise<SignatureHelp>(function (resolve, reject) {

	  	request.post({url:'http://localhost:3000/signature', body: data}, function (error, res, body) {
			if (!error && res.statusCode == 200) {

				let signatureHelp: SignatureHelp = JSON.parse(body);

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