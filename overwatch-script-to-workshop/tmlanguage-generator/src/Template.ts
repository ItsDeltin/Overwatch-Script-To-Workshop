export interface TmLanguage {
    name?: string;

    /** This should be a unique name for the grammar, following the convention of being a dot-separated name where each new (left-most) part specializes the name.
     * Normally it would be a two-part name where the first is either text or source and the second is the name of the language or document type.
     * But if you are specializing an existing type, you probably want to derive the name from the type you are specializing. For example Markdown is text.html.markdown
     * and Ruby on Rails (rhtml files) is text.html.rails. The advantage of deriving it from (in this case) text.html is that everything which works in the text.html
     * scope will also work in the text.html.«something» scope (but with a lower precedence than something specifically targeting text.html.«something»). */
    scopeName?: string;

    /** This is an array of file type extensions that the grammar should (by default) be used with. This is referenced when TextMate does not know what grammar to use for
     * a file the user opens. If however the user selects a grammar from the language pop-up in the status bar, TextMate will remember that choice. */
    fileTypes?: string[];

    /** A regular expressions that lines (in the document) are matched against. If a line matches the pattern (but not the foldingStopMarker), it becomes a folding marker. */
    foldingStartMarker?: string;

    /** A regular expressions that lines (in the document) are matched against. If a line matches the pattern (but not the foldingStartMarker), it becomes a folding marker. */
    foldingStopMarker?: string;

    /** This is an array with the actual rules used to parse the document. */
    patterns?: TmRule[];

    /** A regular expression which is matched against the first line of the document (when it is first loaded). If it matches, the grammar is used for the document
     * (unless there is a user override). Example: ^#!/.*\bruby\b. */
    firstLineMatch?: string;

    /** Rules which can be included from other places in the grammar. The key is the name of the rule and the value is the actual rule. Further explanation (and example)
     * follow with the description of the include rule key. */
    repository?: { [name: string]: TmRule };
}

export interface TmRule {
    /** The name which gets assigned to the portion matched. This is used for styling and scope-specific settings and actions, which means it should generally
     * be derived from one of the standard names. */
    name?: string;

    /** A regular expression which is used to identify the portion of text to which the name should be assigned. Example: '\b(true|false)\b'. */
    match?: string;

    /** These keys allow matches which span several lines and must both be mutually exclusive with the match key.
     * Each is a regular expression pattern. begin is the pattern that starts the block and end is the pattern which ends the block.
     * Captures from the begin pattern can be referenced in the end pattern by using normal regular expression back-references.
     * This is often used with here-docs. A begin/end rule can have nested patterns using the patterns key.
     *
     * Depends on the 'end' or 'while' property. */
    begin?: string;

    /** These keys allow matches which span several lines and must both be mutually exclusive with the match key.
     * Each is a regular expression pattern. begin is the pattern that starts the block and end is the pattern which ends the block.
     * Captures from the begin pattern can be referenced in the end pattern by using normal regular expression back-references.
     * This is often used with here-docs. A begin/end rule can have nested patterns using the patterns key.
     *
     * Depends on the 'begin' property. */
    end?: string;

    while?: string;

    /** This key is similar to the name key but only assigns the name to the text between what is matched by the begin/end patterns. */
    contentName?: string;

    /** These keys allow you to assign attributes to the captures of the match, begin, or end patterns. Using the captures key for
     * a begin/end rule is short-hand for giving both beginCaptures and endCaptures with same values.
     * The value of these keys is a dictionary with the key being the capture number and the value being a dictionary of attributes
     * to assign to the captured text.  */
    captures?: CaptureList;

    /** These keys allow you to assign attributes to the captures of the match, begin, or end patterns. Using the captures key for
     * a begin/end rule is short-hand for giving both beginCaptures and endCaptures with same values.
     * The value of these keys is a dictionary with the key being the capture number and the value being a dictionary of attributes
     * to assign to the captured text.  */
    beginCaptures?: CaptureList;

    /** These keys allow you to assign attributes to the captures of the match, begin, or end patterns. Using the captures key for
     * a begin/end rule is short-hand for giving both beginCaptures and endCaptures with same values.
     * The value of these keys is a dictionary with the key being the capture number and the value being a dictionary of attributes
     * to assign to the captured text.  */
    endCaptures?: CaptureList;

    whileCaptures?: CaptureList;

    /** This allows you to reference a different language, recursively reference the grammar itself or a rule declared in this file’s repository.
     *
     * To reference another language, use the scope name of that language. Example: "source.php"
     *
     * To reference the grammar itself, use "$self".
     *
     * To reference a rule from the current grammars repository, prefix the name with a pound sign (#). Example: "#rule" */
    include?: string;

    patterns?: TmRule[];

    applyEndPatternsLast?: 0 | 1;
}

export type CaptureList = {
    [index: number]: { name?: string; patterns?: TmRule[] };
};
