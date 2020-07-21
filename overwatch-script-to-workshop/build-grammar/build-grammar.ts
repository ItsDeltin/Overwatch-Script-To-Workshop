/*
This will convert the ostw.tmLanguage.yaml file into a tmlanguage file that can be used for syntax highlighting.
*/

import fs = require('fs');
import yaml = require('js-yaml');
import path = require('path');

var variables: {[name: string]:string} = {};

function makeTmLanuage() {
    // Get a reference to the ostw.tmLanguage.yaml file defined in this folder.
    let filePath: string = path.join(__dirname, '..', 'ostw.tmLanguage.yaml');
    let tmLanguageYaml = yaml.safeLoad(fs.readFileSync(filePath, 'utf8'));

    // Get variables
    for (const key in tmLanguageYaml.variables) {
        const value = tmLanguageYaml.variables[key];
        variables[key] = value;
    }

    // Apply variables
    applyVariables(tmLanguageYaml.repository);

    // The variables are no longer needed, and they are useless in the final output.
    delete tmLanguageYaml.variables;

    // Save to /overwatch-script-to-workshop/syntaxes/ostw.tmLanguage.json
    // __dirname is /overwatch-script-to-workshop/build-grammar/out/, so use 2 '..' to go back 2 folders.
    let saveLocation = path.join(__dirname, '..', '..', 'syntaxes', 'ostw.tmLanguage.json');

    // Convert to JSON and save!
    fs.writeFileSync(saveLocation, JSON.stringify(tmLanguageYaml, null, 2));

    console.log('tmLanguage done!');
}

function applyVariables(obj: object) {
    for (const key in obj) {
        const value = obj[key];
        const type = typeof value;

        // Apply variables
        if (typeof value == 'string' && (key == 'begin' || key == 'end' || key == 'match' || key == 'name'))
            obj[key] = setVariables(value);

        // If the type of the property is an object, recursively call applyVariables.
        if (type == 'object')
            applyVariables(value);
    }
}

function setVariables(value:string):string
{
    return value.replace(/{{(.*?)}}/g, function(g0, g1) {
        if (g1 == null) return null;

        // group 1 is the variable name
        let replace = variables[g1];
        if (replace == undefined)
        {
            console.log('The variable {{' + g1 + '}} was not found, idiot.');
            return '';
        }
        return setVariables(replace);
    });
}

makeTmLanuage();