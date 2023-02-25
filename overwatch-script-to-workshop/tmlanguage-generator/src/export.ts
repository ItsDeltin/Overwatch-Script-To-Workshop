import { ostw_grammar } from "./ostw/ostw-grammar";
import { ow_grammar } from "./ow/ow-grammar";
import { TmLanguage } from "./template";
import * as fs from 'fs';
import * as path from 'path';

function write(language: TmLanguage, name: string)
{
    let json = JSON.stringify(language, null, 2);
    let out = path.resolve(__dirname, '../../../syntaxes/' + name + '.tmLanguage.json');
    fs.writeFileSync(out, json);
    console.log('writing ' + name + ' grammar to ' + out);
}

write(ostw_grammar, 'ostw');
write(ow_grammar, 'ow');