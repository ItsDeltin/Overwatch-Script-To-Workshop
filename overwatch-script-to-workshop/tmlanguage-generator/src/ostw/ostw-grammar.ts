import * as tm from '../index';
import { Repository } from './repository';
import { getRepository } from './patterns';

export const ostw_grammar = tm.createTextmateGrammar({
    name: 'Overwatch Script To Workshop',
    fileTypes: ['ostw', 'del', 'workshop'],
    firstLineMatch: tm.Or([tm.WordBoundary(), 'rule', tm.WordBoundary()]),
    scopeName: 'source.del',
    repository: getRepository(),
    patterns: [
        { include: Repository.comment },
        { include: Repository.new_ },
        { include: Repository.import_ },
        { include: Repository.class_struct_declaration },
        { include: Repository.enum_declaration },
        // ow
        { include: 'source.ow#rule' },
        { include: 'source.ow#settings' },
        { include: 'source.ow#variables' },
        { include: 'source.ow#subroutines' },
        // ostw
        { include: Repository.rule },
        { include: Repository.function_declaration },
        { include: Repository.variable_declaration },
        { include: Repository.statement },
    ],
});
