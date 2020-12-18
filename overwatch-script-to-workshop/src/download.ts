import { window, workspace, CancellationToken, ConfigurationTarget, ProgressLocation, Uri } from 'vscode';
import exec = require('child_process');
import fs = require('fs');
import yauzl = require("yauzl");
import axios from 'axios';
import glob = require('glob');
import path = require('path');
import { defaultServerFolder } from './extensions';
import { config } from './config';
import { startLanguageServer, stopLanguageServer, setServerOptions } from './languageServer';

export async function isDotnetInstalled(): Promise<boolean>
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

export async function downloadLatest()
{
	await progressBarDownload(async (token, resolve, reject) => {
		// Get the downloadable url for the ostw server.
		const url: string = await getLatestAssetUrl(token);

		if (url == null)
		{
			// Could not retrieve asset url.
			reject('Could not get release assets, do you have a connection?');
			return;
		}
		
		doDownload(url, token, resolve, reject);
	})
}

export async function progressBarDownload(action: (token: CancellationToken, resolve: () => void, reject: (reason: string) => void) => void): Promise<void>
{
	window.withProgress(
		{ location: ProgressLocation.Notification, title: 'Downloading the Overwatch Script To Workshop server.', cancellable: true },
		async(progress, token) => {
			try {
				await new Promise<void>((resolve, reject) => action(token, resolve, reject));
			}
			// On error
			catch (ex) {
				window.showErrorMessage('Failed to download the OSTW server: ' + ex);
			}

			return null;
		}
	)
}

export async function doDownload(url: string, token: CancellationToken, success: () => void, error: (msg: String) => void)
{
	// Stop the server.
	await stopLanguageServer();

	let data = await cancelableGet(url, token);
	if (data == null)
	{
		success();
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
			await workspace.fs.delete(Uri.file(defaultServerFolder), {recursive: true, useTrash: true});
		}
		catch (ex) {
			error('Failed to delete previous server installation: ' + ex);
			return;
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
				await config.update('deltintegerPath', newCommand, ConfigurationTarget.Global);

				// If updating the config does not start the client, start it now.
				startLanguageServer();

				// Done.
				success();
			}
			else
			{
				error('deltinteger.dll not found within retrieved artifacts.');
			}
		});
	});
}

export async function cancelableGet(url: string, token: CancellationToken)
{
	// Set up the cancel token.
	const CancelToken = axios.CancelToken;
	let source = CancelToken.source();

	// When the progress bar is canceled, cancel the axios request.
	token?.onCancellationRequested(e => {
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

export function getModuleCommand(module: string): string {
	return 'dotnet exec "' + module +'"';
}

// Gets the latest release's download URL.
async function getLatestAssetUrl(token: CancellationToken): Promise<string> {
	return chooseAsset((await getLatestRelease())?.assets, token);
}
export async function chooseAsset(assets: any[], token: CancellationToken = null): Promise<string> {
	if (assets == null) return null;

	let names:string[] = [];
	let urls:string[] = [];

	for (const asset of assets) {
		if (path.extname(asset.name) != '.zip') continue;

		names.push(asset.name);
		urls.push(asset.browser_download_url);
	}

	if (urls.length == 0) return null;
	if (urls.length == 1) return urls[0];

	let selected:string = await window.showQuickPick(names, {canPickMany: false, placeHolder: 'Download release', ignoreFocusOut: true}, token);
	if (selected == undefined) return null;
	return urls[names.indexOf(selected)];
}

// Gets the latest release.
export async function getLatestRelease() {
	try {
		return (await axios.get('https://api.github.com/repos/ItsDeltin/Overwatch-Script-To-Workshop/releases/latest')).data;
	}
	catch (ex) {
		return null;
	}
}

export async function getReleases() {
	try {
		return (await axios.get('https://api.github.com/repos/ItsDeltin/Overwatch-Script-To-Workshop/releases')).data;
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

export async function locateDLL(root: string): Promise<string>
{
	return new Promise<string>((resolve, reject) =>
		glob('**/deltinteger.dll', {cwd: root, nocase: true}, (error, matches: string[]) => {
			if (error || matches.length == 0) resolve(null);
			return resolve(path.join(root, matches[0]));
		})
	);
}

export async function chooseServerLocation()
{
	// Open a file picker to locate the server.
	let openedFiles = await window.showOpenDialog({canSelectMany: false, filters: { 'Application': ['exe', 'dll'] }});

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
		window.showWarningMessage('Failed to ping the Overwatch Script To Workshop server.');

	await stopLanguageServer();
	setServerOptions(module);
	config.update('deltintegerPath', module, ConfigurationTarget.Global);
	startLanguageServer();
}

async function pingModule(module: string): Promise<boolean> {
	return new Promise<boolean>(resolve => {
		exec.exec(module + ' --ping', {timeout: 10000}, (error, stdout, stderr) => {
			if (error) resolve(false);
			resolve(stdout == 'Hello!');
		});
	});
}