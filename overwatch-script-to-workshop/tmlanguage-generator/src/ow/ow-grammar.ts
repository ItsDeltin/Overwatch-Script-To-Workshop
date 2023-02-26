import * as tm from '../index';
import { getRepository } from './patterns';
import { Repository } from './repository';

export const ow_grammar = tm.createTextmateGrammar({
    name: 'Workshop',
    fileTypes: ['ow', 'overwatch', 'workshop', 'ws'], // todo
    firstLineMatch: /\b(settings|variables|subroutines|rule|\[[0-9]{2}:[0-9]{2}:[0-9]{2}\])\b/,
    scopeName: 'source.ow',
    repository: getRepository(),
    patterns: [
        { include: Repository.generated_code_timestamp },
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