import { ExtensionContext, window } from 'vscode';
import { config, startLanguageServer, stopLanguageServer, restartLanguageServer } from './extensions';
import fs = require('fs');
import chokidar = require('chokidar');

let watcher: chokidar.FSWatcher;

export function setupBuildWatcher() {
    let watch = config.get<string>('dev.deltintegerWatchBuild');
    if (watch != null && watch != '')
    {
        watcher = chokidar.watch(watch, {atomic: 1000, ignoreInitial: true})
            .on('add', () => {
                watchUpdated();
            })
            .on('change', () => {
                watchUpdated();
            })
            .on('unlink', () => {
                watchUpdated();
            });
    }
}

async function watchUpdated()
{
    window.showInformationMessage('Deltinteger dev watch file was changed, restarting server.');
    await restartLanguageServer();
}