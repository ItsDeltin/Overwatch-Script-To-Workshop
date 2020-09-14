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

        exec.exec(serverModuleCommand + ' --decompile-clipboard "' + uri.fsPath + '"', {timeout: 10000}, (error, stdout, stderr) => {
            if (stdout == 'Success') {
                vscode.workspace.openTextDocument(uri).then(document => {
                    vscode.window.showTextDocument(document);
                });
            }
            else vscode.window.showErrorMessage(stdout);
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