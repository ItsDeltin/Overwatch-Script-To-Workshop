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

define : accessor? STATIC? (GLOBAL|PLAYER)? REF? (code_type | DEFINE) name=PART (id=number? | NOT?) (EQUALS expr?)? ;

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
	| BASE                                                                             #e_base
	| expr SEPERATOR (method | variable)?											   #e_expr_tree
	| NOT expr                                                                         #e_not
	| '-' expr                                                                         #e_inverse
	| expr IS type=PART?                                                               #e_is
	| lambda                                                                           #e_lambda
	| <assoc=right> left=expr op=('^' | '*' | '/' | '%') right=expr                    #e_op_1
	| left=expr op=('+' | '-') right=expr                                              #e_op_2
	| left=expr op=(LESS_THAN | '<=' | '==' | '>=' | GREATER_THAN | '!=') right=expr   #e_op_compare
	| condition=expr TERNARY consequent=expr? TERNARY_ELSE alternative=expr 		   #e_ternary_conditional
	| left=expr BOOL right=expr                                                        #e_op_bool
	;

typeconvert : LESS_THAN code_type? GREATER_THAN expr? ;

exprgroup   : LEFT_PAREN expr RIGHT_PAREN ;
createarray : INDEX_START (expr (COMMA expr)*)? INDEX_END;

array : (INDEX_START expr INDEX_END)+ ;

varset   : var=expr array? ((statement_operation val=expr?) | INCREMENT | DECREMENT) ;
statement_operation : EQUALS | EQUALS_ADD | EQUALS_DIVIDE | EQUALS_MODULO | EQUALS_MULTIPLY | EQUALS_POW | EQUALS_SUBTRACT ;

method         : (ASYNC NOT?)? PART LEFT_PAREN call_parameters? RIGHT_PAREN ;
call_parameters: call_parameter (COMMA call_parameter?)*   ;
call_parameter : (PART? TERNARY_ELSE)? expr					 ;

variable : PART array? ;
code_type: PART (INDEX_START INDEX_END)* generics?;
generics : LESS_THAN (generic_option (COMMA generic_option)*)? GREATER_THAN;
generic_option: code_type | DEFINE;

lambda: (define | LEFT_PAREN (define (COMMA define)*)? RIGHT_PAREN) INS (expr | block) ;

documented_statement: DOCUMENTATION? statement;
statement :
	  define STATEMENT_END?   #s_define
	| varset STATEMENT_END?   #s_varset
	| method STATEMENT_END?   #s_method
	| if 					  #s_if
	| for					  #s_for
	| for_auto                #s_for_auto
	| foreach				  #s_foreach
	| while					  #s_while
	| return				  #s_return
	| expr STATEMENT_END?	  #s_expr
	| delete STATEMENT_END?	  #s_delete
	| CONTINUE STATEMENT_END? #s_continue
	| BREAK STATEMENT_END?    #s_break
	| switch 				  #s_switch
	| (BLOCK_START documented_statement* BLOCK_END) #s_block
	;

block : (BLOCK_START documented_statement* BLOCK_END) | documented_statement | STATEMENT_END  ;

for     : FOR LEFT_PAREN 
	((define | initialVarset=varset)? STATEMENT_END expr? STATEMENT_END endingVarset=varset?)
	RIGHT_PAREN block;

for_auto : FOR LEFT_PAREN
	((forVariable=expr (EQUALS start=expr?)? | forDefine=define)? startSep=STATEMENT_END stop=expr? stopSep=STATEMENT_END step=expr?)
	RIGHT_PAREN block?;

foreach : FOREACH number? LEFT_PAREN (code_type | DEFINE) name=PART IN expr? RIGHT_PAREN block ;

while   : WHILE LEFT_PAREN expr RIGHT_PAREN block             ;

if      : IF LEFT_PAREN expr? RIGHT_PAREN block? else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr? RIGHT_PAREN block?           ;
else    : ELSE block?                                           ;

return  : RETURN expr? STATEMENT_END                          ;
delete  : DELETE LEFT_PAREN expr RIGHT_PAREN                  ;

switch  : SWITCH LEFT_PAREN expr? RIGHT_PAREN
	BLOCK_START switch_element* BLOCK_END;

switch_element:  (DEFAULT TERNARY_ELSE?) | case | documented_statement;

case    : CASE expr? TERNARY_ELSE?;

rule_if : IF LEFT_PAREN expr? RIGHT_PAREN;

ow_rule : 
	DISABLED? RULE_WORD ':' STRINGLITERAL number?
	expr*
	rule_if*
	block?
	;

define_method : DOCUMENTATION* method_attributes* (VOID | DEFINE | code_type) name=PART LEFT_PAREN setParameters RIGHT_PAREN ((GLOBAL | PLAYER)? subroutineRuleName=STRINGLITERAL)?
	block?
	;

method_attributes : accessor | STATIC | OVERRIDE | VIRTUAL | RECURSIVE;

define_macro  : DOCUMENTATION* accessor? STATIC? (DEFINE | code_type) name=PART (LEFT_PAREN setParameters RIGHT_PAREN)? TERNARY_ELSE? expr? STATEMENT_END? ;

ruleset :
	reserved_global?
	reserved_player?
	import_file*
	((define STATEMENT_END) | ow_rule | define_method | define_macro | type_define | enum_define)*
	EOF;

// Classes/structs

type_define : (STRUCT | CLASS) name=PART (TERNARY_ELSE extends=PART?)?
	BLOCK_START
	((define STATEMENT_END) | constructor | define_method | define_macro)*
	BLOCK_END ;

enum_define : ENUM name=PART BLOCK_START (firstMember=PART enum_element*)? BLOCK_END ;
enum_element : COMMA PART ;

accessor : PRIVATE | PUBLIC | PROTECTED;

constructor : accessor? name=PART LEFT_PAREN setParameters RIGHT_PAREN block ;

setParameters: (define (COMMA define)*)?;

create_object : NEW (type=PART (LEFT_PAREN call_parameters? RIGHT_PAREN)) ;

import_file : IMPORT STRINGLITERAL (AS name=PART?)? STATEMENT_END ;

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
RECURSIVE : 'recursive' ;
RETURN    : 'return'    ;
WHILE     : 'while'     ;
STRUCT    : 'struct'    ;
CLASS     : 'class'     ;
PRIVATE   : 'private'   ;
PUBLIC    : 'public'    ;
PROTECTED : 'protected' ;
THIS      : 'this'      ;
ROOT      : 'root'      ;
NEW       : 'new'       ;
STATIC    : 'static'    ;
IMPORT    : 'import'    ;
AS        : 'as'        ;
DELETE    : 'delete'    ;
DISABLED  : 'disabled'  ;
ENUM      : 'enum'      ;
REF       : 'ref'       ;
VOID      : 'void'		;
ASYNC     : 'async'		;
OVERRIDE  : 'override'  ;
VIRTUAL   : 'virtual'   ;
BREAK     : 'break'     ;
CONTINUE  : 'continue'  ;
SWITCH    : 'switch'	;
CASE      : 'case'		;
DEFAULT   : 'default'   ;
BASE      : 'base'      ;
IS        : 'is'		;
INTERFACE : 'interface' ;

INS             : '=>'  ;
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