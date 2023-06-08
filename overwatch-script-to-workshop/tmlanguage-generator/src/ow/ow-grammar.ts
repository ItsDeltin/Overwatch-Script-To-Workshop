import * as tm from '../index';
import { getRepository } from './patterns';
import { Repository } from './repository';

export const ow_grammar = tm.createTextmateGrammar({
    name: 'Workshop',
    fileTypes: ['ow', 'overwatch', 'workshop', 'ws'], // todo
    firstLineMatch: /settings|variables|subroutines|rule|\[[0-9]{2}:[0-9]{2}:[0-9]{2}\]/,
    scopeName: 'source.ow',
    repository: getRepository(),
    patterns: [
        { include: Repository.comment },
        { include: Repository.settings },
        { include: Repository.variables },
        { include: Repository.subroutines },
        { include: Repository.rule },
        { include: Repository.action_list },
        { include: Repository.condition_list },
        { include: Repository.action },
        { include: Repository.expression },
    ]
});