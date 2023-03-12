import * as os from 'os';
import * as path from 'path';
import * as chokidar from 'chokidar';
import { window } from 'vscode';
import * as fs from 'fs';
import * as fspath from 'path';
import { addSubscribable } from './extensions';
import { Disposable } from 'vscode'; 
import { config } from './config';

const workshopLogPath: string = path.join(os.homedir(), 'Documents', 'Overwatch', 'Workshop');

/** The predicted file that the Overwatch Workshop is writing to. */
let currentLogPath: string | undefined = undefined;
/** The end of the current log file. When the file updates, `dataStart` to the end of the file will
 * need to be written to the output. */
let dataStart: number;
/** The `Disposable` used to close the workshop log window. */
let disposeLog: Disposable | undefined = undefined;
/** Is the log window active? */
let active: Boolean = false;

/** Opens or closes the workshop log output window depending on the `ostw.workshopLog` setting. */
export function tryLogFolder()
{
    if (config.get<Boolean>('workshopLog'))
        watchLogFolder();
    else
        stopWatchingLogFolder();
}

function watchLogFolder()
{
    if (active)
        return;
    active = true;

    let logWindow = window.createOutputChannel('Workshop Log', 'log');

    let watcher = chokidar.watch(workshopLogPath, {
        persistent: true,
        usePolling: true, // poll because of the way Overwatch writes its logs.
        ignoreInitial: false, // do not ignore so we can predict the current log file.
        alwaysStat: true
    }).on('change', (path, stats) => {
        // The 'add' event determines which log file is the most recent and by extension which file
        // Overwatch will log to. If that chooses the wrong file for some reason, the 'if' block will
        // switch the file currently being watched to the new log file. This will have the side effect
        // of ignoring the first few new logs. This shouldn't usually happen, unless the user changes
        // their system time.
        if (currentLogPath != path) {
            currentLogPath = path;
            dataStart = stats?.size ?? 0;
            logWindow.appendLine('switched to file ' + path);
        }
        else
        {
            fs.readFile(path, { encoding: 'utf-8', flag: 'r' }, (err, data) => {
                if (err) {
                    // Failed to read file content.
                    // It might be a good idea to prepend [error] for the log format.
                    logWindow.appendLine(err.message);
                } else {
                    // 1. Jump to the current log position (dataStart)
                    // 2. Trim extra whitespace
                    // 3. Switch from [xx:yy:zz] to xx:yy:zz timestamp format.
                    logWindow.appendLine(
                        data.substring(dataStart)
                            .trim()
                            .replace(/^\[([0-9]+:[0-9]+:[0-9]+)\]/gm, '$1'));
                    dataStart = data.length;
                }
            });
        }
    }).on('add', (path, stats) => {
        // Because 'ignoreInitial' is false, this will execute immediately for each existing log file.
        // A new log file was found.
        const timestamp = getTimestampFromLogPath(path);
        const currentTimestamp = currentLogPath ? getTimestampFromLogPath(currentLogPath) : 0;

        // If the new log file is newer than the log file currently being watched, watch it.
        if (!currentLogPath || currentTimestamp < timestamp)
        {
            currentLogPath = path;
            dataStart = stats?.size ?? 0;
        }
    });

    disposeLog = new Disposable(() => {
        active = false;
        watcher.close();
        logWindow.dispose();
    })

    addSubscribable(logWindow);
    addSubscribable(disposeLog);
}

function stopWatchingLogFolder()
{
    if (active)
        disposeLog?.dispose();
}

/**
 * Gets the timestamp in the workshop log file name and converts it to UTC time.
 * @param path The path of the workshop log file. This should look something like `A:/.../Log-2023-03-11-13-38-13.txt`.
 * @returns The timestamp as UTC time, or zero if conversion fails.
 */
function getTimestampFromLogPath(path: string): number
{
    path = fspath.basename(path);

    const match = path.match(/[0-9]+/g);
    if (match && match.length >= 6) {
        const year = Number.parseInt(match[0]);
        const month = Number.parseInt(match[1]);
        const day = Number.parseInt(match[2]);
        const hour = Number.parseInt(match[3]);
        const minute = Number.parseInt(match[4]);
        const second = Number.parseInt(match[5]);

        // the 'month' variable will be 1-12, but Date.UTC expects 0-11.
        return Date.UTC(year, month - 1, day, hour, minute, second);
    }
    return 0;
}