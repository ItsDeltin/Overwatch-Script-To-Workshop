import { workspace, ConfigurationChangeEvent, WorkspaceConfiguration } from 'vscode';
import { addSubscribable } from './extensions';
import { setupBuildWatcher } from './dev';
import { stopLanguageServer, startLanguageServer, setServerOptions, resetLastWorkshopOutput } from './languageServer';

export let config: WorkspaceConfiguration;

export function setupConfig()
{
    config = workspace.getConfiguration('ostw', null);

    addSubscribable(workspace.onDidChangeConfiguration(async (e: ConfigurationChangeEvent) => {
		if (e.affectsConfiguration('ostw'))
			config = workspace.getConfiguration('ostw', null);

		// ostw.deltintegerPath changed
		if (e.affectsConfiguration('ostw.deltintegerPath'))
		{
			resetLastWorkshopOutput();
			await stopLanguageServer();
			setServerOptions(config.get('deltintegerPath'));
			startLanguageServer();
        }

		// ostw.dev.deltintegerWatchBuild changed
        if (e.affectsConfiguration('ostw.dev.deltintegerWatchBuild'))
			setupBuildWatcher();
	}));
}