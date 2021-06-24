import { Regexable, GetRegexNode } from '.';
import { Pattern, exportPattern } from './Pattern';
import { TmLanguage } from './Template';
import { RegexExport } from './RegexNode';

export class TextmateSettings {
    name?: string;
    scopeName?: string;
    fileTypes?: string[];
    foldingStartMarker?: Regexable;
    foldingStopMarker?: Regexable;
    patterns?: Pattern[];
    firstLineMatch?: Regexable;
    repository?: { [name: string]: Pattern };
}

export function createTextmateGrammar(settings: TextmateSettings): TmLanguage {
    let root: TmLanguage = {
        name: settings.name,
        scopeName: settings.scopeName,
        fileTypes: settings.fileTypes,
        foldingStartMarker: quickExport(settings.foldingStartMarker),
        foldingStopMarker: quickExport(settings.foldingStopMarker),
        patterns: settings.patterns?.map((p) => exportPattern(p)),
        firstLineMatch: quickExport(settings.firstLineMatch),
    };

    // Convert repository.
    root.repository = {};
    for (const key in settings.repository) {
        root.repository[key] = exportPattern(settings.repository[key]);
    }

    // Done
    return root;
}

function quickExport(regexable?: Regexable): string | undefined {
    if (regexable) {
        let exporter: RegexExport = new RegexExport();
        GetRegexNode(regexable).export(exporter);
        return exporter.regex;
    }
    return undefined;
}
