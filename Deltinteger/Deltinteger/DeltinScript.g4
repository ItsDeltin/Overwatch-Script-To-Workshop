grammar DeltinScript;

/*
 * Parser Rules
 */

reserved_global : GLOBAL BLOCK_START reserved_list? BLOCK_END ;
reserved_player : PLAYER BLOCK_START reserved_list? BLOCK_END ;
reserved_list : (PART | NUMBER) (COMMA (PART | NUMBER))* ;

number : NUMBER | neg  ;
neg    : '-'NUMBER     ;
string : LOCALIZED? STRINGLITERAL ;
formatted_string: LESS_THAN string (COMMA expr)* GREATER_THAN ;
true   : TRUE          ;
false  : FALSE         ;
null   : NULL          ;

define : accessor? STATIC? (GLOBAL|PLAYER)? (code_type | DEFINE) name=PART (id=number? | NOT?) (EQUALS expr?)? ;

expr 
	: 
      number                                                                           #e_number
	| method                                                                           #e_method
	| string                                                                           #e_string
	| array=expr INDEX_START index=expr? INDEX_END                                     #e_array_index
	| createarray                                                                      #e_create_array
	| formatted_string                                                                 #e_formatted_string
	| true                                                                             #e_true
	| false                                                                            #e_false
	| null                                                                             #e_null
	| variable                                                                         #e_variable
	| exprgroup								                                           #e_expr_group
	| create_object							                                           #e_new_object
	| typeconvert							                                           #e_type_convert
	| THIS									                                           #e_this
	| ROOT								                                               #e_root
	| expr SEPERATOR (method | variable)?											   #e_expr_tree
	| NOT expr                                                                         #e_not
	| '-' expr                                                                         #e_inverse
	| <assoc=right> left=expr op=('^' | '*' | '/' | '%') right=expr                    #e_op_1
	| left=expr op=('+' | '-') right=expr                                              #e_op_2
	| left=expr op=(LESS_THAN | '<=' | '==' | '>=' | GREATER_THAN | '!=') right=expr   #e_op_compare
	| condition=expr TERNARY consequent=expr? TERNARY_ELSE alternative=expr 		   #e_ternary_conditional
	| left=expr BOOL right=expr                                                        #e_op_bool
	;

typeconvert : LESS_THAN PART? GREATER_THAN expr ;

exprgroup   : LEFT_PAREN expr RIGHT_PAREN ;
createarray : INDEX_START (expr (COMMA expr)*)? INDEX_END;

array : (INDEX_START expr INDEX_END)+ ;

varset   : var=expr array? ((statement_operation val=expr?) | INCREMENT | DECREMENT) ;
statement_operation : EQUALS | EQUALS_ADD | EQUALS_DIVIDE | EQUALS_MODULO | EQUALS_MULTIPLY | EQUALS_POW | EQUALS_SUBTRACT ;

call_parameters  : expr (COMMA expr?)*    		 	         ;
picky_parameter  : PART? TERNARY_ELSE expr?                  ;
picky_parameters : picky_parameter (COMMA picky_parameter?)* ;
method           : PART LEFT_PAREN (picky_parameters | call_parameters)? RIGHT_PAREN ;

variable : PART array? ;
code_type: PART (INDEX_START INDEX_END)*;

statement :
	  varset STATEMENT_END? #s_varset
	| method STATEMENT_END? #s_method
	| if 					#s_if
	| for					#s_for
	| foreach				#s_foreach
	| while					#s_while
	| define STATEMENT_END? #s_define
	| return				#s_return
	| expr STATEMENT_END?	#s_expr
	| delete STATEMENT_END?	#s_delete
	;

block : (BLOCK_START statement* BLOCK_END) | statement | STATEMENT_END  ;

for     : FOR LEFT_PAREN 
	((define | initialVarset=varset)? STATEMENT_END expr? STATEMENT_END endingVarset=varset?)
	RIGHT_PAREN block;

foreach : FOREACH number? LEFT_PAREN (code_type | DEFINE) name=PART IN expr? RIGHT_PAREN block ;

while   : WHILE LEFT_PAREN expr RIGHT_PAREN block             ;

if      : IF LEFT_PAREN expr? RIGHT_PAREN block? else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr? RIGHT_PAREN block?           ;
else    : ELSE block?                                           ;

return  : RETURN expr? STATEMENT_END                          ;
delete  : DELETE LEFT_PAREN expr RIGHT_PAREN                  ;

rule_if : IF LEFT_PAREN expr? RIGHT_PAREN;

ow_rule : 
	DISABLED? RULE_WORD ':' STRINGLITERAL
	expr*
	rule_if*
	block?
	;

define_method : DOCUMENTATION* accessor? method_attribute* (METHOD | code_type) name=PART LEFT_PAREN setParameters RIGHT_PAREN
	block?
	;

method_attribute : RECURSIVE | RULE_WORD ;

define_macro  : DOCUMENTATION* accessor? MACRO name=PART (LEFT_PAREN setParameters RIGHT_PAREN)? TERNARY_ELSE? expr? STATEMENT_END ;

ruleset :
	reserved_global?
	reserved_player?
	(import_file | import_object)*
	((define STATEMENT_END) | ow_rule | define_method | define_macro | type_define | enum_define)*
	EOF;

// Classes/structs

type_define : (STRUCT | CLASS) name=PART
	BLOCK_START
	((define STATEMENT_END) | constructor | define_method | define_macro)*
	BLOCK_END ;

enum_define : ENUM name=PART BLOCK_START (firstMember=PART enum_element*)? BLOCK_END ;
enum_element : COMMA PART ;

accessor : PRIVATE | PUBLIC;

constructor : accessor? name=PART LEFT_PAREN setParameters RIGHT_PAREN block ;

setParameters: (define (COMMA define)*)?;

create_object : NEW (type=PART (LEFT_PAREN call_parameters? RIGHT_PAREN)) ;

import_file : IMPORT STRINGLITERAL STATEMENT_END ;
import_object : IMPORT file=STRINGLITERAL AS name=PART STATEMENT_END ;

/*
 * Lexer Rules
 */

fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment NUMBERS    : [0-9] ;

// Strings have priorty over everything!
STRINGLITERAL             : UNTERMINATEDSTRINGLITERAL '"'      ;
UNTERMINATEDSTRINGLITERAL : '"' (~["\\\r\n] | '\\' (. | EOF))* ;

DOCUMENTATION: '#' .*? NEWLINE ;
// Comments
COMMENT : (('/*' .*? '*/') | ('//' .*? NEWLINE)) -> skip ;

// Misc
WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE    : ('\r'? '\n' | '\r')+ -> skip;

// Numbers
NUMBER : [0-9]+ ('.'[0-9]+)?  ;

// Groupers
LEFT_PAREN    : '(' ;
RIGHT_PAREN   : ')' ;
BLOCK_START   : '{' ;
BLOCK_END     : '}' ;
INDEX_START   : '[' ;
INDEX_END     : ']' ;
STATEMENT_END : ';' ;
SEPERATOR     : '.' ;
COMMA         : ',' ;
TERNARY       : '?' ;
TERNARY_ELSE  : ':' ;
LOCALIZED     : '@' ;

// Keywords
RULE_WORD : 'rule'      ;
IF        : 'if'        ;
ELSE      : 'else'      ;
FOR       : 'for'       ;
FOREACH   : 'foreach'   ;
IN        : 'in'        ;
DEFINE    : 'define'    ;
USEVAR    : 'usevar'    ;
GLOBAL    : 'globalvar' ;
PLAYER    : 'playervar' ;
TRUE      : 'true'      ;
FALSE     : 'false'     ;
NULL      : 'null'      ;
METHOD    : 'method'    ;
RECURSIVE : 'recursive' ;
RETURN    : 'return'    ;
WHILE     : 'while'     ;
STRUCT    : 'struct'    ;
CLASS     : 'class'     ;
PRIVATE   : 'private'   ;
PUBLIC    : 'public'    ;
THIS      : 'this'      ;
ROOT      : 'root'      ;
NEW       : 'new'       ;
STATIC    : 'static'    ;
IMPORT    : 'import'    ;
AS        : 'as'        ;
DELETE    : 'delete'    ;
MACRO     : 'macro'     ;
DISABLED  : 'disabled'  ;
ENUM      : 'enum'      ;

EQUALS          : '='  ;
EQUALS_POW      : '^=' ;
EQUALS_MULTIPLY : '*=' ;
EQUALS_DIVIDE   : '/=' ;
EQUALS_ADD      : '+=' ;
EQUALS_SUBTRACT : '-=' ;
EQUALS_MODULO   : '%=' ;

POW   : '^';
MULT  : '*';
DIV   : '/';
MOD   : '%';
ADD   : '+';
MINUS : '-';

LESS_THAN    : '<' ;
GREATER_THAN : '>';

BOOL : '&&' | '||';
NOT : '!';
INCREMENT : '++' ;
DECREMENT : '--' ;

PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+ ;