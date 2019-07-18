grammar DeltinScript;

/*
 * Parser Rules
 */

number : NUMBER | neg  ;
neg    : '-'NUMBER     ;
string : STRINGLITERAL ;
formatted_string: '<' string (COMMA expr)* '>' ;
true   : TRUE          ;
false  : FALSE         ;
null   : NULL          ;

statement_operation : EQUALS | EQUALS_ADD | EQUALS_DIVIDE | EQUALS_MODULO | EQUALS_MULTIPLY | EQUALS_POW | EQUALS_SUBTRACT ;

rule_define : (GLOBAL|PLAYER) define STATEMENT_END;
define   : (type=PART | DEFINE) name=PART useVar? (EQUALS expr?)? ;
useVar   : PART (INDEX_START number INDEX_END)? ;
useGlobalVar : USEVAR GLOBAL PART STATEMENT_END ;
usePlayerVar : USEVAR PLAYER PART STATEMENT_END ;

expr 
	: 
      number                                      // Numbers
	| method                                      // Methods
	| string                                      // Strings
	| { Deltin.Deltinteger.Elements.EnumData.IsEnum(_input.Lt(1).Text) }? enum // Enums
	| expr INDEX_START expr INDEX_END             // Array creation
	| createarray
	| formatted_string                            // Formatted strings
	| true                                        // True
	| false                                       // False
	| null                                        // Null
	| variable                                    // Variables
	| exprgroup
	| expr SEPERATOR variable                     // Variable seperation
	| NOT expr                                     // !x
	| expr ('<' | '<=' | '==' | '>=' | '>' | '!=') expr // x == y
	| expr BOOL expr                              // x & y
	| expr TERNARY expr TERNARY_ELSE expr
	| <assoc=right> expr ('^' | '*' | '/' | '%') expr // x^y
	| expr ('+' | '-') expr                           // x+y
	;

exprgroup   : LEFT_PAREN expr RIGHT_PAREN ;
createarray : INDEX_START (expr (COMMA expr)*)? INDEX_END;

array : (INDEX_START expr INDEX_END)+ ;

enum : PART SEPERATOR /*{ Deltin.Deltinteger.Elements.EnumData.IsEnum(_localctx?.PART(0)?.GetText()) }?*/ PART? ;

variable : PART ;
varset   : (expr SEPERATOR)? PART array? ((statement_operation expr?) | INCREMENT | DECREMENT) ;

call_parameters : expr (COMMA expr?)*    		 	     ;
method          : PART LEFT_PAREN call_parameters? RIGHT_PAREN ;

statement :
	( varset STATEMENT_END?
	| method STATEMENT_END?
	| if
	| for
	| foreach
	| while
	| define STATEMENT_END?
	| return
	);

block : (BLOCK_START statement* BLOCK_END) | statement | STATEMENT_END  ;

for     : FOR LEFT_PAREN 
	((define | varset)? STATEMENT_END expr? STATEMENT_END forEndStatement?)
	RIGHT_PAREN block;
forEndStatement : varset ;

foreach : FOREACH number? LEFT_PAREN DEFINE PART IN expr RIGHT_PAREN block ;

while   : WHILE LEFT_PAREN expr RIGHT_PAREN block             ;

if      : IF LEFT_PAREN expr RIGHT_PAREN block else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr RIGHT_PAREN block           ;
else    : ELSE block                                          ;

return  : RETURN expr? STATEMENT_END                          ;

rule_if : IF LEFT_PAREN expr? RIGHT_PAREN;

ow_rule : 
	RULE_WORD ':' STRINGLITERAL
	(enum)*
	(rule_if)*
	block
	;

user_method : DOCUMENTATION* RECURSIVE? METHOD PART LEFT_PAREN (PART (COMMA PART)*)? RIGHT_PAREN
	block
	;

ruleset :
	useGlobalVar?
	usePlayerVar?
	(rule_define | ow_rule | user_method | type_define)*
	;

// Classes/structs

type_define : (STRUCT | CLASS) name=PART
	BLOCK_START
	(define | constructor | user_method)*
	BLOCK_END ;

accessor : PRIVATE | PUBLIC;

constructor : accessor? name=PART LEFT_PAREN setParameters RIGHT_PAREN block ;

setParameters : (define? (COMMA define)*)? ;

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

// Keywords
RULE_WORD : 'rule'      ;
IF        : 'if'        ;
ELSE      : 'else'      ;
FOR       : 'for'       ;
FOREACH   : 'foreach'   ;
IN        : 'in'        ;
DEFINE    : 'define'    ;
USEVAR    : 'usevar'    ;
PLAYER    : 'playervar' ;
GLOBAL    : 'globalvar' ;
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

BOOL : '&' | '|';
NOT : '!';
INCREMENT : '++' ;
DECREMENT : '--' ;

PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+ ;