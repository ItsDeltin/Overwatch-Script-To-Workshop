import * as tm from '../index';
import { Pattern } from '../Pattern';
import { Repository } from './repository';
import * as common_nodes from './common-nodes';
import { i, w, b, codeType } from './common-nodes';
import * as utils from './utils';
import { pair } from './utils';
import { Names } from './names';

const includeComment: Pattern = { include: Repository.comment };

// * Patterns
// Type-pattern
const codeTypePattern: Pattern = {
    match: codeType,
};

const codeTypeMatcher: Pattern = {
    patterns: [
        // constant types
        { match: 'const', name: 'storage.modifier' },
        // 'define'
        { match: 'define', name: 'keyword.other' },
        // Type identifiers
        { match: i, name: 'entity.name.type' },
        // Group
        { match: '(', name: 'punctuation.parenthesis.open' },
        { match: ')', name: 'punctuation.parenthesis.close' },
        // Array
        { match: '[', name: 'punctuation.squarebracket.open' },
        { match: ']', name: 'punctuation.squarebracket.close' },
        // Type args
        { match: '<', name: 'punctuation.definition.typeparameters.begin' },
        { match: '>', name: 'punctuation.definition.typeparameters.end' },
        { match: ',', name: Names.comma },
        // Lambda
        { match: '=>', name: 'storage.type.function.arrow' },
        // Union
        { match: '|', name: 'keyword.operator.type' },
    ],
};

// comments
const comment: Pattern = {
    patterns: [
        // Line comments
        {
            begin: '//',
            end: /(?=$)/,
            name: 'comment.line.double-slash',
            zeroBeginCapture: { name: Names.comment },
        },
        // Block comments
        {
            begin: '/*',
            end: '*/',
            name: 'comment.block',
            zeroCapture: { name: Names.comment },
        },
        // Documentation comments.
        {
            begin: '#',
            end: /(?=$)/,
            name: 'comment.block.documentation',
            zeroBeginCapture: { name: Names.comment },
        },
    ],
};

// import
const import_: Pattern = {
    begin: [b, 'import', b],
    end: ';',
    zeroBeginCapture: { name: 'keyword.other' },
    zeroEndCapture: { name: Names.terminator },
    // Highlight the string between the 'import' and the ';'.
    patterns: [{ include: Repository.string_literal }],
};

// * Patterns: Declarations
const matchAttributes = tm.Group({
    value: tm.ZeroOrMore([
        tm.Group({
            value: tm.Or(
                'public',
                'private',
                'protected',
                'virtual',
                'override',
                'abstract',
                'static',
                'recursive',
                'globalvar',
                'playervar'
            ),
            tmName: 'storage.modifier',
        }),
        b,
        w,
    ]),
});

// Variables
const variableDeclaration: Pattern = {
    begin: [
        matchAttributes,
        codeType,
        common_nodes.i_variable_field,
        w,
        tm
            .Or(
                [
                    // Extended collection.
                    tm.Maybe([
                        tm.Group({ value: '!', tmName: Names.assignment }),
                        w,
                    ]),
                    // Assignment
                    tm.Group({ value: '=', tmName: Names.assignment }),
                ],
                // Macro
                tm.Group({ value: ':' })
            )
            .Maybe(),
    ],
    end: ';',
    zeroEndCapture: { name: Names.terminator },
    patterns: [{ include: Repository.expression }],
};

// Constructors
const constructorDeclaration: Pattern = {
    begin: [
        matchAttributes,
        pair('constructor', 'storage.type'),
        w,
        tm.PositiveLookahead('('),
    ],
    end: tm.PositiveLookbehind('}'),
    patterns: [
        { include: Repository.parameter_list },
        { include: Repository.block },
    ],
};

// Functions
const functionDeclaration: Pattern = {
    begin: [
        matchAttributes, // Attributes.
        codeType, // Return type.
        common_nodes.i_function,
        w,
        tm.PositiveLookahead(tm.Or('(', '<')), // Makes sure this is actually a function.
    ],
    end: tm.PositiveLookbehind('}'),
    // Match ; (statement end) or } (block end)
    // end: tm.PositiveLookahead(tm.Or(';', '}')),
    patterns: [
        { include: Repository.type_args },
        { include: Repository.parameter_list },
        { include: Repository.block },
        // Subroutine name
        { include: Repository.string },
        // Subroutine variable type
        {
            match: [b, tm.Or('globalvar, playervar'), b],
            zeroCapture: { name: 'storage.modifier' },
        },
        // Macro
        {
            begin: ':',
            end: ';',
            zeroBeginCapture: { name: Names.assignment },
            zeroEndCapture: { name: Names.terminator },
            patterns: [{ name: Repository.expression }],
        },
    ],
};

// Parameter lists
const parameterList: Pattern = {
    begin: '(',
    end: ')',
    zeroBeginCapture: { name: 'punctuation.parenthesis.open' },
    zeroEndCapture: { name: 'punctuation.parenthesis.close' },
    patterns: [
        // Optional parameter
        {
            begin: '[',
            end: ']',
            patterns: [{ include: Repository.parameter_declaration }],
        },
        // Parameter
        { include: Repository.parameter_declaration },
        // Default values.
        { include: Repository.expression },
        // Default value =
        {
            match: '=',
            name: Names.assignment,
        },
    ],
};

// Single parameter
const parameterDeclaration: Pattern = {
    match: [
        // Match variable attributes 'ref' and 'in'.
        tm
            .Group({
                value: [tm.Or('ref', 'in'), w],
                tmName: 'storage.modifier',
            })
            .ZeroOrMore(),
        // Parameter type.
        codeType,
        // Parameter name.
        tm.Group({ value: i, tmName: 'variable.parameter' }),
    ],
};

// Class or struct declaration
const classOrStructDeclaration: Pattern = {
    begin: [
        tm.Or(
            utils.createTypeDescriptor('class'),
            utils.createTypeDescriptor('struct')
        ),
    ],
    end: tm.PositiveLookbehind('}'),
    patterns: [
        // Inheritance
        {
            begin: pair(':', Names.colon),
            while: pair(',', Names.comma),
            patterns: [{ include: Repository.code_type }],
        },
        // Type arguments
        { include: Repository.type_args },
        // Matches the contents inside the class or struct.
        {
            begin: common_nodes.bracket_open,
            end: common_nodes.bracket_close,
            patterns: [
                includeComment,
                { include: Repository.constructor_declaration },
                { include: Repository.function_declaration },
                { include: Repository.variable_declaration },
            ],
        },
    ],
};

const typeArgs: Pattern = {
    begin: pair('<', Names.typeparameters_begin),
    end: pair('>', Names.typeparameters_end),
    patterns: [
        {
            match: [
                pair(
                    tm.Maybe(['single', common_nodes.bw]),
                    Names.storage_modifier
                ),
                i,
            ],
            name: Names.type_parameter,
        },
        {
            match: common_nodes.comma,
        },
    ],
};

// Enum declaration
const enumDeclaration: Pattern = {
    begin: utils.createTypeDescriptor('enum'),
    end: common_nodes.encapsulated_block,
    patterns: [
        includeComment,
        // Enumerator contents ({...})
        {
            begin: common_nodes.bracket_open,
            end: common_nodes.bracket_close,
            patterns: [
                includeComment,
                // Enum value
                {
                    begin: i,
                    zeroBeginCapture: {
                        name: 'entity.name.variable.enum-member',
                    },
                    end: tm.Or(
                        common_nodes.comma,
                        common_nodes.encapsulated_block
                    ),
                    patterns: [
                        includeComment,
                        // These are matched if an enumerator is assigned to a value.
                        { match: '=', name: Names.assignment },
                        { include: Repository.expression },
                    ],
                },
            ],
        },
    ],
};

// Rules
const rule: Pattern = {
    begin: [
        // 'disabled' keyword.
        tm.Maybe([pair('disabled', Names.storage_modifier), w]),
        // 'rule' keyword.
        pair('rule', Names.keyword_control),
        w,
        // :
        pair(':', Names.colon),
    ],
    end: tm.PositiveLookbehind(tm.Or(';', '}')),
    patterns: [
        includeComment,
        // Rule name
        { include: Repository.string },
        // Rule order
        { include: Repository.number },
        // Event attributes
        {
            match: [
                pair(tm.Or('Event', 'Team', 'Player'), 'support.type'),
                w,
                pair('.', Names.dot),
                w,
                pair(i, 'support.variable'),
            ],
        },
        // Rule conditions
        { include: Repository.if_statement },
        { include: Repository.block },
    ],
};

// Blocks
const block: Pattern = {
    begin: common_nodes.bracket_open,
    end: common_nodes.bracket_close,
    patterns: [{ include: Repository.statement }],
};

// Statements
const statement: Pattern = {
    patterns: [
        includeComment,
        // return
        {
            begin: 'return',
            zeroBeginCapture: { name: 'keyword.control.flow.return' },
            end: ';',
            zeroEndCapture: { name: Names.terminator },
            patterns: [{ include: Repository.expression }],
        },
        // delete
        {
            begin: [b, 'delete', b],
            zeroBeginCapture: { name: 'keyword.operator.expression.delete' },
            end: ';',
            zeroEndCapture: { name: Names.terminator },
            patterns: [{ include: Repository.expression }],
        },
        // for
        {
            begin: [
                pair('for', 'keyword.control.loop.for'),
                w,
                pair('(', Names.parenthesis_open),
            ],
            end: ')',
            zeroEndCapture: { name: Names.parenthesis_close },
            patterns: [
                includeComment,
                { include: Repository.variable_declaration },
                { include: Repository.assignment },
                { include: Repository.expression },
                { include: Repository.statement_end },
            ],
        },
        // foreach
        {
            begin: [
                pair('for', 'keyword.control.loop.foreach'),
                w,
                pair('(', Names.parenthesis_open),
            ],
            end: ')',
            zeroEndCapture: { name: Names.parenthesis_close },
            patterns: [
                includeComment,
                // Match the 'in' keyword followed by the provided expression.
                {
                    begin: [b, 'in', b],
                    zeroBeginCapture: { name: 'keyword.control.loop.in' },
                    end: tm.PositiveLookahead(')'),
                    patterns: [{ include: Repository.expression }],
                },
                // Match the variable name.
                {
                    match: [codeType, w, pair(i, 'entity.name.variable.local')],
                },
            ],
        },
        // If/else-if
        { include: Repository.if_statement },
        // else
        { match: [b, 'else', b], name: Names.else_ },
        // switch/while
        {
            begin: [
                b,
                tm.Or(
                    pair('switch', Names.switch_),
                    pair('while', Names.while_)
                ),
                w,
                pair('(', Names.parenthesis_open),
            ],
            end: ')',
            zeroEndCapture: { name: Names.parenthesis_close },
            patterns: [{ include: Repository.expression }],
        },
        // break
        { match: [b, 'break', b], name: 'keyword.control.flow.break' },
        // continue
        { match: [b, 'continue', b], name: 'keyword.control.flow.continue' },
        // case
        {
            begin: [b, 'case', b],
            zeroBeginCapture: { name: 'keyword.control.case' },
            end: ':',
            zeroEndCapture: { name: 'punctuation.separator.colon' },
            patterns: [{ include: Repository.expression }],
        },
        // Variable declaration.
        { include: Repository.variable_declaration },
        // Function call
        { include: Repository.function_call },
        // Assignment
        { include: Repository.assignment },
        { include: Repository.expression },
        // Nested block.
        { include: Repository.block },
        // ;
        { include: Repository.statement_end },
    ],
};

const statementEnd: Pattern = {
    match: ';',
    zeroCapture: { name: Names.terminator },
};

const ifStatement: Pattern = {
    begin: [
        b,
        tm.Maybe([pair('else', Names.else_), w]),
        pair('if', Names.if_),
        w,
        pair('(', Names.parenthesis_open),
    ],
    end: ')',
    zeroEndCapture: { name: Names.parenthesis_close },
    patterns: [{ include: Repository.expression }],
};

const functionCall: Pattern = {
    begin: [
        common_nodes.accessorMatch,
        // Match the function's name,
        pair(i, Names.entity_name_function),
        w,
        // Match provided type args.
        common_nodes.typeArgs.Maybe(),
        w,
        // Match (
        common_nodes.parenthesis_open,
    ],
    end: ')',
    zeroEndCapture: { name: Names.parenthesis_close },
    patterns: [{ include: Repository.argument_list }],
};

// An argument list in a method call.
const argumentList: Pattern = {
    patterns: [
        includeComment,
        // Named argument
        // Ex:
        //   MyFunction(arg1: x, arg2: y, arg4: z);
        {
            // Match the parameter name and the colon.
            begin: [common_nodes.i_parameter, w, common_nodes.colon],
            // End at a , or ).
            end: tm.PositiveLookahead(tm.Or(',', ')')),
            patterns: [{ include: Repository.expression }],
        },
        // Match the actual values
        { include: Repository.expression },
        // Parameter comma seperator
        { match: ',', name: Names.comma },
    ],
};

// Matches assignment operators.
const assignment: Pattern = {
    match: tm.Or('=', '+=', '-=', '*=', '/=', '%=', '^=', '++', '--'),
    zeroCapture: { name: Names.assignment_compound },
};

const expressionPattern: Pattern = {
    patterns: [
        // number
        { include: Repository.number },
        // string
        { include: Repository.string },
        // true
        { match: 'true', name: 'constant.language.boolean.true' },
        // false
        { match: 'false', name: 'constant.language.boolean.false' },
        // function call
        { include: Repository.function_call },
        // new
        { include: Repository.new_ },
        // create array
        {
            begin: '[',
            zeroBeginCapture: { name: Names.squarebracket_open },
            end: ']',
            zeroEndCapture: { name: Names.squarebracket_close },
            patterns: [
                { include: Repository.expression },
                { match: ',', name: Names.comma },
            ],
        },
        // Ternary conditional
        {
            begin: '?',
            end: ':',
            zeroBeginCapture: {
                name: 'keyword.operator.conditional.question-mark',
            },
            zeroEndCapture: { name: 'keyword.operator.conditional.colon' },
            patterns: [{ include: Repository.expression }],
        },
        // Lambda (one parameter)
        {
            match: [
                tm.Maybe([codeType, w]),
                common_nodes.i_parameter,
                w,
                pair('=>', Names.arrow),
            ],
        },
        // Lambda (Zero or 2+ parameters) OR expression group
        {
            begin: '(',
            zeroBeginCapture: { name: Names.parameters_begin },
            end: [
                pair(')', Names.parameters_end),
                w,
                pair('=>', Names.arrow).Maybe(),
            ],
            patterns: [
                includeComment,
                // Match lambda parameter.
                {
                    match: [
                        tm.Maybe([codeType, w]),
                        common_nodes.i_parameter,
                        b,
                    ],
                },
                // Match parameter seperator
                { match: ',', name: Names.comma },
                // Expression group
                { include: Repository.expression },
            ],
        },
        // Struct declaration
        {
            begin: '{',
            end: '}',
            // Takes advantage of zeroCaptures applying to both 'begin' and 'end'
            zeroCapture: { name: Names.block_definition },
            patterns: [
                {
                    // Start of struct variable
                    begin: [
                        tm.Maybe([codeType, w]),
                        common_nodes.i_parameter,
                        w,
                        common_nodes.colon,
                    ],
                    // End struct variable at the seperator ','
                    // or the end of struct '}'
                    end: tm.Or(
                        pair(',', Names.comma),
                        tm.PositiveLookahead('}')
                    ),
                    patterns: [{ include: Repository.expression }],
                },
                // In lambda contexts, this may not actually be a struct.
                { include: Repository.statement },
            ],
        },
        // Type cast
        {
            match: [
                pair('<', Names.typeparameters_begin),
                w,
                codeType,
                w,
                pair('>', Names.typeparameters_end),
            ],
        },
        { match: tm.Or('==', '!='), name: 'keyword.operator.comparison' }, // Comparison
        {
            match: tm.Or('<=', '>=', '<', '>'),
            name: 'keyword.operator.relational',
        }, // Relational
        { match: tm.Or('!', '&&', '||'), name: 'keyword.operator.logical' }, // Logical
        {
            match: tm.Or('%', '*', '/', '-', '+', '^'),
            name: 'keyword.operator.arithmetic',
        }, // Arithmetic
        { match: '=>', name: 'storage.type.function.arrow' }, // Lambda
        { match: [b, 'null', b], name: 'constant.language.null' }, // null
        { match: [b, 'this', b], name: 'this' }, // this
        { match: [b, 'root', b], name: 'root' }, // root
        // Variable
        {
            match: [common_nodes.accessorMatch, common_nodes.i_variable, b],
        },
        // Type (type-args)
        {
            match: [
                common_nodes.accessorMatch,
                common_nodes.i_type,
                common_nodes.typeArgs,
            ],
        },
        // Lambda block
        { include: Repository.block },
    ],
};

// number pattern
const number: Pattern = {
    match: /(([0-9]+)?\.)?[0-9]+\b/,
    name: 'constant.numeric',
};

// string pattern
const string_: Pattern = {
    patterns: [
        // Single quote
        {
            begin: "'",
            end: [common_nodes.unescaped, "'"],
            name: Names.string_single,
            patterns: [{ include: Repository.escaped_string_character }],
        },
        // Double quote
        {
            begin: '"',
            end: [common_nodes.unescaped, '"'],
            name: Names.string_double,
            patterns: [{ include: Repository.escaped_string_character }],
        },
        // Interpolated string single quote
        {
            begin: ['$', w, "'"],
            end: [common_nodes.unescaped, "'"],
            name: Names.string_single,
            patterns: [
                { include: Repository.escaped_string_character },
                { include: Repository.interpolated_string_inner },
            ],
        },
        // Interpolated string double quote
        {
            begin: ['$', w, '"'],
            end: [common_nodes.unescaped, '"'],
            name: Names.string_double,
            patterns: [
                { include: Repository.escaped_string_character },
                { include: Repository.interpolated_string_inner },
            ],
        },
    ],
};

const escapedCharacter: Pattern = {
    match: ['\\', tm.Any()],
    name: 'constant.character.escape',
};

const interpolatedStringInner: Pattern = {
    // begin will match an unescaped '{'
    // good: {
    // bad:  {{
    // good: {{{
    begin: [
        tm.NegativeLookbehind('{'),
        tm.ZeroOrMore('{{'),
        pair('{', 'punctuation.definition.interpolation.begin'),
        tm.NegativeLookahead('{'),
    ],
    end: '}',
    zeroEndCapture: { name: 'punctuation.definition.interpolation.end' },
    patterns: [{ include: Repository.expression }],
};

const stringLiteral: Pattern = {
    match: common_nodes.string,
};

// new
const new_: Pattern = {
    begin: [
        pair('new', 'keyword.other.new'),
        w,
        codeType,
        w,
        common_nodes.parenthesis_open,
    ],
    end: ')',
    zeroEndCapture: { name: Names.parenthesis_close },
    patterns: [{ include: Repository.argument_list }],
};

export function getRepository() {
    let result: { [name: string]: Pattern } = {};
    function add(name: string, pattern: Pattern) {
        result[name.replace('#', '')] = pattern;
    }

    add(Repository.import_, import_);
    add(Repository.comment, comment);
    add(Repository.expression, expressionPattern);
    add(Repository.string_literal, stringLiteral);
    add(Repository.string, string_);
    add(Repository.escaped_string_character, escapedCharacter);
    add(Repository.interpolated_string_inner, interpolatedStringInner);
    add(Repository.number, number);
    add(Repository.block, block);
    add(Repository.statement, statement);
    add(Repository.statement_end, statementEnd);
    add(Repository.assignment, assignment);
    add(Repository.if_statement, ifStatement);
    add(Repository.parameter_list, parameterList);
    add(Repository.parameter_declaration, parameterDeclaration);
    add(Repository.code_type, codeTypePattern);
    add(Repository.code_type_matcher, codeTypeMatcher);
    add(Repository.variable_declaration, variableDeclaration);
    add(Repository.function_declaration, functionDeclaration);
    add(Repository.constructor_declaration, constructorDeclaration);
    add(Repository.class_struct_declaration, classOrStructDeclaration);
    add(Repository.enum_declaration, enumDeclaration);
    add(Repository.type_args, typeArgs);
    add(Repository.rule, rule);
    add(Repository.argument_list, argumentList);
    add(Repository.function_call, functionCall);
    add(Repository.new_, new_);

    return result;
}
