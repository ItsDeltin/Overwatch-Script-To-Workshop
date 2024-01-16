import * as tm from '../index';
import { b, w } from '../index';
import { Pattern } from '../Pattern';
import { Repository } from './repository';
import * as util from './utils';

const elementName = /(?![\s(),;0-9])[^/\\\+\*\"\';<>=(),\{\}\[\]\.#`]+/;
const functionName = /(?![\s(),;0-9])[^/\\\+\*\"\';<>=(),\{\}\[\]\.#`]+/;

const comment: Pattern = {
    patterns: [
        // Line
        {
            begin: /\/\//,
            end: /(?=$)/,
            name: 'comment.line.double-slash'
        },
        // Block
        {
            begin: /\/\*/,
            end: /\*\//,
            name: 'comment.block.documentation'
        },
        // OSTW inline doc
        {
            match: /#/,
            name: 'comment.block.documentation'
        },
        // Generated code timestamp
        {
            match: /\[[0-9]{2}:[0-9]{2}:[0-9]{2}\]/,
            name: 'comment.block.documentation markup.error invalid.illegal'
        }
    ]
}

const settings: Pattern = util.makeDictionaryLike('settings', [
    util.makeDictionaryLike('lobby', []),
    util.makeDictionaryLike('modes', []),
    util.makeDictionaryLike('extensions', [{
        match: elementName
    }])
]);

const variables: Pattern = util.makeDictionaryLike('variables', [
    // Global variables
    {
        begin: [b, 'global', w, ':'],
        name: 'keyword.control',
        end: /(?=}|player\s*:)/,
        patterns: [util.numberedList('variable')]
    },
    // Player variables
    {
        begin: [b, 'player', w, ':'],
        name: 'keyword.control',
        end: /(?=}|global\s*:)/,
        patterns: [util.numberedList('variable.parameter')]
    }
]);

const subroutines: Pattern = util.makeDictionaryLike("subroutines", [util.numberedList('entity.name.function')]);

const rule: Pattern = {
    begin: /rule(?=\s*\()/,
    end: '}',
    zeroBeginCapture: { name: 'keyword.control' },
    zeroEndCapture: { name: 'punctuation.definition.block', },
    patterns: [
        // Rule name
        {
            begin: '(',
            end: ')',
            zeroCapture: { name: 'meta.brace.round' },
            patterns: [
                { include: Repository.string_literal }
            ]
        },
        // Rule content
        {
            begin: '{',
            end: /(?=})/,
            zeroBeginCapture: { name: 'punctuation.definition.block' },
            patterns: [
                // Event
                util.makeDictionaryLike('event', [
                    // Subroutine
                    {
                        begin: [tm.Match('Subroutine', 'storage.type.function'), w, ';'],
                        end: [tm.Match(util.variable, 'entity.name.function'), w, ';']
                    },
                    // Other
                    { match: elementName, name: 'meta.trait' },
                    { match: ';' }
                ]),
                // Actions and conditions
                { include: Repository.action_list },
                { include: Repository.condition_list },
                { include: Repository.comment }
            ]
        }
    ]
};

const action_list: Pattern = util.makeDictionaryLike('actions', [{include: Repository.action}]);
const condition_list: Pattern = util.makeDictionaryLike('conditions', [{ include: Repository.expression }]);

const action: Pattern = {
    patterns: [
        // Flow with parameters
        {
            begin: [
                tm.Match(/Abort If|Skip If|Skip|While|If|Else If/, 'keyword.control.flow'),
                w,
                tm.Match('(', 'meta.brace.round')
            ],
            end: [tm.Match(')', 'meta.brace.round'), ';'],
            patterns: [
                { include: Repository.expression }
            ]
        },
        // Flow (For Global Variable)
        util.setGlobalVariablePattern(/\bFor Global Variable/, 'keyword.control.flow'),
        // Flow (For Player Variable)
        util.setPlayerVariablePattern(/\bFor Player Variable/, 'keyword.control.flow'),
        // Flow without parameters
        {
            match: [tm.Match(/\bEnd|Loop If Condition Is True|Loop If Condition Is False|Loop|Else|Break|Continue|Abort/, 'keyword.control.flow'), ';'],
        },
        // Parameterless action.
        { match: [tm.Match([b, functionName, b], 'entity.name.function'), w, ';'] },
        // Call Subroutine (todo: start rule)
        {
            match: [
                tm.Match('Call Subroutine', 'entity.name.function'), w,
                tm.Match('(', 'meta.brace.round'), w,
                tm.Match(util.variable, 'entity.name.function'),
                tm.Match(')', 'meta.brace.round'), w,
                ';'
            ]
        },
        // Set/Modify global variable
        util.setGlobalVariablePattern(/\b(Modify|Set) Global Variable( At Index)?/, 'keyword.operator.assignment'),
        // Set/Modify player variable
        util.setPlayerVariablePattern(/\b(Modify|Set) Player Variable( At Index)?/, 'keyword.operator.assignment'),
        // Chase Global Variable
        util.setGlobalVariablePattern(/\bChase Global Variable (Over Time|At Rate)/, 'keyword.operator.assignment'),
        // Chase player variable
        util.setPlayerVariablePattern(/\bChase Player Variable (Over Time|At Rate)?/, 'keyword.operator.assignment'),
        // Action comment
        util.string('comment.block.documentation'),
        // Assignment compound
        {
            begin: /\+=|-=|\/=|\*=|%=/,
            end: ';',
            zeroBeginCapture: { name: 'keyword.operator.assignment.compound' },
            zeroEndCapture: { name: 'punctuation.terminator.statement' },
            patterns: [{ include: Repository.expression }]
        },
        // Expression
        { include: Repository.expression },
        // Assignment
        {
            match: /=/,
            name: 'keyword.operator.assignment'
        }
    ]
};

const expression: Pattern = {
    name: 'meta.expr',
    patterns: [
        // Comment
        { include: Repository.comment },
        // Comma in parameter list.
        { match: tm.Match(/,/, 'punctuation.separator.comma') },
        // Number (todo: all possible variants)
        { match: tm.Match(/-?[0-9]+(\.[0-9]+)?/, 'constant.numeric') },
        // String
        // Notice: translations!
        {
            begin: [tm.Match([b, /String|Custom String/, b], 'constant'), w,
                tm.Match('(', 'meta.brace.round')],
            end: tm.Match(')', 'meta.brace.round'),
            patterns: [
                { include: Repository.string_literal },
                { include: Repository.expression }
            ]
        },
        // Backup string literal
        { include: Repository.string_literal },
        // True
        { match: [b, 'True', b], name: 'constant.language.boolean.true' },
        // False
        { match: [b, 'False', b], name: 'constant.language.boolean.false' },
        // Null
        { match: [b, 'Null', b], name: 'constant.language.null' },
        // Global variable
        {
            match: [
                tm.Match('Global', 'variable.language.super'),
                tm.Maybe([
                    tm.Match('.', 'punctuation.accessor'),
                    tm.Maybe([
                        tm.Match(util.variable, 'variable')
                    ])
                ]),
            ]
        },
        // Player variable
        {
            match: [
                tm.Match('.', 'punctuation.accessor'),
                tm.Match(util.variable, 'variable.parameter')
            ]
        },
        // Array
        {
            begin: '[',
            zeroBeginCapture: { name: 'punctuation.squarebracket.open' },
            end: ']',
            zeroEndCapture: { name: 'punctuation.squarebracket.close' },
            patterns: [
                { include: Repository.expression },
            ],
        },
        // Arthimetic
        { match: /\+|-|\*|\/|%/, name: 'keyword.operator.arithmetic' },
        // Comparison
        { match: /==|!=/, name: 'keyword.operator.comparison' },
        // Logical
        { match: /\|\||&&|!/, name: 'keyword.operator.logical' },
        // Relational
        { match: /<=|>=|<|>/, name: 'keyword.operator.relational' },
        // Ternary
        { match: /\?|:/, name: 'keyword.operator.ternary' },
        // Group
        {
            begin: '(',
            end: ')',
            contentName: 'meta.expr.group',
            zeroCapture: { name: 'meta.brace.round' },
            patterns: [{include: Repository.expression}]
        },
        // Function
        { include: Repository.func },
        // Constant parameter
        { match: elementName, name: 'support.constant' },
    ]
};

const func: Pattern = {
    patterns: [
        // Normal
        {
            begin: [tm.Match(functionName, 'entity.name.function'), tm.Match('(', 'meta.brace.round')],
            end: ')',
            contentName: 'meta.parameter-list',
            zeroEndCapture: {name: 'meta.brace.round'},
            patterns: [
                {include:Repository.expression}
            ]
        },
        // Parameter-less constant
        {
            match: functionName,
            name: 'variable.other.constant'
        }
    ]
};

const string_literal: Pattern = util.string();

export function getRepository() {
    return tm.makeRepository(add => {
        add(Repository.comment, comment);
        add(Repository.action, action);
        add(Repository.action_list, action_list);
        add(Repository.condition_list, condition_list);
        add(Repository.expression, expression);
        add(Repository.func, func);
        add(Repository.string_literal, string_literal);
        add(Repository.settings, settings);
        add(Repository.variables, variables);
        add(Repository.subroutines, subroutines);
        add(Repository.rule, rule);
    });
}
