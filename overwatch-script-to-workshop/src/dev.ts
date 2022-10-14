import { window } from 'vscode';
import { config } from './config';
import { restartLanguageServer } from './languageServer';
import chokidar = require('chokidar');

let watcher: chokidar.FSWatcher | null;

export function setupBuildWatcher() {
    if (watcher != null) {
        watcher.close();
        watcher = null;
    }

    let watch = config.get<string>('dev.deltintegerWatchBuild');
    if (watch != null && watch != '') {
        watcher = chokidar.watch(watch, { atomic: 1000, ignoreInitial: true })
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

async function watchUpdated() {
    window.showInformationMessage('Deltinteger dev watch file was changed, restarting server.');
    await restartLanguageServer();
}