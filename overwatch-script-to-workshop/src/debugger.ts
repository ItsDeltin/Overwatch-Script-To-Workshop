import * as vscode from 'vscode';
import { ProviderResult, ExtensionContext } from 'vscode';
import * as debugAdapter from 'vscode-debugadapter';
import { LoggingDebugSession, Scope, Handles, InitializedEvent, StoppedEvent, Thread, StackFrame } from 'vscode-debugadapter';
import { DebugProtocol } from 'vscode-debugprotocol';
import { EventEmitter } from 'events';
import { getServerModule, client } from './extensions';
import { setTimeout } from 'timers';
import { debugPort } from 'process';
import { debug } from 'console';

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
    // private _runtime: DebuggerRuntime;
    private _variableHandles = new Handles<string>();
    private _cancelationTokens = new Map<number, boolean>();
	private _isLongrunning = new Map<number, boolean>();

    constructor()
    {
        super();

        // Debugger settings
        this.setDebuggerColumnsStartAt1(false);
		this.setDebuggerLinesStartAt1(false);
		
		client.onNotification('debugger.activated', () => {
			this.sendEvent(new StoppedEvent('activated actions', 1));
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

        this.sendResponse(response);
		this.sendEvent(new InitializedEvent());
	}

    protected async launchRequest(response: DebugProtocol.LaunchResponse, args: DebugProtocol.LaunchRequestArguments) {
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

    protected scopesRequest(response: DebugProtocol.ScopesResponse, args: DebugProtocol.ScopesArguments): void {
		response.body = {
			scopes: [
				new Scope("Variables", this._variableHandles.create("variables"), false)
				// new Scope("Global", this._variableHandles.create("global"), false)
            ]
		};
		this.sendResponse(response);
    }
    
    protected async variablesRequest(response: DebugProtocol.VariablesResponse, args: DebugProtocol.VariablesArguments, request?: DebugProtocol.Request) {

		let variables: debugAdapter.Variable[] = await client.sendRequest("debugger.getVariables", args);
		
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
}

class ActionStreamMeta
{
	public Text: string;
	public IsGlobal: boolean | undefined;
	public Keywords: ActionStreamKeywords;
	public Variables: { id: number, name: string }[];
}

class ActionStream
{
	public Meta: ActionStreamMeta;
	public Position: number;

	constructor(meta: ActionStreamMeta)
	{
		this.Meta = meta;
	}

	public getNew(): ActionStream
	{
		let newStream = new ActionStream(this.Meta);
		newStream.Position = this.Position;
		return newStream;
	}

	private visitString(str: string, skipWhitespace: boolean = true): boolean
	{
		for (let i = 0; i < str.length; i++)
			if (!this.is(i, str[i]))
				return false;

		this.accept(str.length);
		this.skipWhitespace();
		return true;
	}

	private skipWhitespace()
	{
		while (this.isWhitespace()) this.accept(1);
	}

	private current(): string {
		return this.Meta.Text[this.Position];
	}
	private is(pos: number, char: string): boolean {
		return this.Position + pos < this.Meta.Text.length && this.Meta.Text[pos + this.Position] == char;
	}
	private isWhitespace(): boolean {
		return this.Position < this.Meta.Text.length && [' ', '\t', '\n', '\r'].includes(this.Meta.Text[this.Position]);
	}
	private isNumeric(): boolean {
		return this.Position < this.Meta.Text.length && '0123456789'.includes(this.Meta.Text[this.Position]);
	}
	private isAlpha(): boolean {
		return this.Position < this.Meta.Text.length && 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'.includes(this.Meta.Text[this.Position]);
	}
	private accept(pos: number) {
		this.Position += pos;
	}

	// Generic
	private visitNumber(): number {
		let numStr = '';
		while (this.isNumeric())
		{
			numStr = numStr.concat(this.current());
			this.accept(1);
		}

		if (numStr == '') return undefined;
		return +numStr;
	}
	private visitIdentifier(): string {
		if (!this.isAlpha()) return undefined;

		let identifier = '';
		while (this.isAlpha() || this.isNumeric())
		{
			identifier = identifier.concat(this.current());
			this.accept(1);
		}

		return identifier;
	}

	// Variable list
	private visitVariables(): boolean {
		if (!this.visitString(this.Meta.Keywords.variables)) return false;

		this.visitString('{');

		// Global variable list
		if (this.visitString(this.Meta.Keywords.global)) {
			this.Meta.IsGlobal = true;
			this.visitVariableList();
		}
		/// Player variable list
		if (this.visitString(this.Meta.Keywords.player)) {
			this.Meta.IsGlobal = false;
			this.visitVariableList();
		}
		
		this.visitString('}');
		return true;
	}

	private visitVariableList() {
		let num: number;
		while ((num = this.visitNumber()) != undefined)
		{
			this.visitString(':');
			this.Meta.Variables.push({id: num, name: this.visitIdentifier()});
		}
	}

	// Action list
	private visitActions() {
		this.visitString(this.Meta.Keywords.actions);
		this.visitString('{');

		while (this.visitString(this.Meta.Keywords.global_identifier) || this.visitString(this.Meta.Keywords.player_identifier))
		{
			this.visitString('.');
			let varName = this.visitIdentifier();
		}

		this.visitString('}');
	}
}

const keywordEnglish: ActionStreamKeywords = {
	variables: "variables",
	global: "global",
	player: "player",
	actions: "actions",
	global_identifier: "Global",
	player_identifier: "Event Player"
};

interface ActionStreamKeywords
{
	variables: string;
	global: string;
	global_identifier;
	player: string;
	actions: string;
	player_identifier: string;
}