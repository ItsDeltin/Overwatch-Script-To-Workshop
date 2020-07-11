/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window, Uri, Position, Location, StatusBarItem } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind, InitializationFailedHandler, ErrorHandler, TextDocument, RequestType, Position as LSPosition, Location as LSLocation, Range as LSRange } from 'vscode-languageclient';
import { setTimeout } from 'timers';
const fetch = require('node-fetch').default;

let client: LanguageClient;
let workshopOut: OutputChannel;
let elementCountStatus: vscode.StatusBarItem;
let config = workspace.getConfiguration("ostw", null);
let isServerRunning = false;

export function activate(context: ExtensionContext) {

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code");

	// Shows element count.
	elementCountStatus = window.createStatusBarItem(vscode.StatusBarAlignment.Left, 0);
	elementCountStatus.tooltip = "The number of elements in the workshop output. The workshop will accept a maximum of 20,000.";
	elementCountStatus.show();
	setElementCount(0);
	
	addCommands(context);
	// context.subscriptions.push(vscode.languages.registerDocument);
	// new vscode.languages.

	workspace.onDidChangeConfiguration((e: vscode.ConfigurationChangeEvent) => {
		if (e.affectsConfiguration("ostw.deltintegerPath"))
		{
			config = workspace.getConfiguration("ostw", null);

			client.outputChannel.hide();
			client.outputChannel.dispose();
			lastWorkshopOutput = "";
			if (isServerRunning) {
				client.stop();
				isServerRunning = false;
			}
			startLanguageServer(context);
		}
	});
	startLanguageServer(context);
}

function setElementCount(count)
{
	elementCountStatus.text = "Element count: " + count + " / 20000";
}

function startLanguageServer(context: ExtensionContext)
{
	// Gets the path to the server executable.
	const serverModule = <string>config.get('deltintegerPath');
	var serverPath: path.ParsedPath = path.parse(serverModule);

	if (serverPath.name.toLowerCase() != "deltinteger")
	{
		workshopOut.clear();
		workshopOut.appendLine("The ostw.deltintegerPath does not resolve to deltinteger.exe.");
		return;
	}

	// It was me, stdio!
	const options: ExecutableOptions = { stdio: "pipe", detached: false };
	const serverOptions: ServerOptions = {
		run:   { command: serverModule, args: ['--langserver']           , options: options },
		debug: { command: serverModule, args: ['--langserver', '--debug'], options: options }
	};

	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [selector],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			// fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
			configurationSection: 'ostw'
		}
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'ostw',
		'Overwatch Script To Workshop',
		serverOptions,
		clientOptions
	);

	// Start the client. This will also launch the server
	client.onReady().then(() => {
		isServerRunning = true;

		// When the client is ready, setup the workshopCode notification.
		client.onNotification("workshopCode", (code: string)=> {

			if (!registeredProvider)
			{
				vscode.languages.registerDocumentSemanticTokensProvider(selector, provider, legend);
				registeredProvider = true;
			}

			if (code != lastWorkshopOutput)
			{
				lastWorkshopOutput = code;
				workshopPanelProvider.onDidChangeEmitter.fire(vscode.Uri.parse('ow_ostw:Workshop Output.ow'));

				// Clear the output
				workshopOut.clear();
				// Append the compiled result.
				workshopOut.appendLine(code);
			}
		});

		// Update element count in window.
		client.onNotification("elementCount", (count: string) => {
			setElementCount(count);
		});

		// Check version.
		client.onNotification("version", (version: string) => {
			// Do not show the message if the newRelease config is false.
			if (!config.get('newRelease')) return;

			fetch('https://api.github.com/repos/ItsDeltin/Overwatch-Script-To-Workshop/releases/latest')
				.then(res => res.json())
				.then(json => {
					let latest: string = json.tag_name;
					let url: string = json.html_url;

					if (version != latest && config.get('ignoreRelease') != latest)
					{
						window.showInformationMessage(
							// Message
							"A new version of Overwatch Script To Workshop (" + latest + ") is now available. (Current: " + version + ")",
							// Options
							"Ignore release", "View release"
						).then(chosenOption => {
							// Open the release.
							if (chosenOption == "View release")
								vscode.env.openExternal(Uri.parse(url));
							// Don't show again for this version.
							else if (chosenOption == "Ignore release")
								config.update('ignoreRelease', latest, vscode.ConfigurationTarget.Global);
						});
					}
				})
				.catch(error => {});
		});
	}).catch((reason) => {
		workshopOut.clear();
		workshopOut.appendLine(reason);
	});

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}

var lastWorkshopOutput : string = null;

function addCommands(context: ExtensionContext)
{
	// Push provider.
	context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider('ow_ostw', workshopPanelProvider));

	context.subscriptions.push(vscode.commands.registerCommand('ostw.virtualDocumentOutput', async () => {
		// Encode uri.
		let uri = vscode.Uri.parse('ow_ostw:Workshop Output.ow');

		let doc : vscode.TextDocument = await vscode.workspace.openTextDocument(uri);
		await vscode.window.showTextDocument(doc, { preview: false });
	}, this));

	// showReferences link
	context.subscriptions.push(vscode.commands.registerCommand('ostw.showReferences', (uriStr: string, position: LSPosition, locations: LSLocation[]) => {
		let uri : Uri = Uri.parse(uriStr);
		let pos: Position = client.protocol2CodeConverter.asPosition(position);
		let locs: Location[] = locations.map(client.protocol2CodeConverter.asLocation);

		vscode.commands.executeCommand('editor.action.showReferences', uri, pos, locs);
	}, this));

	// Pathmap builder
	context.subscriptions.push(vscode.commands.registerCommand('ostw.createPathmap', () => {
		// Send the 'pathmapFromClipboard' request to the language server.
		client.sendRequest('pathmapFromClipboard').then((result: string) => {
			// The request will return 'success' if the pathmap was successfully created. Any other string is an error message.
			if (result != 'success')
			{
				vscode.window.showErrorMessage('Pathmap generator error: ' + result);
				return;
			}
			// If successful, show a save dialog to save the pathmap file.
			vscode.window.showSaveDialog({
				filters: {
					'Pathmaps': ['pathmap']
				}
			}).then((uri: vscode.Uri) => {
				if (uri == undefined) return; // Canceled
				// Send a second request 'pathmapApply' with the uri parameter to the language server.
				client.sendRequest('pathmapApply', uri).then(() => {
					// Success
					vscode.window.showInformationMessage('Pathmap file saved!');
					vscode.workspace.openTextDocument(uri).then(document => {
						vscode.window.showTextDocument(document);
					});
				}, (reason: any) => {
					vscode.window.showErrorMessage(reason);
				});
			});
		});
	}, this));

	// Pathmap editor
	context.subscriptions.push(vscode.commands.registerCommand('ostw.pathmapEditorCode', () => {

		var editPathmap:string = null; // Stores the currently opened .pathmap file contents.
		var editPathmapFile:string = null; // Stores the currently opened .pathmap file path.
		// If the active text editor is a .pathmap file, set 'editPathmap' and 'editPathmapFile'.
		if (window.activeTextEditor != undefined && window.activeTextEditor.document.fileName.toLowerCase().endsWith('.pathmap')) {
			editPathmap = window.activeTextEditor.document.getText();
			editPathmapFile = window.activeTextEditor.document.fileName;
		}

		// Send the 'pathmapEditor' request with the 'editPathmap' contents for the parameter to the language server.
		client.sendRequest<boolean>('pathmapEditor', {Text: editPathmap}).then((result: boolean) => {
			// The request will return true if successful.
			// It can return false if PathfindEditor.del was tinkered with by the user (or there is a bug).
			if (result)
			{
				// Send a success message depending on if the editor is the default editor or the editor is editing a .pathmap file.
				if (editPathmapFile == null)
					vscode.window.showInformationMessage('Default pathmap editor copied to clipboard. Paste the rules in Overwatch to edit.');
				else
					vscode.window.showInformationMessage("Pathmap editor for '" + editPathmapFile + "' copied to clipboard. Paste the rules in Overwatch to edit.");
			}
			else
				vscode.window.showInformationMessage('Failed to generate pathmap editor code.');
		}, (reason: any) => {
			vscode.window.showErrorMessage(reason);
		});
	}, this));
}

const workshopPanelProvider = new class implements vscode.TextDocumentContentProvider {
	// emitter and its event
	onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
	onDidChange = this.onDidChangeEmitter.event;

	provideTextDocumentContent(uri: vscode.Uri): string {
		if (lastWorkshopOutput == null) return "";
		return lastWorkshopOutput;
	}
};

let registeredProvider: boolean = false;
const tokenTypes = ['comment', 'string', 'keyword', 'number', 'regexp', 'operator', 'namespace',
	'type', 'struct', 'class', 'interface', 'enum', 'enummember', 'typeParameter', 'function',
	'member', 'macro', 'variable', 'parameter', 'property', 'label'];
const tokenModifiers = ['declaration', 'readonly', 'static', 'deprecated', 'abstract', 'async', 'modification', 'documentation', 'defaultLibrary'];
const legend = new vscode.SemanticTokensLegend(tokenTypes, tokenModifiers);
const selector = { language: 'ostw', scheme: 'file' }; // register for all Java documents from the local file system

const provider: vscode.DocumentSemanticTokensProvider = {
	async provideDocumentSemanticTokens(document: vscode.TextDocument) {

		// Get the semantic tokens in the provided document from the language server.
		let tokens: {result:string, tokens: {range: LSRange, tokenType:string, modifiers:string[]}[]}
		let count: number = 0;

		do
		{
			tokens = await client.sendRequest('semanticTokens', document.uri);
			if (tokens.result != 'success')
			{
				count++;
				await new Promise(resolve => setTimeout(resolve, 100));
			}
		}
		// Repeat the request until success is returned.
		// This is needed due to the fact that vscode will call provideDocumentSemanticTokens before the script is parsed.
		// Cancel after 10 seconds.
		while (tokens.result != 'success' && count < 100)

		// Create the builder.
		let builder:vscode.SemanticTokensBuilder = new vscode.SemanticTokensBuilder(legend);

		// Push tokens to the builder.
		for (const token of tokens.tokens) {
			builder.push(client.protocol2CodeConverter.asRange(token.range), token.tokenType, token.modifiers);
		}

		// Return the result.
		return builder.build();
	}
};