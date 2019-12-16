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
formatted_string: '<' string (COMMA expr)* '>' ;
true   : TRUE          ;
false  : FALSE         ;
null   : NULL          ;

statement_operation : EQUALS | EQUALS_ADD | EQUALS_DIVIDE | EQUALS_MODULO | EQUALS_MULTIPLY | EQUALS_POW | EQUALS_SUBTRACT ;

define : accessor? STATIC? (type=PART | DEFINE) (GLOBAL|PLAYER)? name=PART (id=number? | NOT?) (EQUALS expr?)? ;

expr 
	: 
      number                                            #e_number
	| method                                            #e_method
	| string                                            #e_string
	| array=expr INDEX_START index=expr? INDEX_END      #e_array_index
	| createarray                                       #e_create_array
	| formatted_string                                  #e_formatted_string
	| true                                              #e_true
	| false                                             #e_false
	| null                                              #e_null
	| PART                                              #e_variable
	| exprgroup								            #e_expr_group
	| create_object							            #e_new_object
	| typeconvert							            #e_type_convert
	| THIS									            #e_this
	| ROOT								                #e_root
	| <assoc=right> expr (SEPERATOR expr?)              #e_expr_tree
	| NOT expr                                          #e_not
	| '-' expr                                          #e_inverse
	| <assoc=right> expr ('^' | '*' | '/' | '%') expr   #e_op_1
	| expr ('+' | '-') expr                             #e_op_2
	| expr ('<' | '<=' | '==' | '>=' | '>' | '!=') expr #e_op_compare
	| expr TERNARY expr TERNARY_ELSE expr				#e_ternary_conditional
	| expr BOOL expr                              		#e_op_bool
	;

typeconvert : '<' PART '>' expr ;

exprgroup   : LEFT_PAREN expr RIGHT_PAREN ;
createarray : INDEX_START (expr (COMMA expr)*)? INDEX_END;

array : (INDEX_START expr INDEX_END)+ ;

varset   : var=expr array? ((statement_operation val=expr?) | INCREMENT | DECREMENT) ;

call_parameters  : expr (COMMA expr?)*    		 	         ;
picky_parameter  : PART? TERNARY_ELSE expr?                  ;
picky_parameters : picky_parameter (COMMA picky_parameter?)* ;
method           : PART LEFT_PAREN (picky_parameters | call_parameters)? RIGHT_PAREN ;

statement :
	( varset STATEMENT_END?
	| method STATEMENT_END?
	| if
	| for
	| foreach
	| while
	| define STATEMENT_END?
	| return
	| expr STATEMENT_END?
	| delete STATEMENT_END?
	);

block : (BLOCK_START statement* BLOCK_END) | statement | STATEMENT_END  ;

for     : FOR LEFT_PAREN 
	((define | varset)? STATEMENT_END expr? STATEMENT_END forEndStatement?)
	RIGHT_PAREN block;
forEndStatement : varset ;

foreach : FOREACH number? LEFT_PAREN define IN expr RIGHT_PAREN block ;

while   : WHILE LEFT_PAREN expr RIGHT_PAREN block             ;

if      : IF LEFT_PAREN expr RIGHT_PAREN block else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr RIGHT_PAREN block           ;
else    : ELSE block                                          ;

return  : RETURN expr? STATEMENT_END                          ;
delete  : DELETE LEFT_PAREN expr RIGHT_PAREN                  ;

rule_if : IF LEFT_PAREN expr? RIGHT_PAREN;

ow_rule : 
	DISABLED? RULE_WORD ':' STRINGLITERAL
	expr*
	rule_if*
	block
	;

define_method : DOCUMENTATION* accessor? RECURSIVE? (METHOD | type=PART) name=PART LEFT_PAREN setParameters RIGHT_PAREN
	block
	;

define_macro  : DOCUMENTATION* accessor? MACRO name=PART LEFT_PAREN setParameters RIGHT_PAREN TERNARY_ELSE expr? STATEMENT_END ;

ruleset :
	reserved_global?
	reserved_player?
	(import_file | import_object)*
	((define STATEMENT_END) | ow_rule | define_method | define_macro | type_define)*
	EOF;

// Classes/structs

type_define : (STRUCT | CLASS) name=PART
	BLOCK_START
	((define STATEMENT_END) | constructor | define_method | define_macro)*
	BLOCK_END ;

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

BOOL : '&&' | '||';
NOT : '!';
INCREMENT : '++' ;
DECREMENT : '--' ;

PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+ ;