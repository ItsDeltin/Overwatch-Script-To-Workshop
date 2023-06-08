import * as tm from '../index';
import { Names } from './names';
import { Repository } from './repository';
import { pair } from './utils';

// Boundary
export const b = tm.WordBoundary();

// Whitespace
export const w = tm.Whitespace().ZeroOrMore();
export const bw = [b, w];

// Symbols
export const bracket_open = pair('{', Names.curly_open);
export const bracket_close = pair('}', Names.curly_close);
export const parenthesis_open = pair('(', Names.parenthesis_open);
export const parenthesis_close = pair(')', Names.parenthesis_close);
export const comma = pair(',', Names.comma);
export const encapsulated_block = tm.PositiveLookahead('}');
export const accessor = pair('.', Names.dot);
export const colon = pair(':', Names.colon);
export const parameters_begin = pair('(', Names.parameters_begin);
export const parameters_end = pair(')', Names.parameters_end);

// Inline string
export const string = tm.Or(
    // Double quotes
    pair(/\"(?:[^"\\]|\\.)*\"/, 'string.quoted.double'),
    // Single quotes
    pair(/\'(?:[^'\\]|\\.)*\'/, 'string.quoted.single')
);

// Unescaped
export const unescaped = /(?<!\\)(?:\\{2})*/;

// Identifier
export const i = [tm.CharacterClass('a-zA-Z0-9_').OneOrMore(), b];
export const i_variable_field = pair(i, 'entity.name.variable.field');
export const i_function = pair(i, 'entity.name.function');
export const i_type = pair(i, 'entity.name.type');
export const i_variable = pair(i, 'variable');
export const i_parameter = pair(i, 'variable.parameter');

// * Code type
export const codeType = tm.Group({
    groupName: 'type',
    tmName: 'meta.type',
    patterns: [{ include: Repository.code_type_matcher }],
});
// Regex
codeType.options.value = [
    // Constant type marker
    tm.Maybe(['const', w]),
    tm.Or(
        // Match parenthesized type recursively.
        [
            '(',
            w,
            tm.ZeroOrMore([codeType.recursiveCall(), w, tm.Maybe([',', w])]),
            ')',
        ],
        // Match type identifier.
        [
            i,
            w,
            // Type args
            tm.Maybe(['<', w, tm.And(tm.Maybe(','), w, codeType.recursiveCall(), w).OneOrMore(), '>']),
        ]
    ),
    w,
    // Arrays
    tm.ZeroOrMore(['[', w, ']', w]),
    // Union types
    tm.ZeroOrMore(['|', w, codeType.recursiveCall(), w]),
    // Function types
    tm.Maybe(['=>', w, codeType.recursiveCall(), w]),
];

export function typeArgs(recursive: boolean = false) {
    return tm.And(
        pair('<', 'punctuation.definition.typeparameters.begin'),
        w,
        tm.ZeroOrMore([tm.Maybe(comma), w, codeType.get(recursive), w]),
        pair('>', 'punctuation.definition.typeparameters.end')
    );
}

// Match preceeding dot if it exists.
export const accessorMatch = tm.Maybe([accessor, w]);
