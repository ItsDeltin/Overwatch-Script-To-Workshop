import { window, commands, workspace, env, Uri, StatusBarAlignment, StatusBarItem, ExtensionContext, TextDocument, QuickPickItem, ThemeColor } from 'vscode';
import * as download from './download';
import { serverVersion, restartLanguageServer, stopLanguageServer } from './languageServer'
import { openIssues, openDefaultServerDirectory } from './extensions'
import { Release } from "./githubApi"

let versionStatus: StatusBarItem;

export function createVersionStatusBar(context: ExtensionContext) {
    versionStatus = window.createStatusBarItem(StatusBarAlignment.Left, 0);
    versionStatus.text = 'OSTW $(chrome-close~spin)';
    versionStatus.tooltip = 'Select version';
    versionStatus.command = 'ostw.versionInfo';
    versionStatus.show();
    context.subscriptions.push(commands.registerCommand('ostw.versionInfo', async () => {

        let items: (QuickPickItem & { action: () => void })[] = [
            // Wiki
            { label: 'Wiki', action: () => env.openExternal(Uri.parse('https://github.com/ItsDeltin/Overwatch-Script-To-Workshop/wiki')) },
            // Report issues
            { label: 'Report an issue', action: openIssues },
            // Restart language server
            { label: 'Restart language server', action: restartLanguageServer },
            // Stop language server
            { label: 'Stop language server', action: stopLanguageServer },
            // Download new version option
            { label: 'Download new server version' + (serverVersion != null ? ' (current: ' + serverVersion + ')' : ''), action: selectVersion },
            // Choose server location
            { label: 'Choose server location', action: download.chooseServerLocation },
            // Open default server directory
            { label: 'Open default server directory', action: openDefaultServerDirectory }
        ];

        let option = await window.showQuickPick(items);
        option?.action();
    }, this));
}

export function ostwNotEnabled() {
    versionStatus.text = 'OSTW $(x)';
}

export function setOstwVersion(version: string | undefined) {
    if (version) {
        versionStatus.text = `OSTW ${version} $(check)`;
    } else {
        versionStatus.text = `OSTW $(check)`;
    }
}

export function ostwLoading() {
    versionStatus.text = 'OSTW $(loading~spin)';
}

async function selectVersion() {
    await download.getReleases().then(async releases => {
        if (releases) {
            let items: (QuickPickItem & { release: Release })[] = [];
            for (const release of releases)
                items.push({
                    label: release.name,
                    detail: release.tag_name,
                    description: release.prerelease ? 'prerelease' : undefined,
                    release
                });

            let chosen = await window.showQuickPick(items);
            if (chosen) {
                let release = chosen.release; // type guard shenanigans
                download.progressBarDownload(async (token, progress, resolve, reject) => {
                    let url = await download.assetFromRelease(release, token);
                    if (url != null) {
                        await download.doDownload(url, token, progress, resolve, reject);
                    }
                });
            }
        }
    }, err => {
        window.showErrorMessage('Error getting release assets: ' + err);
    });
}