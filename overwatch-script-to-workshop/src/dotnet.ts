import { Uri, window } from 'vscode';
import { cancelableGet } from './extensions';
import * as vscode from 'vscode';
import fs = require('fs');
import path = require('path');
import child_process = require('child_process');

export function downloadDotnet(tempDir: string)
{
    window.withProgress(
		{ location: vscode.ProgressLocation.Notification, title: 'Downloading installation script...', cancellable: true },
		async(progress, token) => {
			try {
				await new Promise((resolve, reject) => {
					doDownloadDotnet(tempDir, token, successResponse => {
						resolve(successResponse);
					}, errorResponse => {
						reject(errorResponse)
					});
				});
			}
			// On error
			catch (ex) {
				vscode.window.showErrorMessage('Failed to download dotnet: ' + ex);
            }

			return null;
		}
	)
}

async function doDownloadDotnet(tempDir: string, token: vscode.CancellationToken, success, error)
{
    let isWindows = process.platform === "win32";
    let url: string = isWindows ?
        // Windows powershell
        'https://dotnet.microsoft.com/download/dotnet-core/scripts/v1/dotnet-install.ps1':
        // Linux
        'https://dotnet.microsoft.com/download/dotnet-core/scripts/v1/dotnet-install.sh';

    let data = await cancelableGet(url, token);
    if (data == null)
    {
        success(null);
        return;
    }

    // Save the file.
    let scriptLocation = path.join(
        tempDir,
        isWindows ? 
            // Windows
            'dotnet.ps1':
            // Other
            'dotnet.sh'
    );
    await vscode.workspace.fs.writeFile(Uri.parse('file:///' + scriptLocation), data);
    success(null);

    let terminal = vscode.window.createTerminal('Download dotnet core');
    terminal.show();
    terminal.sendText(isWindows ?
        'powershell -ExecutionPolicy Bypass -File ' + scriptLocation:
        scriptLocation
    );
}