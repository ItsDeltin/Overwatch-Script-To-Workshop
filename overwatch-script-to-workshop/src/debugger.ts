import * as vscode from 'vscode';
import { ProviderResult, ExtensionContext } from 'vscode';
import * as debugAdapter from 'vscode-debugadapter';
import { LoggingDebugSession } from 'vscode-debugadapter';
import { getServerModule } from './extensions';

export function register(context: ExtensionContext)
{
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('ostw', adapterFactory));
}

const adapterFactory = new class implements vscode.DebugAdapterDescriptorFactory {
	createDebugAdapterDescriptor(_session: vscode.DebugSession): ProviderResult<vscode.DebugAdapterDescriptor> {
        return new vscode.DebugAdapterExecutable(
            getServerModule(),
            ['--debugger'],
            {}
        );
	}
}