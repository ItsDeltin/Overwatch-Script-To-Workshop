import {
    RegexNode,
    TextNode,
    GroupOptions,
    GroupNode,
    GroupKind,
    ChainNode,
    OrNode,
    ZeroOrMoreNode,
    MaybeNode,
    OneOrMoreNode,
    AtLeastNode,
    BetweenNode,
    ExactlyNode,
    CharacterClassNode,
    ExpressionGroup
} from './regexNode';
import { TmName } from './tmName';
import { Pattern } from './pattern';

export { Pattern, GlobalRegexCapture } from './pattern';
export { createTextmateGrammar, TextmateSettings } from './textmateGenerator';
export { TmLanguage, TmRule, CaptureList } from './template';
export { TmName } from './tmName';

export type Regexable = string | RegExp | RegexNode | RegexNode[] | Regexable[];

export function GetRegexNode(regexable: Regexable): RegexNode {
    let elements: RegexNode[] = [];

    function recursive(current: Regexable) {
        // Array
        if (Array.isArray(current))
            for (const iterator of current) recursive(iterator);
        // Regex
        else if (current instanceof RegExp) elements.push(new ExpressionGroup(current));
        // Literal string
        else if (typeof current === 'string') elements.push(Literal(current));
        // Regex node
        else if (current instanceof RegexNode) elements.push(current);
    }
    recursive(regexable);

    if (elements.length == 0) throw new Error('No elements in regex node.');
    if (elements.length == 1) return elements[0];
    return new ChainNode(elements);
}

export const b = WordBoundary();
export const w = Whitespace().ZeroOrMore();

// Logic
export function Group(options: GroupOptions) {
    return new GroupNode(options);
}
export function Match(value: Regexable, tmName: TmName) {
    return new GroupNode({ value, tmName });
}
export function NonCapturing(value: Regexable) {
    return new GroupNode({
        groupKind: GroupKind.NonCapturing,
        value: value,
    });
}

export function Or(...values: Regexable[]) {
    return new OrNode(values.map((v) => GetRegexNode(v)));
}
export function And(...values: Regexable[]) {
    return new ChainNode(values.map((v) => GetRegexNode(v)));
}

// Quantifiers
export function Maybe(value: Regexable, lazy: boolean = false) {
    return new MaybeNode(GetRegexNode(value), lazy);
}
export function ZeroOrMore(value: Regexable, lazy: boolean = false) {
    return new ZeroOrMoreNode(GetRegexNode(value), lazy);
}
export function OneOrMore(value: Regexable, lazy: boolean = false) {
    return new OneOrMoreNode(GetRegexNode(value), lazy);
}
export function Exactly(
    value: Regexable,
    count: number,
    lazy: boolean = false
) {
    return new ExactlyNode(GetRegexNode(value), count, lazy);
}
export function AtLeast(
    value: Regexable,
    count: number,
    lazy: boolean = false
) {
    return new AtLeastNode(GetRegexNode(value), count, lazy);
}
export function Between(
    value: Regexable,
    from: number,
    to: number,
    lazy: boolean = false
) {
    return new BetweenNode(GetRegexNode(value), from, to, lazy);
}

// Symbols
export function Digit() {
    return Raw('\\d');
}
export function NotDigit() {
    return Raw('\\D');
}
export function WordCharacter() {
    return Raw('\\w');
}
export function NotWordCharacter() {
    return Raw('\\W');
}
export function Whitespace() {
    return Raw('\\s');
}
export function NotWhitespace() {
    return Raw('\\S');
}
export function Any() {
    return Raw('.');
}

// Anchors
export function StartOfLine() {
    return Raw('^');
}
export function EndOfLine() {
    return Raw('$');
}
export function WordBoundary() {
    return Raw('\\b');
}
export function NotWordBoundary() {
    return Raw('\\B');
}

// Character classes
export function CharacterClass(text: string) {
    return new CharacterClassNode(text);
}

// Lookarounds
export function PositiveLookahead(value: Regexable) {
    return new GroupNode({
        groupKind: GroupKind.PositiveLookahead,
        value: value,
    });
}
export function PositiveLookbehind(value: Regexable) {
    return new GroupNode({
        groupKind: GroupKind.PositiveLookbehind,
        value: value,
    });
}
export function NegativeLookahead(value: Regexable) {
    return new GroupNode({
        groupKind: GroupKind.NegativeLookahead,
        value: value,
    });
}
export function NegativeLookbehind(value: Regexable) {
    return new GroupNode({
        groupKind: GroupKind.NegativeLookbehind,
        value: value,
    });
}

// Other
export function Raw(value: string) {
    return new TextNode(value);
}
export function Literal(value: string) {
    const reservedCharacters = [
        '.', // Any character
        '(',
        ')', // Grouping
        '[',
        ']', // Character set
        '{',
        '}', // Repetition
        '*', // Zero or more
        '+', // One or more
        '?', // Zero or one
        '^', // Start of string
        '$', // End of string
        '/', // Seperator
        '-', // Range
        '\\', // Escape
        '|', // Or
    ];

    let result: string = '';

    for (let i = 0; i < value.length; i++) {
        const element = value[i];

        if (reservedCharacters.indexOf(element) != -1) result += '\\';
        result += element;
    }

    return Raw(result);
}

// Repository maker
export function makeRepository(addElements: (add: (name: string, pattern: Pattern) => void) => void)
{
    let result: { [name: string]: Pattern } = {};
    addElements((name, pattern) => {
        result[name.replace('#', '')] = pattern;
    });
    return result;
}
