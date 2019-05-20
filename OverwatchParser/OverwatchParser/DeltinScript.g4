grammar DeltinScript;

/*
 * Parser Rules
 */

number : NUMBER | neg  ;
neg    : '-'NUMBER     ;
string : STRINGLITERAL ;
true   : TRUE          ;
false  : FALSE         ;
null   : NULL          ;
not    : NOT           ;

vardefine : DEFINE (GLOBAL|PLAYER) PART (PART (INDEX_START number INDEX_END)?)? STATEMENT_END; /*#define global/player NAME (use variable?); */
useGlobalVar : USEVAR GLOBAL PART STATEMENT_END ;
usePlayerVar : USEVAR PLAYER PART STATEMENT_END ;

expr 
	: 
      number                                      // Numbers
	| method                                      // Methods
	| string                                      // Strings
	| enum                                        // Enums
	| expr INDEX_START expr INDEX_END             // Array creation
	| INDEX_START expr (',' expr)* INDEX_END      // Arrays
	| '<' string (',' expr)* '>'                  // Formatted strings
	| true                                        // True
	| false                                       // False
	| null                                        // Null
	| expr SEPERATOR expr                         // Variable seperation
	| variable                                    // Variables
	|<assoc=right> '(' expr ')'                   // Groups
	| expr '^' expr                               // x^y
	| expr '*' expr                               // x*y
	| expr '/' expr                               // x/y
	| expr '%' expr                               // x%y
	| expr '+' expr                               // x+y
	| expr '-' expr                               // x-y
	| not expr                                    // !x
	| expr BOOL expr                              // x & y
	| expr ('<' | '<=' | '==' | '>=' | '>' | '!=') expr // x == y
	;

enum     : (BUTTON | COLOR | EFFECT | EFFECTREV) SEPERATOR PART ;
variable : PART ;
method   : PART LEFT_PAREN expr? (',' expr)* RIGHT_PAREN ;

statement :
	( method STATEMENT_END
	| expr STATEMENT_OPERATION expr STATEMENT_END
	| GOTO
	| GOTO_STATEMENT
	| if
	| for
	);

block : BLOCK_START statement* BLOCK_END ;

for     : FOR LEFT_PAREN PART IN expr RIGHT_PAREN block       ; /* Syntax: for (VARIABLE in ARRAY) */
if      : IF LEFT_PAREN expr RIGHT_PAREN block else_if* else? ;
else_if : ELSE IF LEFT_PAREN expr RIGHT_PAREN block           ;
else    : ELSE block                                          ;

rule_if : IF LEFT_PAREN expr RIGHT_PAREN ;

ow_rule : 
	RULE_WORD ':' STRINGLITERAL expr* 
	(rule_if)*
	block
	;

ruleset :
	useGlobalVar
	usePlayerVar
	vardefine*
	ow_rule*
	;

/*
 * Lexer Rules
 */

fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
fragment NUMBERS    : [0-9] ;

// Strings have priorty over everything!
STRINGLITERAL             : UNTERMINATEDSTRINGLITERAL '"'      ;
UNTERMINATEDSTRINGLITERAL : '"' (~["\\\r\n] | '\\' (. | EOF))* ;

// Comments
COMMENT : (('/*' .*? '*/') | ('//' .*? NEWLINE))? -> skip ;

// Misc
WHITESPACE : (' '|'\t')+ -> skip ;
NEWLINE    : ('\r'? '\n' | '\r')+ -> skip;

// Goto statement
/* split this into a parse statement later */
GOTO           : '@goto' WHITESPACE+ PART ';' ;
GOTO_STATEMENT :  'goto' WHITESPACE+ PART ';' ;

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

// Keywords
RULE_WORD : 'rule'      ;
IF        : 'if'        ;
ELSE      : 'else'      ;
FOR       : 'for'       ;
IN        : 'in'        ;
DEFINE    : 'define'    ;
USEVAR    : 'usevar'    ;
PLAYER    : 'playervar' ;
GLOBAL    : 'globalvar' ;
TRUE      : 'true'      ;
FALSE     : 'false'     ;
NULL      : 'null'      ;
ARRAY     : 'array'     ;

// Enum
BUTTON    : 'Button'    ;
COLOR     : 'Color'     ;
EFFECT    : 'Effect'    ;
EFFECTREV : 'EffectRev' ;

STATEMENT_OPERATION : '=' | '^=' | '*=' | '/=' | '+=' | '-=' | '%=';
BOOL : '&' | '|';
NOT : '!';

PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+   ;