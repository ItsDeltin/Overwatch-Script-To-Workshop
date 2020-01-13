/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window, Uri, Position, Location } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind, InitializationFailedHandler, ErrorHandler } from 'vscode-languageclient';
import { setTimeout } from 'timers';

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
			restartServer();
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
				// Clear the output
				workshopOut.clear();
				// Append the compiled result.
				workshopOut.appendLine(code);
				lastWorkshopOutput = code;
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
	context.subscriptions.push(
		vscode.commands.registerCommand('ostw.restartLanguageServer', restartServer, this)
	);
	context.subscriptions.push(
		vscode.commands.registerCommand('ostw.testCommand', (uriStr: string, posStr: string, locationsStr: string[]) => {
			let uri: Uri = Uri.parse(uriStr);
			let pos: Position = <Position>JSON.parse(posStr);
			let locations: Location[] = [];

			for (var i = 0; i < locationsStr.length; i++)
				locations.push(<Location>JSON.parse(locationsStr[i]));

			vscode.commands.executeCommand('editor.action.showReferences', uri.toString(), pos, locations[0]);
		}, this)
	);
}

function restartServer()
{
	config = workspace.getConfiguration("ostw", null);
	client.outputChannel.hide();
	client.outputChannel.dispose();
	lastWorkshopOutput = "";
	workshopOut.clear();
	if (isServerRunning) {
		client.stop();
		isServerRunning = false;
	}
	setTimeout(() => startLanguageServer, 5000);
	//startLanguageServer();
}