import { Pattern } from '../pattern';
import * as tm from '../index';
import { Repository } from './repository';

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

export function setPlayerVariablePattern(start: tm.Regexable, startTmName: tm.TmName): Pattern {
    return {
        begin: [tm.Match(start, startTmName), tm.w, tm.Match('(', 'meta.brace.round')],
        end: ')',
        zeroEndCapture: { name: 'meta.brace.round' },
        patterns: [
            // Second parameter
            {
                begin: [
                    tm.Match(',', 'punctuation.separator.comma'), tm.w,
                    tm.Match(variable, 'variable.parameter'), tm.w,
                    tm.Match(',', 'punctuation.separator.comma'),
                ],
                end: /(?=\))/,
                patterns: [{ include: Repository.expression }]
            },
            // First parameter
            { include: Repository.expression }
        ]
    };
}