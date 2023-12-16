import { window, workspace, CancellationToken, ConfigurationTarget, ProgressLocation, Uri, Progress, QuickPickItem } from 'vscode';
import exec = require('child_process');
import fs = require('fs');
import { fromBuffer } from 'yauzl';
const axios = require('axios');
const util = require('util');
const glob = util.promisify(require('glob'));
import path = require('path');
import { defaultServerFolder, extensionContext, KEY_DOWNLOADED_SERVER_DATE } from './extensions';
import { config } from './config';
import { restartLanguageServer, startLanguageServer, stopLanguageServer } from './languageServer';
import { Asset, Release } from "./githubApi";
import process = require('process');

type OstwProgress = Progress<{ message?: string; increment?: number }>;

/**
 * Determines if dotnet is installed.
 */
export async function isDotnetInstalled(): Promise<boolean> {
	try {
		return new Promise<boolean>(resolve => {
			exec.exec('dotnet --list-runtimes', (error, stdout, stderr) => {
				if (error) resolve(false);
				resolve(/Microsoft\.NETCore\.App 3\.[0-9]+/.test(stdout));
			});
		});
	}
	catch (ex) {
		// An error may be thrown if the command does not exist.
		return false;
	}
}

/**
 * Downloads the latest OSTW release.
 */
export async function downloadLatest() {
	await progressBarDownload(async (token, progress, resolve, reject) => {
		// Get the downloadable url for the ostw server.
		await assetFromRelease(await getLatestRelease(), token).then(async asset => {
			if (asset == undefined) {
				// Could not retrieve asset url.
				reject('Could not get release assets, do you have a connection?');
				return;
			}

			await doDownload(asset, token, progress, resolve, reject);
		}, err => {
			reject("Error getting release assets: " + err);
		});
	})
}

/**
 * Shows the Language Server download progress bar.
 * @param action The action that will download the language server.
 */
export async function progressBarDownload(
	action: (
		token: CancellationToken,
		progress: OstwProgress,
		resolve: () => void, reject: (reason: string) => void
	) => void
): Promise<void> {
	window.withProgress(
		{ location: ProgressLocation.Notification, title: 'Downloading the Overwatch Script To Workshop server.', cancellable: true },
		async (progress, token) => {
			try {
				await new Promise<void>((resolve, reject) => action(token, progress, resolve, reject));
			}
			// On error
			catch (ex) {
				window.showErrorMessage('Failed to download the OSTW server: ' + ex);
			}

			return null;
		}
	)
}

/**
 * Downloads, unpacks, and starts the OSTW language server.
 * @param asset The url of the language server release asset.
 * @param token The token that can cancel the download.
 * @param progress Report download progress.
 * @param success Called when the download is successful.
 * @param error Called when there is an error.
 */
export async function doDownload(
	asset: OstwAsset,
	token: CancellationToken,
	progress: OstwProgress,
	success: () => void,
	error: (msg: String) => void) {
	progress.report({ message: '(1/6) Stopping OSTW server...', increment: 15 });

	// Stop the server.
	await stopLanguageServer();

	progress.report({ message: '(2/6) Getting release from github...', increment: 30 });

	let data = await cancelableGet(asset.download_url, token);
	if (data == null) {
		success();
		return;
	}
	else if (typeof data == 'string') {
		error(data);
		return;
	}

	progress.report({ message: '(3/6) Deleting current installation...', increment: 45 });

	// Send previous installation to the trash.
	if (fs.existsSync(defaultServerFolder)) {
		try {
			await workspace.fs.delete(Uri.file(defaultServerFolder), { recursive: true, useTrash: false });
		}
		catch (ex) {
			error('Failed to delete previous server installation: ' + ex);
			return;
		}
	}

	progress.report({ message: '(4/6) Unpacking...', increment: 60 });

	fromBuffer(data, { lazyEntries: true }, async (err, zipfile) => {
		if (err) {
			error(err.message);
			return;
		}
		zipfile.readEntry();
		zipfile.on("entry", function (entry) {
			if (/\/$/.test(entry.fileName)) {
				// Directory file names end with '/'.
				// Note that entires for directories themselves are optional.
				// An entry's fileName implicitly requires its parent directories to exist.
				zipfile.readEntry();
			} else {
				// file entry
				zipfile.openReadStream(entry, function (err, readStream) {
					if (err) throw err;
					readStream.on("end", function () {
						zipfile.readEntry();
					});

					// The path to the file.
					let p = path.join(defaultServerFolder, entry.fileName);

					// Create the directory if it does not exist.
					ensureDirectoryExistence(p);

					// Create the write stream.
					let ws = fs.createWriteStream(p, { mode: 0o777 }); // mode: 0x777 allows execution
					ws.on('error', (e) => { console.error(e); });

					// Pipe the readStream into the write stream.
					readStream.pipe(ws);
				});
			}
		});
		await zipfile.once("end", async () => {
			try {
				progress.report({ message: '(5/6) Applying server module...', increment: 75 });
				// Extraction done.
				// Locate the DLL file.
				if (await locateAndApplyServerModule(defaultServerFolder, asset.date)) {
					progress.report({ message: '(6/6) Starting language server...', increment: 90 });

					// If updating the config does not start the client, start it now.
					await startLanguageServer();

					// Done.
					success();
				}
				else {
					error('deltinteger.dll not found within retrieved artifacts.');
				}
			}
			catch (ex) {
				error(ex)
			}
		});
	});
}

/**
 * Executes an http request that can be canceled with a `CancellationToken`.
 * @param url The url of the request.
 * @param token The token that is used to cancel the http request.
 * @returns the http response data.
 */
async function cancelableGet(url: string, token: CancellationToken): Promise<any> {
	// Set up the cancel token.
	const CancelToken = axios.CancelToken;
	let source = CancelToken.source();

	// When the progress bar is canceled, cancel the axios request.
	token?.onCancellationRequested(e => {
		source.cancel(e);
	}, this);

	// Download the file.
	let response: any;

	try {
		response = await axios.get(url, {
			responseType: 'arraybuffer',
			cancelToken: source.token
		});
		return response.data;
	}
	catch (cancel) {
		// Canceled.
		// Todo: caller may need to know that this was canceled.
		return cancel.message;
	}
}

/**
 * OSTW github releases have multiple assets for each architecture. This chooses the appropriate one given the environment.
 * If a suitable asset can't be found automatically, a quick pick is shown so the user can choose manually.
 * @param assets An array of assets.
 * @param token Used to cancel the quickpick if the download was cancelled.
 * @returns The URL of the chosen asset.
 */
export async function chooseAsset(assets: Asset[], token: CancellationToken | undefined = undefined): Promise<string | undefined> {
	if (assets == undefined) return undefined;

	let names: string[] = [];
	let urls: string[] = [];

	for (const asset of assets) {
		if (path.extname(asset.name) != '.zip') continue;

		names.push(asset.name);
		urls.push(asset.browser_download_url);
	}

	// No zip assets
	if (urls.length == 0) return undefined;
	// Only one zip asset
	if (urls.length == 1) return urls[0];

	// Get default installation.
	// let dotnetInstalled = await isDotnetInstalled();
	for (let i = 0; i < names.length; i++) {
		let fileName = names[i].replace(/\.[^/.]+$/, "");

		// Win x64
		if (fileName.endsWith('win-x64') && process.arch == 'x64' && process.platform == 'win32')
			return urls[i];
		// Linux x64
		else if (fileName.endsWith('linux-x64') && process.arch == 'x64' && process.platform == 'linux')
			return urls[i];
	}

	let selected: string | undefined = (await window.showQuickPick(
		names,
		{ canPickMany: false, placeHolder: 'Download release', ignoreFocusOut: true },
		token));

	if (selected == undefined) return undefined;
	return urls[names.indexOf(selected)];
}

/**
 * Gets the list of releases from github. The promise may error with the get request.
 * @returns The releases from the github api.
 */
export async function getReleases(): Promise<Release[] | undefined> {
	return (await axios.get('https://api.github.com/repos/ItsDeltin/Overwatch-Script-To-Workshop/releases')).data;
}

/**
 * Gets the latest release from github. The promise may error with the get request.
 * @returns The latest release from the github api.
 */
export async function getLatestRelease() {
	let releases = await getReleases();
	if (releases != undefined && releases.length > 0) return releases[0];
	return undefined;
}

/**
 * Gets a downloadable asset from a github release.
*/
export async function assetFromRelease(release: Release | undefined, token: CancellationToken): Promise<OstwAsset | undefined> {
	if (!release)
		return undefined;

	let asset = await chooseAsset(release.assets);
	if (!asset)
		return undefined;

	return {
		download_url: asset,
		date: new Date(release.created_at)
	};
}

function ensureDirectoryExistence(filePath) {
	var dirname = path.dirname(filePath);
	if (fs.existsSync(dirname)) {
		return true;
	}
	ensureDirectoryExistence(dirname);
	fs.mkdirSync(dirname);
}

export async function locateAndApplyServerModule(root: string, date: Date): Promise<string | undefined> {
	let module = await getServerModuleFromFolder(root);
	if (!module)
		return undefined;

	await config.update('deltintegerPath', module, ConfigurationTarget.Global);
	await extensionContext.globalState.update(KEY_DOWNLOADED_SERVER_DATE, date);
	return module;
}

/**
 * Give a directory, gets a command that can be used to execute the language server.
 * @param root The root folder whose children will be checked.
 * @returns The command that runs the language server.
 */
async function getServerModuleFromFolder(root: string): Promise<string | undefined> {
	// Find the Version file.
	let versionGlob = await aglob('**/Version', { cwd: root });

	// If the version file is not found, use the fallback strategy.
	if (!versionGlob.matches || versionGlob.matches.length == 0) {
		let pattern = process.platform == 'win32' ? '**/Deltinteger.exe' : '**/Deltinteger'; // .exe for windows, no extension for linux.
		let generic = await aglob(pattern, { cwd: root, nocase: true }); // Execute glob.

		// Was the executable found?
		if (generic.matches && generic.matches.length > 0)
			return getServerModuleFromFile(path.join(root, generic.matches[0]));
		else
			return undefined;
	}

	// Loop through each version file found.
	// This should usually have one file.
	// TODO: Rather than assuming, it may be better to display a selector if there is more than one item.
	for (const file of versionGlob.matches) {
		let versionPath = path.join(root, file);
		let version = await getBinariesInfo(versionPath);

		// This isn't possible unless the version file is deleted inbetween
		// the glob and the getBinariesInfo call.
		if (!version) {
			continue;
		}

		let exec;
		switch (version.arch) {
			case 'win-x64':
			case 'win-x86':
				exec = 'Deltinteger.exe';
				break;

			case 'linux-x64':
				exec = 'Deltinteger';
				break
		}
		exec = path.join(path.dirname(versionPath), exec);

		if (fs.existsSync(exec))
			return getServerModuleFromFile(exec);
	}
	return undefined;
}

function aglob(pattern: string, options: any): Promise<{ error: string, matches: string[] }> {
	return new Promise((resolve, reject) => glob(pattern, options, (error, matches) => resolve({ error: error, matches: matches })));
}

export function getServerModuleFromFile(file: string): string {
	return file;
}

/**
 * The language server binaries have a file named 'Version' that contains the
 * version and the build architecture.
 * @param file The path to the Version file.
 * @param error 
 * @returns The arch and the version of the language server binaries.
 * + **arch**: should be one of the following: `win-x64`, `win-x86`, `linux-x64`.
 * + **version** will be `branch:[BRANCH_NAME]` if it is from an automated release
 * or with Semantic Versioning format like `v[MAJOR].[MINOR].[PATH]` for actual releases.
 * 
 * This function simply reads the input version file, which the user may change for some reason.
 */
export async function getBinariesInfo(file: string): Promise<{ arch: string, version: string } | undefined> {
	let versionFile = path.join(path.dirname(file), 'Version');
	if (!fs.existsSync(versionFile))
		return undefined;

	return await new Promise((resolve, reject) => {
		return fs.readFile(versionFile, 'utf8', (err, data) => {
			if (err) {
				reject(err);
				return;
			}

			let split = data.split(/\r?\n/);
			if (split.length < 2) {
				reject('Invalid version file');
				return;
			}

			resolve({ arch: split[0], version: split[1] });
		})
	});
}

export async function chooseServerLocation() {
	// Open a file picker to locate the server.
	let openedFiles = await window.showOpenDialog({ canSelectMany: false, filters: { 'Application': ['exe', 'dll', ''] } });

	// 'opened' will be undefined if canceled.
	if (openedFiles == undefined) return;

	let opened = openedFiles[0];
	let module: string = getServerModuleFromFile(opened.fsPath);

	if (!module) {
		window.showWarningMessage('Selected file is not a valid target.');
		return;
	}
	else {
		await config.update('deltintegerPath', module, ConfigurationTarget.Global);
		await restartLanguageServer(0);
	}
}

export async function pingModule(module: string): Promise<boolean> {
	return new Promise<boolean>((resolve, reject) => {
		exec.exec(module + ' --ping', { timeout: 10000 }, (error, stdout, stderr) => {
			if (error) reject(error);
			resolve(stdout == 'Hello!');
		});
	});
}

interface OstwAsset {
	download_url: string,
	date: Date,
}