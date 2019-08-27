/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window } from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind,
} from 'vscode-languageclient';

let client: LanguageClient;

let workshopOut: OutputChannel;

import * as http from 'http';
const request = require('request');

const config = workspace.getConfiguration("ostw", null);

export function activate(context: ExtensionContext) {

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code"); // Create the channel.
	
	addCommands(context);

	ping();

	// The server is implemented in node
	let serverModule = context.asAbsolutePath(
		path.join('server', 'out', 'server.js')
	);
	// The debug options for the server
	// --inspect=6009: runs the server in Node's Inspector mode so VS Code can attach to the server for debugging
	let debugOptions = { execArgv: ['--nolazy', '--inspect=6009'] };

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	let serverOptions: ServerOptions = {
		run: { module: serverModule, transport: TransportKind.ipc },
		debug: {
			module: serverModule,
			transport: TransportKind.ipc,
			options: debugOptions
		}
	};

	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{ scheme: 'file', language: 'plaintext' }, { scheme: 'file', language: 'ostw' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
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
	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}

var failSent : boolean;
var lastWorkshopOutput : string = null;
function ping()
{
	request('http://localhost:' + config.get('port1') + '/ping', function(error, res, body) {
		if (!error && res.statusCode == 200 && body == 'OK') {
			if (failSent)
			{
				window.showInformationMessage('Connected to the OSTW language server on port ' + config.get('port1') + '.');
				failSent = false;
			}
		}
		else if (!failSent) {
			window.showWarningMessage('Failed to connect to the OSTW language server on port ' + config.get('port1') + '.');
			failSent = true;
		}
	});

	let file = window.activeTextEditor.document.fileName;
	getCode(file, function (code) {
		if (lastWorkshopOutput != code && code != "")
		{
			// Clear the output
			workshopOut.clear();
			// Append the compiled result.
			workshopOut.appendLine(code);
			lastWorkshopOutput = code;
		}

		for (var i = 0; i < panels.length; i++)
			if (panels[i].fullPath == file)
				panels[i].setCode(code);
	});
	setTimeout(ping, 1000);
}

function getCode(uri:string, callback)
{
	request.post({url:'http://localhost:' + config.port1 + '/code', body: JSON.stringify({uri: uri})}, function (error, res, body) {
		if (!error) callback(body);
		res.end();
	});
}

function addCommands(context: ExtensionContext)
{
	context.subscriptions.push(
		vscode.commands.registerCommand('ostw.webviewOutput', webviewOutput, this)
	);
}

function webviewOutput()
{
	let fullPath = vscode.window.activeTextEditor.document.fileName;

	for (var i = 0; i < panels.length; i++)
		if (panels[i].fullPath == fullPath)
		{
			panels[i].panel.reveal();
			return;
		}

	let panel: OutputPanel = new OutputPanel(fullPath);
	panel.panel.reveal();
	panels.push(panel);
}

class OutputPanel
{
	panel: vscode.WebviewPanel;
	fileName: string;
	fullPath: string;
	lastWorkshopOutput : string = null;

	constructor(fullPath: string)
	{
		this.fullPath = fullPath;
		this.fileName = path.basename(fullPath);

		this.panel = window.createWebviewPanel(
			'ostw',
			this.fileName + ' Workshop Output',
			vscode.ViewColumn.Active
		);

		this.panel.onDidDispose(() => this.dispose());

		getCode(fullPath, (code) => this.setCode(code));
	}

	setCode(code)
	{
		if (this.lastWorkshopOutput != code)
		{
			this.panel.webview.html = "<pre><code>" + code + "</code></pre>";
			this.lastWorkshopOutput = code;
		}
	}

	dispose()
	{
		var index = panels.indexOf(this);
		panels.splice(index, 1);
	}
}
var panels : OutputPanel[] = [];