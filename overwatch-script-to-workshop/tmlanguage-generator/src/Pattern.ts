import { TmName } from './TmName';
import { Regexable, GetRegexNode } from './index';
import { TmRule } from './Template';
import { RegexExport } from './RegexNode';

export interface Pattern {
    /**
     * The name which gets assigned to the portion matched. This is used for styling and scope-specific settings and actions,
     * which means it should generally be derived from one of the standard names.
     */
    name?: TmName;

    /**
     * A regular expression which is used to identify the portion of text to which the name should be assigned.
     */
    match?: Regexable;

    /**
     * 'begin' and 'end' allow matches which span several lines and must both be mutually exclusive with the match key.
     * Each is a regular expression pattern. begin is the pattern that starts the block and end is the pattern which ends the block.
     * Captures from the begin pattern can be referenced in the end pattern by using normal regular expression back-references.
     * This is often used with here-docs. A begin/end rule can have nested patterns using the patterns key.
     *
     * With begin/end, if the end pattern is not found, the overall match does not fail: rather, once the begin pattern is matched,
     * the overall match runs to the end pattern or to the end of the document, whichever comes first. The underlying architectural
     * reason is that the TextMate parser does not backtrack; once the begin pattern is matched, it is matched successfully and that’s
     * that — TextMate can’t change its mind and decide that it shouldn’t have matched the begin pattern after all.
     *
     * Depends on the 'end' or 'while' property.
     */
    begin?: Regexable;

    /**
     * these keys allow matches which span several lines and must both be mutually exclusive with the match key.
     * Each is a regular expression pattern. begin is the pattern that starts the block and end is the pattern which ends the block.
     * Captures from the begin pattern can be referenced in the end pattern by using normal regular expression back-references.
     * This is often used with here-docs. A begin/end rule can have nested patterns using the patterns key.
     *
     * Depends on the 'begin' property.
     */
    end?: Regexable;

    /**
     * Allows matches which span several lines and must both be mutually exclusive with the match key.
     * Each is a regular expression pattern. begin is the pattern that starts the block and while continues it.
     *
     * Depends on the 'begin' property.
     */
    while?: Regexable;

    /**
     * Allows you to assign attributes to the ***0th capture*** of the **match** pattern.
     *
     * TextMate grammar patterns have an additional property called 'captures' which is absent from the 'Pattern' class. These values are generated when
     * exporting and can be set in the group nodes themselves. Since the 0th capture does not have a group, this field is used to assign attributes to it.
     *
     * Using the **zeroCapture** key for a begin/end rule is short-hand for giving both zeroBeginCapture and zeroEndCapture the same values.
     */
    zeroCapture?: GlobalRegexCapture;

    /**
     * Allows you to assign attributes to the ***0th capture*** of the **begin** pattern.
     *
     * TextMate grammar patterns have an additional property called 'beginCaptures' which is absent from the 'Pattern' class. These values are generated when
     * exporting and can be set in the group nodes themselves. Since the 0th capture does not have a group, this field is used to assign attributes to it.
     *
     * Using the **zeroCapture** key for a begin/end rule is short-hand for giving both zeroBeginCapture and zeroEndCapture the same values.
     */
    zeroBeginCapture?: GlobalRegexCapture;

    /**
     * Allows you to assign attributes to the ***0th capture*** of the **end** pattern.
     *
     * TextMate grammar patterns have an additional property called 'endCaptures' which is absent from the 'Pattern' class. These values are generated when
     * exporting and can be set in the group nodes themselves. Since the 0th capture does not have a group, this field is used to assign attributes to it.
     *
     * Using the **zeroCapture** key for a begin/end rule is short-hand for giving both zeroBeginCapture and zeroEndCapture the same values.
     */
    zeroEndCapture?: GlobalRegexCapture;

    /**
     * Allows you to assign attributes to the ***0th capture*** of the **while** pattern.
     *
     * TextMate grammar patterns have an additional property called 'whileCaptures' which is absent from the 'Pattern' class. These values are generated when
     * exporting and can be set in the group nodes themselves. Since the 0th capture does not have a group, this field is used to assign attributes to it.
     *
     * Using the **zeroCapture** key for a begin/end rule is short-hand for giving both zeroBeginCapture and zeroEndCapture the same values.
     */
    zeroWhileCapture?: GlobalRegexCapture;

    /**
     * This key is similar to the name key but only assigns the name to the text between what is matched by the begin/end patterns.
     *
     * @example // Get the text between #if 0 and #endif marked up as a comment:
     * let p = new Pattern();
     *
     * p.begin = ['#if 0', Group([Whitespace(), Any().ZeroOrMore()]).Maybe(), EndOfLine()];
     * p.end = '#endif';
     * p.contentName = 'comment.block.preprocessor';
     */
    contentName?: TmName;

    /**
     * This allows you to reference a different language, recursively reference the grammar itself or a rule declared in this file’s repository.
     * To reference another language, use the scope name of that language.
     * To reference the grammar itself, use $self.
     * To reference a rule from the current grammars repository, prefix the name with a pound sign (#).
     */
    include?: string;

    /**
     * Applies to the region between the begin and end matches.
     */
    patterns?: Pattern[];

    applyEndPatternsLast?: 0 | 1;
}

export function exportPattern(pattern: Pattern) {
    let rule: TmRule = {
        name: pattern.name,
        contentName: pattern.contentName,
        applyEndPatternsLast: pattern.applyEndPatternsLast,
        include: pattern.include,
    };

    // Export 'match'.
    if (pattern.match) {
        let matchExporter = new RegexExport();
        GetRegexNode(pattern.match).export(matchExporter);
        matchExporter.validate();
        rule.match = matchExporter.regex;
        rule.captures = matchExporter.getCaptureList(pattern.zeroBeginCapture);
    }

    // Export 'begin'.
    if (pattern.begin) {
        let beginExporter = new RegexExport();
        GetRegexNode(pattern.begin).export(beginExporter);
        beginExporter.validate();
        rule.begin = beginExporter.regex;
        rule.beginCaptures = beginExporter.getCaptureList(
            pattern.zeroBeginCapture
        );
    }

    // Export 'end'
    if (pattern.end) {
        let endExporter = new RegexExport();
        GetRegexNode(pattern.end).export(endExporter);
        endExporter.validate();
        rule.end = endExporter.regex;
        rule.endCaptures = endExporter.getCaptureList(pattern.zeroEndCapture);
    }

    // Export 'while'
    if (pattern.while) {
        let whileExporter = new RegexExport();
        GetRegexNode(pattern.while).export(whileExporter);
        whileExporter.validate();
        rule.while = whileExporter.regex;
        rule.whileCaptures = whileExporter.getCaptureList(
            pattern.zeroWhileCapture
        );
    }

    // Export patterns
    if (pattern.patterns) {
        rule.patterns = pattern.patterns.map((p) => exportPattern(p));
    }

    return rule;
}

export interface GlobalRegexCapture {
    name?: TmName;
    patterns?: Pattern[];
}
