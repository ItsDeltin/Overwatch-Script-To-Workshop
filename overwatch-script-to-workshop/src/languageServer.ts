import { env, window, ConfigurationTarget, EventEmitter, OutputChannel, StatusBarItem, StatusBarAlignment, Uri } from 'vscode';
import { LanguageClient, LanguageClientOptions, Executable, ErrorHandler, ErrorAction, Message, CloseAction } from 'vscode-languageclient';
import { defaultServerFolder, selector, addSubscribable } from './extensions';
import { config } from './config';
import { locateDLL, getModuleCommand, isDotnetInstalled, getLatestRelease, downloadLatest } from './download';
import { workshopPanelProvider } from './workshopPanelProvider';
import * as versionSelector from './versionSelector';

export let client: LanguageClient;

let isServerRunning = false;
let canBeStarted = false;
let clientStartInstance;
export let isServerReady = false;
export let onServerReady = new EventEmitter();
export let serverModuleCommand: string;
export let serverVersion: string;

export let workshopOut: OutputChannel;
let elementCountStatus: StatusBarItem;
export var lastWorkshopOutput : string = null;

export async function makeLanguageServer()
{
	workshopOut = window.createOutputChannel("Workshop Code");
	addSubscribable(workshopOut);

	// Shows element count.
	elementCountStatus = window.createStatusBarItem(StatusBarAlignment.Left, 0);
	elementCountStatus.tooltip = "The number of elements in the workshop output. The workshop will accept a maximum of 20,000.";
	elementCountStatus.show();
	setElementCount(0);

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
			versionSelector.setCurrentVersion('OSTW server not installed');
			// Not found at the default location.
			doStart = false;
			// Ask the user if they want to install the OSTW server.
			window.showWarningMessage('The Overwatch Script To Workshop server was not found.', 'Automatically Install Latest', 'View Releases')
				.then(option => {
					// Download OSTW
					if (option == 'Automatically Install Latest') downloadLatest();
					// View releases
					if (option == 'View Releases') env.openExternal(Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/releases'));
				})
		}
		else {
			serverModule = getModuleCommand(findInstallLocation);
			// Was found at the default location, update config.
			config.update('deltintegerPath', serverModule, ConfigurationTarget.Global);
		}
	}

	// Update the server options.
	setServerOptions(serverModule);

	// Confirm that dotnet is installed.
	if (!await isDotnetInstalled())
	{
		doStart = false;
		canBeStarted = false;
		window.showWarningMessage('Overwatch Script To Workshop requires .Net Core 3.1 to be installed.', 'View Download Page')
			.then(option => {
				// View dotnet
				if (option == 'View Download Page') env.openExternal(Uri.parse('https://dotnet.microsoft.com/download/dotnet-core/current/runtime'));
			});
	}
	else
	{
		canBeStarted = true;
		if (doStart) startLanguageServer();
	}
}

export function startLanguageServer() {
	// If the server is running, or the server cannot be started, or the command server option is invalid, return.
	if (isServerRunning || !canBeStarted || serverModuleCommand == null || serverModuleCommand == '') return;

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
			command: serverModuleCommand,
			args: ['--langserver', ...waitForDebugger ? ['--waitfordebugger'] : []],
			options: serverExecutableOptions
		},
		debug: {
			command: serverModuleCommand,
			args: ['--langserver', '--debug', ...waitForDebugger ? ['--waitfordebugger'] : []],
			options: serverExecutableOptions
		}
	};
	
	// Create the language client and start the client.
	client = new LanguageClient(
		'ostw',
		'Overwatch Script To Workshop',
		serverOptions,
		clientOptions
	);

	client.onReady().then(clientReady);
	clientStartInstance = client.start();
	isServerRunning = true;
}

function clientReady()
{
	isServerReady = true;
	onServerReady.fire(null);

	// When the client is ready, setup the workshopCode notification.
	client.onNotification("workshopCode", (code: string) => {
		if (code != lastWorkshopOutput)
		{
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
	client.onNotification("version", async (version: string) => {
		serverVersion = version;
		versionSelector.setCurrentVersion('OSTW ' + version);

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
					downloadLatest();
				// Open the release.
				else if (chosenOption == "View release")
					env.openExternal(Uri.parse(url));
				// Don't show again for this version.
				else if (chosenOption == "Ignore release")
					config.update('ignoreRelease', latest, ConfigurationTarget.Global);
			});
		}
	});
}

export async function stopLanguageServer() {
	if (!isServerRunning) return;
	isServerReady = false;
	await client.stop();
	clientStartInstance.dispose();
	isServerRunning = false;
}

export async function restartLanguageServer(timeout:number = 5000) {
	await stopLanguageServer();
	await new Promise<void>(resolve => setTimeout(() => resolve(), timeout));
	startLanguageServer();
}

export function setServerOptions(serverModule: string)
{
	serverModuleCommand = serverModule;
}

function setElementCount(count)
{
	elementCountStatus.text = "Element count: " + count + " / 20000";
}

export function resetLastWorkshopOutput()
{
	lastWorkshopOutput = '';
}