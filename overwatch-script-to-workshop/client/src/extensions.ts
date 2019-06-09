/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import { workspace, ExtensionContext, OutputChannel, window } from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind,
} from 'vscode-languageclient';

let client: LanguageClient;

let workshopOut: OutputChannel;

const http = require('http');
const request = require('request');

const config = workspace.getConfiguration("ostw", null);

export function activate(context: ExtensionContext) {
	
	ping();

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code"); // Create the channel.
	// Create the server.
	http.createServer(function (req, res) {
			
		if (req.method == 'POST') {
			var body = '';
			req.on('data', function(data) {
				body += data;
			});
			req.on('end', function() {
				// Clear the output
				workshopOut.clear();

				// Append the compiled result.
				workshopOut.appendLine(body);

				// Close connection.
				res.end();
			});
		}
		else
		{
			res.end();
		}
	}).listen(config.get('port2')); // Listen on port.

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
	setTimeout(ping, 5000);
}