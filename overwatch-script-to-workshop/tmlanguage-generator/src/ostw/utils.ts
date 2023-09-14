import * as tm from '../index';
import { i, w } from './common-nodes';
import { Pattern } from '../index';

export function pair(regexable: tm.Regexable, name: tm.TmName) {
    return tm.Group({ value: regexable, tmName: name });
}

export function createTypeDescriptor(tagName: 'struct' | 'class' | 'enum') {
    return [
        tm.Maybe([pair('single', 'storage.modifier'), w]),
        // ['class' A]
        tm.Group({ value: tagName, tmName: 'keyword.other.' + tagName }),
        // [class' 'A]
        w,
        // [class 'A']
        tm.Group({ value: i, tmName: 'entity.name.type.' + tagName }),
    ];
}

export function languageHighlight(activationName: string, referenceName: string, inline: boolean): Pattern {
    return {
        begin: '```' + activationName,
        end: /```|^(?!\s*#)/,
        zeroBeginCapture: { name: 'comment.block.documentation' },
        zeroEndCapture: { name: 'comment.block.documentation' },
        patterns: [
            inline ?
            {
                match: [
                    tm.Match(/#/, 'comment.block.documentation'),
                    tm.Group({ value: /.*?($|(?=```))/, patterns: [{include: referenceName}] })
                ]
            } :
            // Referencing external languages doesn't seem to work in captured patterns?
            {
                begin: /#/,
                end: /$|(?=```)/,
                zeroBeginCapture: { name: 'comment.block.documentation' },
                patterns: [{include: referenceName}]
            }
        ]
    }
}
