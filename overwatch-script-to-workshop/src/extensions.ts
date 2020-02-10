/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind, InitializationFailedHandler, ErrorHandler, TextDocument, RequestType } from 'vscode-languageclient';

let client: LanguageClient;
let workshopOut: OutputChannel;
let config = workspace.getConfiguration("ostw", null);
let isServerRunning = false;

export function activate(context: ExtensionContext) {

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code"); // Create the channel.
	
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
			startLanguageServer();
		}
	});
	startLanguageServer();
}

function startLanguageServer()
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

	let initFail: InitializationFailedHandler = function(error: any): boolean {
		return true;
	};

	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{ scheme: 'file', language: 'ostw' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			// fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
			configurationSection: 'ostw'
		},
		initializationFailedHandler: initFail
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
				workshopPanelProvider.onDidChangeEmitter.fire(this.uri);

				// Clear the output
				workshopOut.clear();
				// Append the compiled result.
				workshopOut.appendLine(code);
			}
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