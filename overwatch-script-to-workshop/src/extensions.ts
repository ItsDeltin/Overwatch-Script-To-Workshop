import * as vscode from 'vscode';
import { workspace, ExtensionContext, OutputChannel, window, Uri, Position, Location, StatusBarItem } from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, ExecutableOptions, Executable, TransportKind, InitializationFailedHandler, ErrorHandler, TextDocument, RequestType, Position as LSPosition, Location as LSLocation, Range as LSRange, ErrorAction, Message, CloseAction } from 'vscode-languageclient';
import { setTimeout } from 'timers';
import axios from 'axios';
import fs = require('fs');
import path = require('path');
import glob = require('glob');
import util = require('util');
import yauzl = require("yauzl");
import exec = require('child_process');

let globalStoragePath:string;
let defaultServerFolder:string;

let client: LanguageClient;
let workshopOut: OutputChannel;
let elementCountStatus: vscode.StatusBarItem;
let config = workspace.getConfiguration("ostw", null);
let isServerRunning = false;
let canBeStarted = false;

export async function activate(context: ExtensionContext) {
	globalStoragePath = context.globalStoragePath;
	defaultServerFolder = path.join(globalStoragePath, 'Server');

	// Shows the compiled result in an output window.
	workshopOut = window.createOutputChannel("Workshop Code");

	// Shows element count.
	elementCountStatus = window.createStatusBarItem(vscode.StatusBarAlignment.Left, 0);
	elementCountStatus.tooltip = "The number of elements in the workshop output. The workshop will accept a maximum of 20,000.";
	elementCountStatus.show();
	setElementCount(0);
	
	addCommands(context);

	workspace.onDidChangeConfiguration(async (e: vscode.ConfigurationChangeEvent) => {
		if (e.affectsConfiguration("ostw.deltintegerPath"))
		{
			config = workspace.getConfiguration("ostw", null);
			lastWorkshopOutput = "";
			await stopLanguageServer();
			setServerOptions(config.get('deltintegerPath'));
			startLanguageServer();
		}
	});

	makeLanguageServer();
}

function setElementCount(count)
{
	elementCountStatus.text = "Element count: " + count + " / 20000";
}

let serverOptions: {
    run: Executable;
    debug: Executable;
} = {run: null, debug: null};

async function makeLanguageServer()
{
	// Gets the path to the server executable.
	let serverModule = <string>config.get('deltintegerPath');

	// Determines if the server should be started after this call.
	let doStart: boolean = true;

	// Confirm the serverModule.
	if (serverModule == null || serverModule == '')
	{
		// If serverModule is not set, locate the dll at its default location.
		let findInstallLocation = await locateDLL(defaultServerFolder);
		if (findInstallLocation == null) {
			// Not found at the default location.
			doStart = false;
			// Ask the user if they want to install the OSTW server.
			vscode.window.showWarningMessage('The Overwatch Script To Workshop server was not found.', 'Automatically Install Latest', 'View Releases')
				.then(option => {
					// Download OSTW
					if (option == 'Automatically Install Latest') downloadOSTW();
					// View releases
					if (option == 'View Releases') vscode.env.openExternal(Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/releases'));
				})
		}
		else {
			serverModule = getModuleCommand(findInstallLocation);
			// Was found at the default location, update config.
			config.update('deltintegerPath', serverModule, vscode.ConfigurationTarget.Global);
		}
	}

	// Confirm that dotnet is installed.
	if (!await IsDotnetInstalled())
	{
		doStart = false;
		vscode.window.showWarningMessage('Overwatch Script To Workshop requires .Net Core 3.1 to be installed.', 'View Download Page')
			.then(option => {
				// View dotnet
				if (option == 'View Download Page') vscode.env.openExternal(Uri.parse('https://dotnet.microsoft.com/download/dotnet-core/current/runtime'));
			});
	}

	setServerOptions(serverModule);

	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [selector],
		synchronize: {
			configurationSection: 'ostw'
		},
		initializationFailedHandler: error => {
			console.log(error);
			return true; // hmm
		},
		errorHandler: new class implements ErrorHandler {
			error(error: Error, message: Message, count: number): ErrorAction {
				return ErrorAction.Continue;
			}
			closed(): CloseAction {
				return CloseAction.DoNotRestart;
			}
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
		// When the client is ready, setup the workshopCode notification.
		client.onNotification("workshopCode", (code: string)=> {

			if (!registeredProvider && config.get<boolean>('semanticHighlighting'))
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
		client.onNotification("version", async (version: string) => {
			// Do not show the message if the newRelease config is false.
			if (!config.get('newRelease')) return;

			// Get the latest release.
			let latestRelease = await getLatestRelease();
			if (latestRelease == null) return;

			// Get the name and url.
			let latest: string = latestRelease.tag_name;
			let url: string = latestRelease.html_url;

			if (version != latest && config.get('ignoreRelease') != latest)
			{
				window.showInformationMessage(
					// Message
					"A new version of Overwatch Script To Workshop (" + latest + ") is now available. (Current: " + version + ")",
					// Options
					"Download release", "Ignore release", "View release"
				).then(chosenOption => {
					// Download the release.
					if (chosenOption == "Download release")
						downloadOSTW();
					// Open the release.
					else if (chosenOption == "View release")
						vscode.env.openExternal(Uri.parse(url));
					// Don't show again for this version.
					else if (chosenOption == "Ignore release")
						config.update('ignoreRelease', latest, vscode.ConfigurationTarget.Global);
				});
			}
		});
	}).catch((reason) => {
		workshopOut.clear();
		workshopOut.appendLine(reason);
	});

	canBeStarted = true;
	if (doStart) startLanguageServer();
}

function startLanguageServer() {
	if (isServerRunning || !canBeStarted || serverOptions.run.command == null || serverOptions.run.command == '') return;
	client.start();
	isServerRunning = true;
}

async function stopLanguageServer() {
	if (!isServerRunning) return;
	await client.stop();
	isServerRunning = false;
}

function setServerOptions(serverModule: string)
{
	// It was me, stdio!
	let serverExecutableOptions = { stdio: "pipe", detached: false, shell: <boolean>config.get('deltintegerShell') };
	serverOptions.run = {
		command: serverModule,
		args: ['--langserver'],
		options: serverExecutableOptions
	};
	serverOptions.debug = {
		command: serverModule,
		args: ['--langserver', '--debug'],
		options: serverExecutableOptions
	};
}

async function pingModule(module: string): Promise<boolean> {
	return new Promise<boolean>(resolve => {
		exec.exec(module + ' --ping', {timeout: 10000}, (error, stdout, stderr) => {
			if (error) resolve(false);
			resolve(stdout == 'Hello!');
		});
	});
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	isServerRunning = false;
	return client.stop();
}

var lastWorkshopOutput : string = null;

function addCommands(context: ExtensionContext)
{
	// Push provider.
	context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider('ow_ostw', workshopPanelProvider));

	// Download latest release
	context.subscriptions.push(vscode.commands.registerCommand('ostw.downloadLatestRelease', () => {
		downloadOSTW();
	}));

	// Virtual document
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

	// Locate server installation
	context.subscriptions.push(vscode.commands.registerCommand('ostw.locateServerInstallation', async () => {
		// Open a file picker to locate the server.
		let openedFiles = await vscode.window.showOpenDialog({canSelectMany: false, filters: { 'Application': ['exe', 'dll'] }});

		// 'opened' will be undefined if canceled.
		if (openedFiles == undefined) return;

		let opened = openedFiles[0];
		let ext = path.extname(opened.fsPath).toLowerCase();
		let module:string = null;

		if (ext == '.dll')
			module = getModuleCommand(opened.fsPath);
		else if (ext == '.exe')
			module = opened.fsPath;
		
		if (!await pingModule(module))
			vscode.window.showWarningMessage('Failed to ping the Overwatch Script To Workshop server.');

		await stopLanguageServer();
		setServerOptions(module);
		config.update('deltintegerPath', module, vscode.ConfigurationTarget.Global);
		startLanguageServer();

		// Determines if the file name is 'deltinteger'.
		// if (opened.fsPath.split('\\').pop().split('/').pop().split('.')[0].toLowerCase() != 'deltinteger')
	}));

	// Copy workshop code
	context.subscriptions.push(vscode.commands.registerCommand('ostw.copyWorkshopCode', () => {
		vscode.env.clipboard.writeText(lastWorkshopOutput);
	}))
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
		// Wait for the server to be ready.
		if (!await waitForServer()) return null;

		// Get the semantic tokens in the provided document from the language server.
		let tokens: {range: LSRange, tokenType:string, modifiers:string[]}[] = await client.sendRequest('semanticTokens', document.uri);

		// Create the builder.
		let builder:vscode.SemanticTokensBuilder = new vscode.SemanticTokensBuilder(legend);

		// Push tokens to the builder.
		for (const token of tokens) {
			builder.push(client.protocol2CodeConverter.asRange(token.range), token.tokenType, token.modifiers);
		}

		// Return the result.
		return builder.build();
	}
};

async function waitForServer(): Promise<boolean>
{
	if (isServerRunning) return true;
	return new Promise(resolve =>
		{
			onServerReady.event(() => {
				resolve(true);
			}, this);
			setTimeout(() => {
				resolve(false);
			}, 10000);
		}
	);
}

async function IsDotnetInstalled(): Promise<boolean>
{
	try
	{
		return new Promise<boolean>(resolve => {
			exec.exec('dotnet --list-runtimes', (error, stdout, stderr) => {
				if (error) resolve(false);
				resolve(/Microsoft\.NETCore\.App 3\.[0-9]+/.test(stdout));
			});
		});
	}
	catch (ex)
	{
		// An error may be thrown if the command does not exist.
		return false;
	}
}

async function downloadOSTW(): Promise<void>
{
	window.withProgress(
		{ location: vscode.ProgressLocation.Notification, title: 'Downloading the Overwatch Script To Workshop server.', cancellable: true },
		async(progress, token) => {
			try {
				await new Promise((resolve, reject) => {
					doDownload(token, successResponse => {
						resolve(successResponse);
					}, errorResponse => {
						reject(errorResponse)
					});
				});
			}
			// On error
			catch (ex) {
				vscode.window.showErrorMessage('Failed to download the OSTW server: ' + ex);
			}

			return null;
		}
	)
}

async function doDownload(token: vscode.CancellationToken, success, error)
{
	// Stop the server.
	await stopLanguageServer();

	// Get the downloadable url for the ostw server.
	const url: string = await getAssetUrl();

	if (url == null)
	{
		// Could not retrieve asset url.
		error('Could not get release assets, do you have a connection?');
		return;
	}

	let data = await cancelableGet(url, token);
	if (data == null)
	{
		success(null);
		return;
	}
	else if (typeof data == 'string')
	{
		error(data);
		return;
	}

	// Send previous installation to the trash.
	if (fs.existsSync(defaultServerFolder)) {
		try {
			await vscode.workspace.fs.delete(Uri.parse('file:///' + defaultServerFolder, true), {recursive: true, useTrash: true});
		}
		catch (ex) {
			window.showWarningMessage('Failed to delete previous server installation: ' + ex);
		}
	}

	await yauzl.fromBuffer(data, {lazyEntries: true}, async (err, zipfile) => {
		if (err) throw err;
		zipfile.readEntry();
		zipfile.on("entry", function(entry) {
			if (/\/$/.test(entry.fileName)) {
				// Directory file names end with '/'.
				// Note that entires for directories themselves are optional.
				// An entry's fileName implicitly requires its parent directories to exist.
				zipfile.readEntry();
			} else {
				// file entry
				zipfile.openReadStream(entry, function(err, readStream) {
					if (err) throw err;
					readStream.on("end", function() {
						zipfile.readEntry();
					});
					
					// The path to the file.
					let p = path.join(defaultServerFolder, entry.fileName);

					// Create the directory if it does not exist.
					ensureDirectoryExistence(p);

					// Create the write stream.
					let ws = fs.createWriteStream(p);
					ws.on('error', (e) => { console.error(e); });

					// Pipe the readStream into the write stream.
					readStream.pipe(ws);
				});
			}
		});
		await zipfile.once("end", async () => {
			// Extraction done.
			// Locate the DLL file.
			let executable = await locateDLL(defaultServerFolder);
			if (executable != null)
			{
				let newCommand = getModuleCommand(executable);
				setServerOptions(newCommand);

				// Update config.
				await config.update('deltintegerPath', newCommand, vscode.ConfigurationTarget.Global);

				// If updating the config does not start the client, start it now.
				startLanguageServer();

				// Done.
				success(newCommand);
			}
			else
			{
				error('deltinteger.dll not found within retrieved artifacts.');
			}
		});
	});
}

export async function cancelableGet(url: string, token: vscode.CancellationToken)
{
	// Set up the cancel token.
	const CancelToken = axios.CancelToken;
	let source = CancelToken.source();

	// When the progress bar is canceled, cancel the axios request.
	token.onCancellationRequested(e => {
		source.cancel(e);
	}, this);

	// Download the file.
	let response: any;

	try
	{
		response = await axios.get(url, {
			responseType: 'arraybuffer',
			cancelToken: source.token
		});
		return response.data;
	}
	catch (cancel)
	{
		// Canceled.
		return cancel.message;
	}
}

function getModuleCommand(module: string): string {
	return 'dotnet exec ' + module;
}

// Gets the latest release's download URL.
async function getAssetUrl(): Promise<string> {
	let assets: any[] = (await getLatestRelease())?.assets;
	if (assets == null) return null;

	for (const asset of assets) {
		if (path.extname(asset.name) != '.zip') continue;
		// TODO: more matches
		return asset.browser_download_url;
	}

	return null;
}

// Gets the latest release.
async function getLatestRelease() {
	try {
		return (await axios.get('https://api.github.com/repos/ItsDeltin/Overwatch-Script-To-Workshop/releases/latest')).data;
	}
	catch (ex) {
		return null;
	}
}

function ensureDirectoryExistence(filePath) {
	var dirname = path.dirname(filePath);
	if (fs.existsSync(dirname)) {
		return true;
	}
	ensureDirectoryExistence(dirname);
	fs.mkdirSync(dirname);
}

async function locateDLL(root: string): Promise<string>
{
	return new Promise<string>((resolve, reject) =>
		glob('**/deltinteger.dll', {cwd: root}, (error, matches: string[]) => {
			if (error || matches.length == 0) resolve(null);
			return resolve(path.join(root, matches[0]));
		})
	);
}