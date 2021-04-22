import { EventEmitter, TextDocumentContentProvider, Uri } from 'vscode';
import { decompilerOriginalWorkshopCode } from './decompile';
import { lastWorkshopOutput } from './languageServer';

export const workshopPanelProvider = new class implements TextDocumentContentProvider {
	// emitter and its event
	onDidChangeEmitter = new EventEmitter<Uri>();
	onDidChange = this.onDidChangeEmitter.event;

	provideTextDocumentContent(uri: Uri): string {
		if (uri.fsPath.startsWith('[decompile]'))
		{
			return decompilerOriginalWorkshopCode;
		}
		else
		{
			if (lastWorkshopOutput == null) return "";
			return lastWorkshopOutput;
		}
	}
};