import { Pattern } from '../Pattern';
import * as patterns from './patterns';

export namespace Repository {
    export const comment = '#comment';
    export const expression = '#expression';
    export const string_literal = '#string-literal';
    export const string = '#string-interpolated';
    export const escaped_string_character = '#escaped-string-character';
    export const interpolated_string_inner = '#interpolated-string-inner';
    export const number = '#number';
    export const block = '#block';
    export const statement = '#statement';
    export const statement_end = '#statement-end';
    export const assignment = '#assignment';
    export const if_statement = '#if-statement';
    export const parameter_list = '#parameter_list';
    export const parameter_declaration = '#parameter-declaration';
    export const code_type = '#type';
    export const code_type_matcher = '#type-matcher';
    export const variable_declaration = '#variable-declaration';
    export const function_declaration = '#function-declaration';
    export const constructor_declaration = '#constructor-declaration';
    export const class_struct_declaration = '#class-struct-declaration';
    export const enum_declaration = '#enum-declaration';
    export const type_args = '#type_args';
    export const rule = '#rule';
    export const argument_list = '#argument-list';
    export const function_call = '#function-call';
    export const new_ = '#new';
    export const import_ = '#import';
}
