/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window, Uri, Position, Location, StatusBarItem } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind, InitializationFailedHandler, ErrorHandler, TextDocument, RequestType, Position as LSPosition, Location as LSLocation } from 'vscode-languageclient';
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
		documentSelector: [{ scheme: 'file', language: 'ostw' }],
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

	context.subscriptions.push(vscode.commands.registerCommand('ostw.showReferences', (uriStr: string, position: LSPosition, locations: LSLocation[]) => {
		let uri : Uri = Uri.parse(uriStr);
		let pos: Position = client.protocol2CodeConverter.asPosition(position);
		let locs: Location[] = locations.map(client.protocol2CodeConverter.asLocation);

		vscode.commands.executeCommand('editor.action.showReferences', uri, pos, locs);
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
