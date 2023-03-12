import { env, window, ConfigurationTarget, EventEmitter, OutputChannel, StatusBarItem, StatusBarAlignment, Uri, SourceBreakpoint } from 'vscode';
import { LanguageClient, LanguageClientOptions, Executable, ErrorHandler, ErrorAction, Message, CloseAction, ReferencesRequest, State } from 'vscode-languageclient/node';
import { defaultServerFolder, selector, addSubscribable, extensionContext, KEY_DOWNLOADED_SERVER_DATE } from './extensions';
import { config } from './config';
import { isDotnetInstalled, getLatestRelease, downloadLatest, locateAndApplyServerModule, getBinariesInfo, pingModule } from './download';
import { workshopPanelProvider } from './workshopPanelProvider';
import * as versionSelector from './versionSelector';
import fs = require('fs');

export let client: LanguageClient;

export let serverStatus: 'stopped' | 'starting' | 'started' | 'ready' = 'stopped';
export let onServerReady = new EventEmitter();
export let serverVersion: string;
let gotVersionThisInstance = false;

export let workshopOut: OutputChannel;
let elementCountStatus: StatusBarItem;
export var lastWorkshopOutput: string | undefined = undefined;

export async function makeLanguageServer() {
	workshopOut = window.createOutputChannel("Workshop Code");
	addSubscribable(workshopOut);

	// Shows element count.
	elementCountStatus = window.createStatusBarItem(StatusBarAlignment.Left, 0);
	elementCountStatus.tooltip = "The number of elements in the workshop output. The workshop will accept a maximum of 32,000.";
	elementCountStatus.show();
	setElementCount(0);
	startLanguageServer();
}

async function checkServerModule() {
	serverStatus = 'starting';
	gotVersionThisInstance = false;

	// Gets the path to the server executable.
	let serverModule: string | undefined = <string>config.get('deltintegerPath');

	// Confirm the serverModule.
	if (serverModule == undefined || serverModule == '') {
		serverModule = await locateAndApplyServerModule(defaultServerFolder, new Date(0));

		// If serverModule is not set, locate the dll at its default location.
		if (!serverModule) {
			versionSelector.setCurrentVersion('OSTW server not installed');
			// Ask the user if they want to install the OSTW server.
			window.showWarningMessage('The Overwatch Script To Workshop server was not found.', 'Automatically Install Latest', 'View Releases')
				.then(option => {
					// Download OSTW
					if (option == 'Automatically Install Latest') downloadLatest();
					// View releases
					if (option == 'View Releases') env.openExternal(Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/releases'));
				});

			serverStatus = 'stopped';
			return false;
		}
	}

	// Get the binaries info of ostw.
	let binaries = await getBinariesInfo(serverModule).catch(reason => {
		window.showWarningMessage('Failed to retrieve version info: ' + reason);
		return undefined;
	});
	gotVersion(binaries?.version);
	checkForNewReleases();

	return await pingModule(serverModule).catch(e => {
		window.showErrorMessage('Failed to ping OSTW: ' + e);
		serverStatus = 'stopped';
		return false;
	});
}

export async function startLanguageServer() {
	// If the server is running, or the server cannot be started, or the command server option is invalid, return.
	if (serverStatus != 'stopped' || !await checkServerModule()) return;

	let waitForDebugger = config.get<string>('ostw.dev.waitForDebugger');
	if (waitForDebugger)
		window.showWarningMessage('The setting \'ostw.dev.waitForDebugger\' is turned on.');

	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [selector],
		synchronize: {
			configurationSection: 'ostw'
		}
	};

	// It was me, stdio!
	let serverExecutableOptions = { stdio: "pipe", detached: false, shell: <boolean>config.get('deltintegerShell') };
	let serverOptions: { run: Executable; debug: Executable; } = {
		run: {
			command: <string>config.get('deltintegerPath'),
			args: ['--langserver', ...waitForDebugger ? ['--waitfordebugger'] : []],
			options: serverExecutableOptions
		},
		debug: {
			command: <string>config.get('deltintegerPath'),
			args: ['--langserver', '--debug', ...waitForDebugger ? ['--waitfordebugger'] : []],
			options: serverExecutableOptions
		}
	};

	// Create the language client and start the client.
	client = new LanguageClient('ostw', 'Overwatch Script To Workshop', serverOptions, clientOptions);

	client.onDidChangeState(s => {
		if (s.newState == State.Running) {
			clientReady();
		}
	}, this);
	client.start();
	serverStatus = 'started';
}

function clientReady() {
	serverStatus = 'ready';
	onServerReady.fire(null);

	// When the client is ready, setup the workshopCode notification.
	client.onNotification("workshopCode", (code: string) => {
		if (code != lastWorkshopOutput) {
			lastWorkshopOutput = code;
			workshopPanelProvider.onDidChangeEmitter.fire(Uri.parse('ow_ostw:Workshop Output.ow'));

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
	// client.onNotification("version", gotVersion);
}

export async function stopLanguageServer() {
	if (serverStatus == 'stopped') return;
	serverStatus = 'stopped';
	if (client.needsStop()) await client.stop();
}

export async function restartLanguageServer(timeout: number = 5000) {
	await stopLanguageServer();
	await new Promise<void>(resolve => setTimeout(() => resolve(), timeout));
	await startLanguageServer();
}

async function gotVersion(currentVersion: string | undefined) {
	if (gotVersionThisInstance)
		return;
	gotVersionThisInstance = true;

	if (currentVersion) {
		serverVersion = currentVersion;
		versionSelector.setCurrentVersion('OSTW ' + currentVersion);
	}
}

async function checkForNewReleases() {
	// Do not show the message if the newRelease config is false.
	if (!config.get('newRelease')) return;

	let current_date = extensionContext.globalState.get<Date>(KEY_DOWNLOADED_SERVER_DATE);

	// Get the latest release. Since this is more for convenience when typescript
	// is started, silently swallow any errors that may occur.
	let latestRelease = await getLatestRelease().catch(e => { return undefined; });
	if (latestRelease == undefined) return;

	// Get the name, date, and url.
	let latestDate = new Date(latestRelease.created_at);
	let name: string = latestRelease.name;
	let url: string = latestRelease.html_url;

	if ((!current_date || current_date < latestDate) && config.get('ignoreRelease') != name) {
		window.showInformationMessage(
			// Message
			"A new version of Overwatch Script To Workshop (" + name + ") is now available.",
			// Options
			"Download release", "Ignore release", "View release"
		).then(chosenOption => {
			// Download the release.
			if (chosenOption == "Download release")
				downloadLatest();
			// Open the release.
			else if (chosenOption == "View release")
				env.openExternal(Uri.parse(url));
			// Don't show again for this version.
			else if (chosenOption == "Ignore release")
				config.update('ignoreRelease', name, ConfigurationTarget.Global);
		});
	}
}

function setElementCount(count) {
	elementCountStatus.text = "Element count: " + count + " / 32000";
}

export function resetLastWorkshopOutput() {
	lastWorkshopOutput = '';
}