import { window, workspace, CancellationToken, ConfigurationTarget, ProgressLocation, Uri } from 'vscode';
import exec = require('child_process');
import fs = require('fs');
import yauzl = require("yauzl");
import axios from 'axios';
import glob = require('glob');
import path = require('path');
import { defaultServerFolder } from './extensions';
import { config } from './config';
import { startLanguageServer, stopLanguageServer } from './languageServer';
import process = require('process');

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
			if (await locateAndApplyServerModule(defaultServerFolder))
			{
				// If updating the config does not start the client, start it now.
				await startLanguageServer();

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

async function cancelableGet(url: string, token: CancellationToken)
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

	// No zip assets
	if (urls.length == 0) return null;
	// Only one zip asset
	if (urls.length == 1) return urls[0];

	// Get default installation.
	let dotnetInstalled = await isDotnetInstalled();
	for (let i = 0; i < names.length; i++) {
		let fileName = names[i].replace(/\.[^/.]+$/, "");

		// Cross-platform
		if (dotnetInstalled && fileName.endsWith('crossplatform'))
			return urls[i]
		// Win x32
		else if (fileName.endsWith('win-x86') && process.arch == 'x32' && process.platform == 'win32')
			return urls[i];
		// Win x64
		else if (fileName.endsWith('win-x64') && process.arch == 'x64' && process.platform == 'win32')
			return urls[i];
		// Linux x64
		else if (fileName.endsWith('linux-x64') && process.arch == 'x64' && process.platform == 'linux')
			return urls[i];

	}

	let selected:string = await window.showQuickPick(names, {canPickMany: false, placeHolder: 'Download release', ignoreFocusOut: true}, token);
	if (selected == undefined) return null;
	return urls[names.indexOf(selected)];
}

// Gets the latest release.
export async function getLatestRelease() {
	let releases = await getReleases();
	if (releases != null) return releases[0];
	return null;
}

export async function getReleases(): Promise<any[]> {
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

export async function locateAndApplyServerModule(root: string): Promise<boolean>
{
	let module = await getServerModuleFromFolder(root);
	if (!module)
		return false;
		
	await config.update('deltintegerPath', module, ConfigurationTarget.Global);
	return true;
}

async function getServerModuleFromFolder(root: string): Promise<string> {
	let pattern = process.platform == 'win32' ? '**/Deltinteger.@(dll|exe)' : '**/Deltinteger?(.dll)'

	return new Promise<string>((resolve, reject) => glob(pattern, {cwd: root, nocase: true}, async (error, matches: string[]) => {
		if (matches.length == 0)
		{
			resolve(null);
			return;
		}
		
		for (const match of matches) {
			let result = getServerModuleFromFile(path.join(root, match));
			if (result)
			{
				resolve(result);
				return;
			}
		}
		resolve(null);
	}));
}

export function getServerModuleFromFile(file: string): string {
	let ext = path.extname(file).toLowerCase();
	if (ext == '.dll') return 'dotnet exec "' + file + '"';
	else if (ext == '') return file;
	else if (ext == '.exe') return file;
	return null;
}

export async function getVersionInfo(file: string): Promise<{arch:string, version:string}>
{
	let versionFile = path.join(path.dirname(file), 'Version');
	let def = {
		arch: 'crossplatform',
		version: 'unprovided'
	};

	if (!fs.existsSync(versionFile))
		return def;
	
	try
	{
		return await new Promise((resolve, reject) => fs.readFile(versionFile, 'utf8', (err, data) => {
			if (err)
			{
				reject(err);
				return;
			}

			let split = data.split(/\r?\n/);
			if (split.length < 2)
			{
				reject('Invalid version file');
				return;
			}

			resolve({ arch: split[0], version: split[1] });
		}));
	}
	catch (ex)
	{
		window.showErrorMessage('Failed to retrieve version info: ' + ex);
		return def;
	}
}

export async function chooseServerLocation()
{
	// Open a file picker to locate the server.
	let openedFiles = await window.showOpenDialog({canSelectMany: false, filters: { 'Application': ['exe', 'dll', ''] }});

	// 'opened' will be undefined if canceled.
	if (openedFiles == undefined) return;

	let opened = openedFiles[0];
	let module:string = getServerModuleFromFile(opened.fsPath);

	if (!module)
	{
		window.showWarningMessage('Selected file is not a valid target.');
		return;
	}
	else
	{
		await config.update('deltintegerPath', module, ConfigurationTarget.Global);
		await stopLanguageServer();
		await startLanguageServer();
	}
}

export async function pingModule(module: string): Promise<boolean> {
	return new Promise<boolean>(resolve => {
		exec.exec(module + ' --ping', {timeout: 10000}, (error, stdout, stderr) => {
			if (error) resolve(false);
			resolve(stdout == 'Hello!');
		});
	});
}