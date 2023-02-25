import { Pattern } from '../pattern';
import * as tm from '../index';

export const variable = /[a-zA-Z0-9_]+/;

export function makeDictionaryLike(name: string, patterns: Pattern[]): Pattern {
    return {
        begin: [tm.WordBoundary(), name, tm.WordBoundary()],
        zeroBeginCapture: { name: 'keyword.control' },
        end: '}',
        zeroEndCapture: { name: 'punctuation.definition.dictionary.end' },
        patterns: [
            {
                begin: '{',
                zeroBeginCapture: { name: 'punctuation.definition.dictionary.begin' },
                end: /(?=})/,
                patterns
            }
        ]
    }
}

export function numberedList(valueName: string): Pattern {
    return {
        match: [
            tm.Group({ value: /[0-9]+/, tmName: 'constant.numeric' }), tm.w,
            tm.Group({ value: /:/, tmName: 'punctuation.separator.dictionary.key-value' }), tm.w,
            tm.Group({ value: variable, tmName: valueName })
        ]
    };
}