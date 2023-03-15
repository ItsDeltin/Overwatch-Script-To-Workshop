import * as chokidar from 'chokidar';
import { OutputChannel, window } from 'vscode';
import * as fs from 'fs';
import * as fspath from 'path';
import { addSubscribable } from './extensions';
import { Disposable } from 'vscode'; 
import { config } from './config';
import { exec } from 'child_process';

/** The predicted file that the Overwatch Workshop is writing to. */
let currentLogPath: string | undefined = undefined;
/** The end of the current log file. When the file updates, `currentLogCharacter` to the end of the file will
 * need to be written to the output. */
let currentLogCharacter: number | undefined;
/** The `Disposable` used to close the workshop log window. */
let disposeLog: Disposable | undefined = undefined;
/** Is the log window active? */
let active: Boolean = false;
/** The output channel that the workshop will log to. */
let logWindow: OutputChannel;
/** Saves the byte count of all the log files. */
let logFiles = new Map<string, {size:number}>();

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

    if (!logWindow) {
        logWindow = window.createOutputChannel('Workshop Log', 'log');
        addSubscribable(logWindow);
    }

    getWorkshopFolder(directory => {
        if (!directory) {
            logWindow.appendLine(errorMessage('Failed to locate the workshop log folder, please set the `ostw.workshopLogFolder` setting in Visual Studio Code.'));
            active = false;
            return;
        }
        logWindow.appendLine(infoMessage('Workshop directory: \'' + directory + '\''));
        
        let watcher = chokidar.watch(directory, {
            persistent: true,
            usePolling: true, // poll because of the way Overwatch writes its logs.
            ignoreInitial: false, // do not ignore so we can predict the current log file.
            alwaysStat: true
        }).on('change', (path, stats) => {
            // When the workshop writes to the log, currentLogPath should equal path.
            // In case it doesn't update the currentLogPath and set currentLogCharacter to undefined
            // so that it can be recalculated.
            if (currentLogPath != path) {
                currentLogPath = path;
                currentLogCharacter = undefined;
            }

            // 'lastSize' contains the size of the 'path' file before the update.
            // Save lastSize to the current size then update the current size.
            let lastSize = logFiles.get(path)?.size ?? 0;
            logFiles.set(path, { size: stats?.size ?? 0 });

            fs.readFile(path, { encoding: 'utf-8', flag: 'r' }, (err, data) => {
                if (err) {
                    logWindow.appendLine(errorMessage(err.message));
                } else {
                    if (currentLogCharacter === undefined) {
                        currentLogCharacter = characterIndexFromByte(data, lastSize);
                        logWindow.appendLine(infoMessage('Current log file: \'' + path + '\''));
                    }
                    // 1. Jump to the current log position (dataStart)
                    // 2. Trim extra whitespace
                    // 3. Switch from [xx:yy:zz] to xx:yy:zz timestamp format.
                    let content = data.substring(currentLogCharacter)
                        .trim()
                        .replace(/^\[([0-9]+:[0-9]+:[0-9]+)\]/gm, '$1');
                    // Only print if there is actually new content.
                    if (content != '')
                        logWindow.appendLine(content);
                    // Update current position.
                    currentLogCharacter = data.length;
                }
            });
        }).on('add', (path, stat) => {
            logFiles.set(path, { size: stat?.size ?? 0 });

            // Because 'ignoreInitial' is false, this will execute immediately for each existing log file.
            // A new log file was found.
            const timestamp = getTimestampFromLogPath(path);
            const currentTimestamp = currentLogPath ? getTimestampFromLogPath(currentLogPath) : 0;

            // If the new log file is newer than the log file currently being watched, watch it.
            if (!currentLogPath || currentTimestamp < timestamp)
            {    
                currentLogPath = path;
                currentLogCharacter = undefined;
            }
        }).on('error', err => {
            logWindow.appendLine(errorMessage(err.message));
        });
        
        disposeLog = new Disposable(() => {
            active = false;
            watcher.close();
        })
        addSubscribable(disposeLog);
    });
}

export function stopWatchingLogFolder()
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

function infoMessage(text: string)
{
    return '[info] ' + text;
}

function errorMessage(text: string)
{
    return '[error] ' + text;
}

function getWorkshopFolder(callback: (directory: string | undefined) => void)
{
    // The config has priority.
    let configDirectory = config.get<string>('workshopLogFolder');
    if (configDirectory && configDirectory.trim().length > 0) {
        callback(configDirectory);
        return;
    }

    if (process.platform == 'win32') {
        // Attempt to locate the documents folder.
        exec('[environment]::getfolderpath("mydocuments")', { 'shell': 'powershell.exe' }, (error, stdout, stderr) => {
            if (error || stderr) {
                callback(undefined);
            } else {
                // 'stdout' will contain directory and newlines.
                const path = fspath.join(stdout.trim(), 'Overwatch', 'Workshop');
                
                // Confirm directory existence.
                fs.access(path, (err) => {
                    if (!err)
                        callback(path);
                    else
                        callback(undefined);
                });
            }
        });
    } else {
        callback(undefined);
    }
}

/**
 * Returns the index of a character in a string given a byte index.
 * @param s The string to scan.
 * @param byte The byte index.
 * @returns Will return the character index that the byte is a part of.
 */
function characterIndexFromByte(s: string, byte: number)
{
    let length = 0;

    for (let i = 0; i < s.length; i++) {
        let currentChar = length + Buffer.byteLength(s[i]);
        if (currentChar > byte)
            break;
        else
            length = currentChar;
    }

    return length;
}