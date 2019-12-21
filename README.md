# Pascal Like Compiler Written in C#

### Scanner

* Breaks the progam up into tokens
* Takes this
```program bob;
var
	num1 : int;
	num2, num3 : int;
	str : string;
	aray: array [-1..8] of int;

// This is a comment and should not appear in your output
BEGIN
	num1 := 3;
	aray[1] := num1;
	num2 := -1;
	while (num2 <= 8) do
		begin
			aray[num2] = num2;
			num2 := num2 + 1
		end;
	write (num2);
	read (num3)
END.
```
* And turns it into this
```
Token Type: PROGRAM     Lexeme: program Line#: 1        Column#: 1
Token Type: IDENT       Lexeme: bob     Line#: 1        Column#: 9
Token Type: SEMICOLON   Lexeme: ;       Line#: 1        Column#: 12
Token Type: VARTOK      Lexeme: var     Line#: 2        Column#: 1
Token Type: IDENT       Lexeme: num1    Line#: 3        Column#: 2
Token Type: COLON       Lexeme: :       Line#: 3        Column#: 7
Token Type: INTTOK      Lexeme: int     Line#: 3        Column#: 9
Token Type: SEMICOLON   Lexeme: ;       Line#: 3        Column#: 12
Token Type: IDENT       Lexeme: num2    Line#: 4        Column#: 2
Token Type: COMMA       Lexeme: ,       Line#: 4        Column#: 6
Token Type: IDENT       Lexeme: num3    Line#: 4        Column#: 8
Token Type: COLON       Lexeme: :       Line#: 4        Column#: 13
Token Type: INTTOK      Lexeme: int     Line#: 4        Column#: 15
Token Type: SEMICOLON   Lexeme: ;       Line#: 4        Column#: 18
Token Type: IDENT       Lexeme: str     Line#: 5        Column#: 2
Token Type: COLON       Lexeme: :       Line#: 5        Column#: 6
Token Type: STRINGTOK   Lexeme: string  Line#: 5        Column#: 8
Token Type: SEMICOLON   Lexeme: ;       Line#: 5        Column#: 14
Token Type: IDENT       Lexeme: aray    Line#: 6        Column#: 2
Token Type: COLON       Lexeme: :       Line#: 6        Column#: 6
Token Type: ARRAYTOK    Lexeme: array   Line#: 6        Column#: 8
Token Type: LBRACK      Lexeme: [       Line#: 6        Column#: 14
Token Type: MINUS       Lexeme: -       Line#: 6        Column#: 15
Token Type: INTCONST    Lexeme: 1       Line#: 6        Column#: 16
Token Type: RANGE       Lexeme: ..      Line#: 6        Column#: 17
Token Type: INTCONST    Lexeme: 8       Line#: 6        Column#: 19
Token Type: RBRACK      Lexeme: ]       Line#: 6        Column#: 20
Token Type: OFTOK       Lexeme: of      Line#: 6        Column#: 22
Token Type: INTTOK      Lexeme: int     Line#: 6        Column#: 25
Token Type: SEMICOLON   Lexeme: ;       Line#: 6        Column#: 28
Token Type: BEGINTOK    Lexeme: begin   Line#: 9        Column#: 1
Token Type: IDENT       Lexeme: num1    Line#: 10       Column#: 2
Token Type: ASSIGN      Lexeme: :=      Line#: 10       Column#: 7
Token Type: INTCONST    Lexeme: 3       Line#: 10       Column#: 10
Token Type: SEMICOLON   Lexeme: ;       Line#: 10       Column#: 11
Token Type: IDENT       Lexeme: aray    Line#: 11       Column#: 2
Token Type: LBRACK      Lexeme: [       Line#: 11       Column#: 6
Token Type: INTCONST    Lexeme: 1       Line#: 11       Column#: 7
Token Type: RBRACK      Lexeme: ]       Line#: 11       Column#: 8
Token Type: ASSIGN      Lexeme: :=      Line#: 11       Column#: 10
Token Type: IDENT       Lexeme: num1    Line#: 11       Column#: 13
Token Type: SEMICOLON   Lexeme: ;       Line#: 11       Column#: 17
Token Type: IDENT       Lexeme: num2    Line#: 12       Column#: 2
Token Type: ASSIGN      Lexeme: :=      Line#: 12       Column#: 7
Token Type: MINUS       Lexeme: -       Line#: 12       Column#: 10
Token Type: INTCONST    Lexeme: 1       Line#: 12       Column#: 11
Token Type: SEMICOLON   Lexeme: ;       Line#: 12       Column#: 12
Token Type: WHILETOK    Lexeme: while   Line#: 13       Column#: 2
Token Type: LPAREN      Lexeme: (       Line#: 13       Column#: 8
Token Type: IDENT       Lexeme: num2    Line#: 13       Column#: 9
Token Type: LEQ         Lexeme: <=      Line#: 13       Column#: 14
Token Type: INTCONST    Lexeme: 8       Line#: 13       Column#: 17
Token Type: RPAREN      Lexeme: )       Line#: 13       Column#: 18
Token Type: DOTOK       Lexeme: do      Line#: 13       Column#: 20
Token Type: BEGINTOK    Lexeme: begin   Line#: 14       Column#: 3
Token Type: IDENT       Lexeme: aray    Line#: 15       Column#: 4
Token Type: LBRACK      Lexeme: [       Line#: 15       Column#: 8
Token Type: IDENT       Lexeme: num2    Line#: 15       Column#: 9
Token Type: RBRACK      Lexeme: ]       Line#: 15       Column#: 13
Token Type: EQL         Lexeme: =       Line#: 15       Column#: 15
Token Type: IDENT       Lexeme: num2    Line#: 15       Column#: 17
Token Type: SEMICOLON   Lexeme: ;       Line#: 15       Column#: 21
Token Type: IDENT       Lexeme: num2    Line#: 16       Column#: 4
Token Type: ASSIGN      Lexeme: :=      Line#: 16       Column#: 9
Token Type: IDENT       Lexeme: num2    Line#: 16       Column#: 12
Token Type: PLUS        Lexeme: +       Line#: 16       Column#: 17
Token Type: ILLEGAL     Lexeme: 1       Line#: 16       Column#: 19
Token Type: ENDTOK      Lexeme: end     Line#: 17       Column#: 3
Token Type: SEMICOLON   Lexeme: ;       Line#: 17       Column#: 6
Token Type: WRITETOK    Lexeme: write   Line#: 18       Column#: 2
Token Type: LPAREN      Lexeme: (       Line#: 18       Column#: 8
Token Type: IDENT       Lexeme: num2    Line#: 18       Column#: 9
Token Type: RPAREN      Lexeme: )       Line#: 18       Column#: 13
Token Type: SEMICOLON   Lexeme: ;       Line#: 18       Column#: 14
Token Type: READTOK     Lexeme: read    Line#: 19       Column#: 2
Token Type: LPAREN      Lexeme: (       Line#: 19       Column#: 7
Token Type: IDENT       Lexeme: num3    Line#: 19       Column#: 8
Token Type: RPAREN      Lexeme: )       Line#: 19       Column#: 12
Token Type: ENDTOK      Lexeme: end     Line#: 20       Column#: 1
Token Type: DOT         Lexeme: .       Line#: 20       Column#: 4
Token Type: EOFTOK      Lexeme:         Line#: 20       Column#: 0
```

### Parser

I created a recursive descent parser that verifies that the program is within the language.
This uses the scanner to get one token at a time until the end of the program or until an error is found.
If there is an error the parser outputs an error file with the line, column, token, and type along with other useful information.


### Symbol Table

I implemented the [symbol table](https://en.wikipedia.org/wiki/Symbol_table) as a hashtable with the key as the identifier and the value containing information about the symbol.
As the program is parsed, symbols are added to the table such as variables, procedures, and temporary variables for string constants and intermediate values.

### Optimizations

I added [constant folding](https://en.wikipedia.org/wiki/Constant_folding) to optimize expressions that can be solved at compile time and stored as a value saving computation at runtime.

### Assembly Output

This is all translated into assembly as output.

#### NASM

I output assembly to be compiled and linked with [NASM](https://en.wikipedia.org/wiki/Netwide_Assembler).
The program can then be executed.




