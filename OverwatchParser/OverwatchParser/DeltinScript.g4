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

vardefine : DEFINE (GLOBAL|PLAYER) PART (PART | ARRAY)? STATEMENT_END; /*#define global/player NAME (use variable?); */
useGlobalVar : USEVAR GLOBAL PART STATEMENT_END ;
usePlayerVar : USEVAR PLAYER PART STATEMENT_END ;

expr 
	: number
	| method
	| variable
	| string
	| array
	| true
	| false
	| null
	| seperated
	|<assoc=right> '(' expr ')'
	| expr '^' expr
	| expr '*' expr
	| expr '/' expr
	| expr '+' expr
	| expr '-' expr 
	| not expr
	| expr BOOL expr
	| expr COMPARE expr
	;

seperated: PART SEPERATOR PART ;
method : PART LEFT_PAREN expr? (',' expr)* RIGHT_PAREN  ;
variable : PART;
array  : PART INDEX_START expr INDEX_END ;

statement :
	( method STATEMENT_END
	| expr STATEMENT_OPERATION expr STATEMENT_END
	| GOTO
	| GOTO_STATEMENT
	| IF LEFT_PAREN expr RIGHT_PAREN block (ELSE IF LEFT_PAREN expr RIGHT_PAREN)* (block ELSE block)?
	| FOR LEFT_PAREN expr IN expr RIGHT_PAREN block /* Syntax: for (VARIABLE in ARRAY) */
	);

block : BLOCK_START statement* BLOCK_END ;

ow_rule : 
	RULE_WORD ':' STRINGLITERAL (',' expr)* 
	(IF LEFT_PAREN expr RIGHT_PAREN)*
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

/*
ONGOING_GLOBAL            : 'Event.Ongoing_Global'          ;
ONGOING_EACH_PLAYER       : 'Event.OngoingPlayer'           ;
PLAYER_EARNED_ELIMINATION : 'Event.PlayerEarnedElimination' ;
PLAYER_DEALT_FINAL_BLOW   : 'Event.PlayerDealtFinalBlow'    ;
PLAYER_DEALT_DAMAGE       : 'Event.PlayerDealtDamage'       ;
PLAYER_TOOK_DAMAGE        : 'Event.PlayerTookDamage'        ;
PLAYER_DIED               : 'Event.PlayerDied'              ;
*/

COMPARE : '<' | '<=' | '==' | '>=' | '>' | '!=';
STATEMENT_OPERATION : '=' | '^=' | '*=' | '/=' | '+=' | '-=';
BOOL : '&' | '|';
NOT : '!';

PART : (LOWERCASE | UPPERCASE | '_' | NUMBERS)+   ;