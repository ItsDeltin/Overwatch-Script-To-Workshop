import exec = require('child_process');
import { serverModuleCommand, client } from './languageServer';
import * as vscode from 'vscode';

export function decompileClipboard()
{
    vscode.window.showSaveDialog({
        filters: {
            'Deltinscript': ['ostw', 'del', 'workshop']
        }
    }).then((uri: vscode.Uri) => {
        if (uri == undefined) return; // Canceled

        client.sendRequest('decompile.file', {file: uri.fsPath}).then((value: any) => {
            handleDecompileResult(value, () => vscode.workspace.openTextDocument(uri).then(document => vscode.window.showTextDocument(document)));
        });
    });
}

export function insertActions(textEditor: vscode.TextEditor)
{
    client.sendRequest('decompile.insert').then((value: any) => {
        handleDecompileResult(value, () => {
            let snippet: vscode.SnippetString = new vscode.SnippetString(value.code);
            textEditor.insertSnippet(snippet);
        });
    });
}

async function handleDecompileResult(value, onSuccess: () => void)
{
    if (value.result == 'success')
        onSuccess();
    else if (value.result == 'incompleted')
    {
        decompilerOriginalWorkshopCode = value.original;
        showDecompileError(value);
        vscode.window.showWarningMessage('Failed to fully decompile the workshop code, stuck at line ' + value.range.start.line + ', column ' + value.range.start.character,
            'Show error location', 'Report on github')
            .then(option => {
                if (option == 'Show error location')
                    showDecompileError(value);
                else if (option == 'Report on github')
                    vscode.env.openExternal(vscode.Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/issues'))
            });
    }
    else if (value.result == 'exception')
        vscode.window.showErrorMessage('An exception was thrown while decompiling: ' + value.exception);
}

export let decompilerOriginalWorkshopCode: string;

async function showDecompileError(value)
{
    let uri = vscode.Uri.parse('ow_ostw:[decompile].ow');
    let doc : vscode.TextDocument = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(doc, { preview: false, selection: value.range });
}