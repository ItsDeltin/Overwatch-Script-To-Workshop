import { Raw, GetRegexNode, Regexable } from './index';
import { TmName } from './tmName';
import { Pattern, GlobalRegexCapture, exportPattern } from './pattern';
import { CaptureList } from './template';

/**
 * Converts a RegexNode to a string.
 * Groups are catalogged for tmNames and patterns. */
export class RegexExport {
    regex: string = '';
    groups: (GroupNode | undefined)[] = [];
    captures: Capture[] = [];

    public write(value: string) {
        this.regex += value;
    }

    public addGroup(group: GroupNode) {
        this.groups.push(group);
        this.captures.push({
            index: this.groups.length,
            tmName: group.options.tmName,
            patterns: group.options.patterns,
        });
    }

    public addCapture(capture: Capture) {
        this.captures.push(capture);
    }

    public addDummyGroup() {
        this.groups.push(undefined);
    }

    public referenceGroup(group: GroupNode): string {
        return '\\' + (this.groups.indexOf(group) + 1);
    }

    public validate() {
        new RegExp(this.regex);
    }

    public getCaptureList(zerothCapture?: GlobalRegexCapture): CaptureList {
        let captureList: CaptureList = {};

        if (zerothCapture) {
            captureList[0] = {
                name: zerothCapture.name,
                patterns: zerothCapture.patterns?.map((p) => exportPattern(p)),
            };
        }

        this.captures.forEach((capture) => {
            if (capture.tmName || capture.patterns)
                captureList[capture.index] = {
                    name: capture.tmName,
                    patterns: capture.patterns?.map((p) => exportPattern(p)),
                };
        });

        return captureList;
    }
}

enum ExportContainer {
    Node,
    WholeChain,
}

export abstract class RegexNode {
    public containExport(
        exporter: RegexExport,
        context: ExportContainer = ExportContainer.Node
    ) {
        if (this.groupQuantifier(context))
            new GroupNode({ value: this }).export(exporter);
        else this.export(exporter);
    }

    public abstract export(exporter: RegexExport): void;
    public groupQuantifier(context: ExportContainer): boolean {
        return false;
    }

    public Maybe(lazy: boolean = false) {
        return new MaybeNode(this, lazy);
    }
    public ZeroOrMore(lazy: boolean = false) {
        return new ZeroOrMoreNode(this, lazy);
    }
    public OneOrMore(lazy: boolean = false) {
        return new OneOrMoreNode(this, lazy);
    }
    public Exactly(count: number, lazy: boolean = false) {
        return new ExactlyNode(this, count, lazy);
    }
    public AtLeast(count: number, lazy: boolean = false) {
        return new AtLeastNode(this, count, lazy);
    }
    public Between(from: number, to: number, lazy: boolean = false) {
        return new BetweenNode(this, from, to, lazy);
    }
}

export class GroupNode extends RegexNode {
    options: GroupOptions;

    constructor(options: GroupOptions) {
        super();
        this.options = options;
    }

    public recursiveCall() {
        return Raw('\\g<' + this.options.groupName + '>');
    }

    public doExportCaptures(): boolean {
        return !!this.options.tmName || !!this.options.patterns;
    }

    public export(exporter: RegexExport): void {
        // Add the group to the exporter.
        exporter.addGroup(this);

        // Start the group.
        exporter.write('(');

        // The group is named.
        if (this.options.groupName) {
            exporter.write('?<' + this.options.groupName + '>');
        } else {
            switch (this.options.groupKind) {
                // The group is non-capturing.
                case GroupKind.NonCapturing:
                    exporter.write('?:');
                    break;

                // Positive lookahead.
                case GroupKind.PositiveLookahead:
                    exporter.write('?=');
                    break;

                // Positive lookbehind.
                case GroupKind.PositiveLookbehind:
                    exporter.write('?<=');
                    break;

                // Negative lookahead.
                case GroupKind.NegativeLookahead:
                    exporter.write('?!');
                    break;

                // Negative lookbehind.
                case GroupKind.NegativeLookbehind:
                    exporter.write('?<!');
                    break;
            }
        }

        // Export the value.
        GetRegexNode(<Regexable>this.options.value).export(exporter);

        // End the group.
        exporter.write(')');
    }
}

export interface GroupOptions {
    /** The TM name of the group. */
    tmName?: TmName;
    /** The name of the group, used for recursion. Mutually exclusive with groupKind. */
    groupName?: string;
    /** The type of the group. Mutually exclusive with groupName. */
    groupKind?: GroupKind;
    /** The patterns that are applied to the matched text. */
    patterns?: Pattern[] | undefined;
    /** The group's value. Must be non-null once exported. */
    value?: Regexable;
}

export interface Capture {
    index: number;
    tmName?: TmName;
    patterns?: Pattern[];
}

export enum GroupKind {
    NonCapturing,
    PositiveLookahead,
    PositiveLookbehind,
    NegativeLookahead,
    NegativeLookbehind,
}

// ...
export class ChainNode extends RegexNode {
    nodes: RegexNode[] = [];

    public constructor(nodes: RegexNode[] = []) {
        super();
        this.nodes = nodes;
    }

    public add(value: RegexNode) {
        this.nodes.push(value);
    }

    public export(exporter: RegexExport): void {
        for (const node of this.nodes) node.containExport(exporter);
    }

    public groupQuantifier(context: ExportContainer): boolean {
        return context == ExportContainer.WholeChain;
    }

    // public groupQuantifier(): boolean {
    //     return this.nodes.every((n) => n instanceof TextNode);
    // }
}

export class QuantifierNode extends RegexNode {
    value: RegexNode;
    symbol: string;
    lazy: boolean;

    constructor(value: RegexNode, symbol: string, lazy: boolean = false) {
        super();
        this.value = value;
        this.symbol = symbol;
        this.lazy = lazy;
    }

    public export(exporter: RegexExport): void {
        this.value.containExport(exporter, ExportContainer.WholeChain);
        exporter.write(this.symbol);

        if (this.lazy) exporter.write('?');
    }

    public groupQuantifier(context: ExportContainer): boolean {
        return context == ExportContainer.WholeChain;
    }
}

// ?
export class MaybeNode extends QuantifierNode {
    constructor(value: RegexNode, lazy: boolean = false) {
        super(value, '?', lazy);
    }
}

// *
export class ZeroOrMoreNode extends QuantifierNode {
    constructor(value: RegexNode, lazy: boolean = false) {
        super(value, '*', lazy);
    }
}

// +
export class OneOrMoreNode extends QuantifierNode {
    constructor(value: RegexNode, lazy: boolean = false) {
        super(value, '+', lazy);
    }
}

// {n}
export class ExactlyNode extends QuantifierNode {
    constructor(value: RegexNode, count: number, lazy: boolean = false) {
        super(value, '{' + count + '}', lazy);
    }
}

// {n,}
export class AtLeastNode extends QuantifierNode {
    constructor(value: RegexNode, count: number, lazy: boolean = false) {
        super(value, '{' + count + ',}', lazy);
    }
}

// {n,m}
export class BetweenNode extends QuantifierNode {
    constructor(
        value: RegexNode,
        from: number,
        to: number,
        lazy: boolean = false
    ) {
        super(value, '{' + from + ',' + to + '}', lazy);
    }
}

// Or node
export class OrNode extends RegexNode {
    values: RegexNode[];

    constructor(values: RegexNode[]) {
        super();
        this.values = values;
    }

    public export(exporter: RegexExport): void {
        for (let i = 0; i < this.values.length; i++) {
            if (i != 0) exporter.write('|');
            this.values[i].containExport(exporter);
        }
    }

    public groupQuantifier(context: ExportContainer): boolean {
        return true;
    }
}

// Text node
export class TextNode extends RegexNode {
    value: string;

    constructor(value: string) {
        super();
        this.value = value;
    }

    public export(exporter: RegexExport): void {
        exporter.write(this.value);
    }
}

// Character class
export class CharacterClassNode extends RegexNode {
    text: string;

    public constructor(text: string) {
        super();
        this.text = text;
    }

    public export(exporter: RegexExport): void {
        exporter.write('[' + this.text + ']');
    }
}

export class ExpressionGroup extends RegexNode {
    exp: RegExp;
    // captures: Capture[];
    numberOfGroups: number;

    constructor(exp: RegExp/*, captures: Capture[]*/) {
        super();
        this.exp = exp;
        // this.captures = captures;

        // Get the number of groups in the expression.
        this.numberOfGroups =
            (<RegExpExecArray>new RegExp(exp.toString() + '|').exec(''))
                .length - 1;
    }

    public export(exporter: RegexExport): void {
        // Write the regex
        exporter.write(this.exp.source);

        // Add captures.
        /*for (let i = 0; i < this.captures.length; i++) {
            const capture = this.captures[i];
            exporter.addCapture({
                index: capture.index + exporter.groups.length,
                tmName: capture.tmName,
                patterns: capture.patterns,
            });
        }
        */

        // Add dummy groups.
        for (let i = 0; i < this.numberOfGroups; i++) {
            exporter.addDummyGroup();
        }
    }
}
