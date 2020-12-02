import exec = require('child_process');
import { serverModuleCommand, client } from './extensions';
import * as vscode from 'vscode';

export function decompileClipboard()
{
    vscode.window.showSaveDialog({
        filters: {
            'Deltinscript': ['ostw', 'del', 'workshop']
        }
    }).then((uri: vscode.Uri) => {
        if (uri == undefined) return; // Canceled

        client.sendRequest('decompile.file', {file: uri.fsPath}).then((value: {success: boolean, msg: string}) => {
            if (value.success) {
                vscode.workspace.openTextDocument(uri).then(document => {
                    vscode.window.showTextDocument(document);
                });
            }
            else
                vscode.window.showErrorMessage(value.msg);
        });
    });
}

export function insertActions(textEditor: vscode.TextEditor)
{
    client.sendRequest('decompile.insert').then((value: {success: boolean, code: string}) => {
        if (value.success)
        {   
            let snippet: vscode.SnippetString = new vscode.SnippetString(value.code);
            textEditor.insertSnippet(snippet);
        }
        else
        {
            vscode.window.showErrorMessage(value.code);
        }
    });
}