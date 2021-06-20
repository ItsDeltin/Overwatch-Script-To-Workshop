import * as tm from '../index';
import { i, w } from './common-nodes';

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
