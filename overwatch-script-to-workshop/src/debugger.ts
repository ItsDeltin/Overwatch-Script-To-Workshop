import * as vscode from 'vscode';
import { ProviderResult, ExtensionContext } from 'vscode';
import * as debugAdapter from 'vscode-debugadapter';
import { LoggingDebugSession, Scope, Handles, InitializedEvent, StoppedEvent, Thread, StackFrame } from 'vscode-debugadapter';
import { DebugProtocol } from 'vscode-debugprotocol';
import { client } from './languageServer';

export function register(context: ExtensionContext)
{
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('ostw', adapterFactory));
}

const adapterFactory = new class implements vscode.DebugAdapterDescriptorFactory {
	createDebugAdapterDescriptor(_session: vscode.DebugSession): ProviderResult<vscode.DebugAdapterDescriptor> {
        return new vscode.DebugAdapterInlineImplementation(new DeltinDebugger());
	}
};

class DeltinDebugger extends LoggingDebugSession
{
    private _cancelationTokens = new Map<number, boolean>();

    constructor()
    {
        super();

        // Debugger settings
        this.setDebuggerColumnsStartAt1(false);
		this.setDebuggerLinesStartAt1(false);
		
		client.onNotification('debugger.activated', () => {
			this.sendEvent(new StoppedEvent('activated actions', 1));
		});

		client.onNotification('debugger.error', msg => {
			vscode.window.showErrorMessage('OSTW debugger exception: ' + msg);
		});

        // _runtime = new DebuggerRuntime();
    }

    protected initializeRequest(response: DebugProtocol.InitializeResponse, args: DebugProtocol.InitializeRequestArguments)
    {
        response.body = response.body || {};

        // response.body.supportsCancelRequest = true;
		response.body.supportsEvaluateForHovers = true;
		response.body.supportsRestartRequest = true;
		response.body.supportsValueFormattingOptions = true;
		response.body.supportsClipboardContext = true;

        this.sendResponse(response);
		this.sendEvent(new InitializedEvent());
	}

    protected async launchRequest(response: DebugProtocol.LaunchResponse, args: DebugProtocol.LaunchRequestArguments) {
		await client.sendRequest('debugger.start');
        this.sendResponse(response);
	}
	
	protected threadsRequest(response: DebugProtocol.ThreadsResponse): void {
		// runtime supports no threads so just return a default thread.
		response.body = {
			threads: [
				new Thread(1, "main")
			]
		};
		this.sendResponse(response);
	}

	protected stackTraceRequest(response: DebugProtocol.StackTraceResponse, args: DebugProtocol.StackTraceArguments): void {

		const startFrame = typeof args.startFrame === 'number' ? args.startFrame : 0;
		const maxLevels = typeof args.levels === 'number' ? args.levels : 1000;

		response.body = {
			stackFrames: [{
				id: 1,
				name: 'todo: Stack Frame Name',
				column: 0,
				line: 0,
				source: {name:'saucy', path:this.convertDebuggerPathToClient(vscode.window.activeTextEditor.document.uri.fsPath), adapterData:'this is a test'}
			}],
			totalFrames: 1
		};
		this.sendResponse(response);
	}

    protected async scopesRequest(response: DebugProtocol.ScopesResponse, args: DebugProtocol.ScopesArguments) {
		let scopes: DebugProtocol.Scope[] = await client.sendRequest("debugger.scopes", args);
		
		response.body = {
			scopes: scopes
		};
		this.sendResponse(response);
    }
    
    protected async variablesRequest(response: DebugProtocol.VariablesResponse, args: DebugProtocol.VariablesArguments, request?: DebugProtocol.Request) {

		let variables: debugAdapter.Variable[] = await client.sendRequest("debugger.variables", args);
		
		response.body = {
			variables: variables
		};
		this.sendResponse(response);
	}

	protected async evaluateRequest(response: DebugProtocol.EvaluateResponse, args: DebugProtocol.EvaluateArguments) {
		response.body = await client.sendRequest('debugger.evaluate', args);
		this.sendResponse(response);
	}

	protected pauseRequest(response: DebugProtocol.PauseResponse, args: DebugProtocol.PauseArguments, request?: DebugProtocol.Request) {
		this.sendEvent(new StoppedEvent('pause', 1));
		response.body = {};
		this.sendResponse(response);
	}

	protected restartRequest(response: DebugProtocol.RestartResponse, args: DebugProtocol.RestartArguments, request?: DebugProtocol.Request) {
		response.body = {};
		this.sendResponse(response);
	}

	protected async disconnectRequest(response: DebugProtocol.DisconnectResponse, args: DebugProtocol.DisconnectArguments, request?: DebugProtocol.Request) {
		await client.sendRequest('debugger.stop');
		this.sendResponse(response);
	}
}