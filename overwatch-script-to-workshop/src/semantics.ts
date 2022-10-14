import { languages, Disposable, DocumentSemanticTokensProvider, SemanticTokensBuilder, SemanticTokensLegend, TextDocument } from 'vscode';
import { Range as LSRange } from 'vscode-languageclient';
import { selector } from './extensions';
import { config } from './config';
import { client, serverStatus, onServerReady } from './languageServer';

let semantics: Disposable | null;

export function setupSemantics() {
	let enabled = config.get('semanticHighlighting');

	if (!enabled && semantics != null) {
		semantics.dispose();
		semantics = null;
	}

	if (enabled)
		semantics = languages.registerDocumentSemanticTokensProvider(selector, provider, legend);
}

const tokenTypes = ['comment', 'string', 'keyword', 'number', 'regexp', 'operator', 'namespace',
	'type', 'struct', 'class', 'interface', 'enum', 'enummember', 'typeParameter', 'function',
	'member', 'macro', 'variable', 'parameter', 'property', 'label'];
const tokenModifiers = ['declaration', 'readonly', 'static', 'deprecated', 'abstract', 'async', 'modification', 'documentation', 'defaultLibrary'];
const legend = new SemanticTokensLegend(tokenTypes, tokenModifiers);

const provider: DocumentSemanticTokensProvider = {
	async provideDocumentSemanticTokens(document: TextDocument) {
		// Wait for the server to be ready.
		if (!await waitForServer()) return null;

		// Get the semantic tokens in the provided document from the language server.
		let tokens: { range: LSRange, tokenType: string, modifiers: string[] }[] = await client.sendRequest('semanticTokens', document.uri);

		// Create the builder.
		let builder: SemanticTokensBuilder = new SemanticTokensBuilder(legend);

		// Push tokens to the builder.
		for (const token of tokens) {
			builder.push(client.protocol2CodeConverter.asRange(token.range), token.tokenType, token.modifiers);
		}

		// Return the result.
		return builder.build();
	}
};

async function waitForServer(): Promise<boolean> {
	if (serverStatus == 'ready') return true;
	return new Promise(resolve => {
		onServerReady.event(() => {
			resolve(true);
		}, this);
		setTimeout(() => {
			resolve(false);
		}, 10000);
	}
	);
}