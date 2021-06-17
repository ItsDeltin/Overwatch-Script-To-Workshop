import * as tm from '../index';
import { Repository } from './repository';
import { getRepository } from './patterns';
import * as fs from 'fs';
import * as path from 'path';

let grammar = tm.createTextmateGrammar({
    name: 'Overwatch Script To Workshop',
    fileTypes: ['ostw', 'del', 'workshop'],
    firstLineMatch: tm.Or([tm.WordBoundary(), 'rule', tm.WordBoundary()]),
    scopeName: 'source.del',
    repository: getRepository(),
    patterns: [
        { include: Repository.new_ },
        { include: Repository.class_struct_declaration },
        { include: Repository.enum_declaration },
        { include: Repository.rule },
        { include: Repository.function_declaration },
        { include: Repository.variable_declaration },
    ],
});

let json = JSON.stringify(grammar, null, 2);
let out = path.resolve(__dirname, '../../../syntaxes/ostw.tmLanguage.json');
fs.writeFileSync(out, json);
console.log('writing grammar to ' + out);
