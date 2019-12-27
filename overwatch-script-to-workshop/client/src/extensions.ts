/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind } from 'vscode-languageclient';
import request from 'request';

let client: LanguageClient;

let workshopOut: OutputChannel;

const config = workspace.getConfiguration("ostw", null);

export function activate(context: ExtensionContext) {

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code"); // Create the channel.
	
	addCommands(context);

	workspace.onDidChangeConfiguration((e: vscode.ConfigurationChangeEvent) => {
		if (e.affectsConfiguration("ostw.deltintegerPath"))
		{
			// This block should run when the `ostw.deltintegerPath` setting is changed.
			// TODO: Start the language server using the new filepath. Also stop the language server if it is already started.
		}
	});

	// Gets the path to the server executable.
	const serverModule = <string>config.get('deltintegerPath');

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
	client.start();

	client.onReady().then(() => {
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
	});
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}

var lastWorkshopOutput : string = null;
/*
var failSent : boolean;
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

	if (window.activeTextEditor != null)
	{
		let file = window.activeTextEditor.document.fileName;
		getCode(file, (code) => updateCode(file, code));
	}
	setTimeout(ping, 1000);
}
*/

function updateCode(file: string, code: string)
{
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
	if (vscode.window.activeTextEditor == null)
		return;

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
			vscode.ViewColumn.Active,
			{
				enableScripts: true,
				retainContextWhenHidden: true
			}
		);

		this.panel.onDidDispose(() => this.dispose());

		getCode(fullPath, (code) => this.setCode(code));
	}

	setCode(code: string)
	{
		if (this.lastWorkshopOutput != code)
		{
			this.panel.webview.html = this.getContent(code);
			this.lastWorkshopOutput = code;
		}
	}

	getContent(code: string)
	{
		if (code == null) code = lastWorkshopOutput;

		return `<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<title>OSTW Output</title>
	<style>
		pre{
			counter-reset: line;
		}
		code{
			counter-increment: line;
			color: var(--vscode-editor-foreground);
		}
		code:before{
			content: counter(line);
			-webkit-user-select: none;

			display: inline-block;
			text-align: right;
			width: 25px;
			margin-right: 15px;
			font-family: Consolas;
			color: var(--vscode-editorLineNumber-foreground);
			font-size: 14px;
		}
		button {
			color: var(--vscode-button-foreground);
			background-color: var(--vscode-button-background);
			border: none;
			padding: 5px 25px 5px 25px;
			font-family: sans-serif;
		}
		button:hover {
			background-color: var(--vscode-button-hoverBackground);
		}
	</style>
</head>
<body>
	${this.formatCode(code)}
</body>
</html>`;
	}

	formatCode(code: string) {
		var final: string = '<pre id="workshop-code">';
		var lines: string[] = code.split('\n');
		for (var i = 0; i < lines.length; i++)
			final += '<code>' + lines[i] + '</code>';
		final += '</pre>';
		return final;
	}

	dispose()
	{
		var index = panels.indexOf(this);
		panels.splice(index, 1);
	}
}
var panels : OutputPanel[] = [];