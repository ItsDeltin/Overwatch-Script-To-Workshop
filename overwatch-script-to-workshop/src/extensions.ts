import * as vscode from 'vscode';
import { ExtensionContext, window, Uri, Position, Location } from 'vscode';
import { Position as LSPosition, Location as LSLocation } from 'vscode-languageclient';
import { register } from './debugger';
import { decompileClipboard, insertActions } from './decompile';
import { setupBuildWatcher } from './dev';
import path = require('path');
import exec = require('child_process');
import { client, makeLanguageServer, lastWorkshopOutput, restartLanguageServer } from './languageServer';
import { downloadLatest, chooseServerLocation } from './download';
import { setupConfig, config } from './config';
import { workshopPanelProvider } from './workshopPanelProvider';
import * as semantics from './semantics';
import { createVersionStatusBar } from './versionSelector';

export let extensionContext: ExtensionContext;
export let globalStoragePath:string;
export let defaultServerFolder:string;

export const selector = { language: 'ostw', scheme: 'file' };

export async function activate(context: ExtensionContext) {
	extensionContext = context;
	globalStoragePath = context.globalStoragePath;
	defaultServerFolder = path.join(globalStoragePath, 'Server');

	setupConfig();
	subscribe(context);
	createVersionStatusBar(context);
	makeLanguageServer();
	setupBuildWatcher();
	semantics.setupSemantics();
}

export function addSubscribable(disposable)
{
	extensionContext.subscriptions.push(disposable);
}

export function deactivate(): Thenable<void> | undefined {
	if (!client)
		return undefined;
	return client.stop();
}

function subscribe(context: ExtensionContext)
{
	register(context);
	
	// Push provider.
	context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider('ow_ostw', workshopPanelProvider));

	// Download latest release
	context.subscriptions.push(vscode.commands.registerCommand('ostw.downloadLatestRelease', () => {
		downloadLatest();
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
		client.sendRequest<boolean>('pathmapEditor', {Text: editPathmap, File: editPathmapFile}).then((result: any) => {
			// The request will return true if successful.
			// It can return false if PathfindEditor.del was tinkered with by the user (or there is a bug).
			if (result.success)
			{
				// Send a success message depending on if the editor is the default editor or the editor is editing a .pathmap file.
				if (editPathmapFile == null)
					vscode.window.showInformationMessage('Default pathmap editor copied to clipboard. Paste the rules in Overwatch to edit.');
				else
					vscode.window.showInformationMessage("Pathmap editor for '" + editPathmapFile + "' copied to clipboard. Paste the rules in Overwatch to edit.");
			}
			else
				vscode.window.showErrorMessage(result.reason);
		}, (reason: any) => {
			vscode.window.showErrorMessage(reason);
		});
	}, this));

	// Locate server installation
	context.subscriptions.push(vscode.commands.registerCommand('ostw.locateServerInstallation', chooseServerLocation));

	// Copy workshop code
	context.subscriptions.push(vscode.commands.registerCommand('ostw.copyWorkshopCode', () => {
		vscode.env.clipboard.writeText(lastWorkshopOutput);
	}));
	
	// Decompile clipboard
	context.subscriptions.push(vscode.commands.registerCommand('ostw.decompile.clipboard', () => {
		decompileClipboard();
	}));

	// Decompile clipboard and insert.
	context.subscriptions.push(vscode.commands.registerTextEditorCommand('ostw.decompile.insert', (textEditor, edit) => {
		insertActions(textEditor);
	}));

	// Restart language server.
	context.subscriptions.push(vscode.commands.registerCommand('ostw.restartLanguageServer', async () => {
		await restartLanguageServer(500);
	}));
}

export function openIssues()
{
	vscode.env.openExternal(vscode.Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/issues'));
}