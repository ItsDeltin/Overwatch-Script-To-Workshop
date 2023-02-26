import * as tm from '../index';
import { i, w } from './common-nodes';
import { Pattern } from '../index';

export function pair(regexable: tm.Regexable, name: tm.TmName) {
    return tm.Group({ value: regexable, tmName: name });
}

export function createTypeDescriptor(tagName: 'struct' | 'class' | 'enum') {
    return [
        // ['class' A]
        tm.Group({ value: tagName, tmName: 'keyword.other.' + tagName }),
        // [class' 'A]
        w,
        // [class 'A']
        tm.Group({ value: i, tmName: 'entity.name.type.' + tagName }),
    ];
}

export function languageHighlight(activationName: string, referenceName: string): Pattern {
    return {
        begin: '```' + activationName,
        end: /```|^\s*(?!#)/,
        zeroBeginCapture: { name: 'markup.fenced_code.block.markdown' },
        zeroEndCapture: { name: 'markup.fenced_code.block.markdown' },
        patterns: [
            {
                begin: /#/,
                end: /$|(?=```)/,
                zeroBeginCapture: { name: 'comment.block.documentation' },
                patterns: [{include: referenceName}]
            }
        ]
    }
}
