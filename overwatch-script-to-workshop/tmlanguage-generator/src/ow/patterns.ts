import * as tm from '../index';
import { b, w } from '../index';
import { Pattern } from '../pattern';
import { Repository } from './repository';
import * as util from './utils';

const elementName = /(?![\s(),;0-9])[^/\\\+\*\"\';<>=(),\{\}\[\]\.]+/;
const functionName = /(?![\s(),;0-9])[^/\\\+\*\"\';<>=(),\{\}\[\]\.]+/;

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
        patterns: [util.numberedList('variable')]
    }
]);

const subroutines: Pattern = util.makeDictionaryLike("subroutines", [util.numberedList('variable.function')]);

const rule: Pattern = {
    begin: 'rule',
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
                { include: Repository.condition_list }
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
                tm.Match(/Abort If|Skip If|Skip|While|If|Else If|For Player Variable|For Global Variable/, 'keyword.control.flow'),
                w,
                tm.Match('(', 'meta.brace.round')
            ],
            end: [tm.Match(')', 'meta.brace.round'), ';'],
            patterns: [
                { include: Repository.expression }
            ]
        },
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
        // Modify variable
        {
            begin: [
                tm.Match(/\bModify Global Variable|Modify Player Variable|Modify Global Variable At Index|Modify Player Variable At Index/, 'keyword.operator.assignment'),
                w, tm.Match('(', 'meta.brace.round'), w,
                tm.Match(util.variable, 'variable'),
            ],
            end: ')',
            zeroEndCapture: {name: 'meta.brace.round'},
            patterns: [
                { include: Repository.expression }
            ]
        },
        // Expression
        { include: Repository.expression },
        // Assignment compound
        {
            match: /\+=|-=|\/=|\*=|%=/,
            name: 'keyword.operator.assignment.compound'
        },
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
        { match: [b, 'Null', b], name: 'Null' },
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
                tm.Match(util.variable, 'variable')
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
        // Logical
        { match: /\|\||&&|!/, name: 'keyword.operator.logical' },
        // Relational
        { match: /<=|>=|<|>/, name: 'keyword.operator.relational' },
        // Comparison
        { match: /==|!=/, name: 'keyword.operator.comparison' },
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

const string_literal: Pattern = {
    patterns: [
        {
            match: /\"\"/,
            name: 'string.quoted.double'
        },
        {
            begin: '\"',
            end: /((?:^|[^\\])(?:\\{2})*)"/,
            name: 'string.quoted.double'
        }
    ]
};

const generated_code_timestamp: Pattern = {
    match: /\[[0-9]{2}:[0-9]{2}:[0-9]{2}\]/,
    name: 'markup.error'
};

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
        add(Repository.generated_code_timestamp, generated_code_timestamp);
    });
}
