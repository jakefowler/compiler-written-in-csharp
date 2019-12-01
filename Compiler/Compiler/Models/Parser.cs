using System;
using System.IO;
using System.Text;

namespace Compiler.Models
{
    class Parser
    {
        private readonly Scanner _scanner;
        private StreamWriter _assemblyFile;
        private StreamWriter _errorFile;
        private readonly string _path;
        public Scanner.Token CurrentToken { get; set; }
        public Scanner.Token NextToken { get; set; }
        public Parser(Scanner scanner, string path = "")
        {
            _scanner = scanner;
            _path = path;
            Program();
        }

        public bool GetNextToken()
        {
            CurrentToken = NextToken;
            NextToken = _scanner.GetNextToken();
            return CurrentToken.Type != Scanner.Type.EOFTOK;
        }

        private bool SetupFiles(string programIdentifier)
        {
            try
            {
                string fullPath = _path + programIdentifier;
                _assemblyFile = new StreamWriter(fullPath + ".asm");
                _errorFile = new StreamWriter(fullPath + ".err");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return true;
        }

        public void WriteError(string message)
        {
            _errorFile.WriteLine("Error: " + message + ". Occured at Line: " + CurrentToken.Line + " Column: " + CurrentToken.Column);
        }

        // <program> ::= program <identifier> ; <block> .
        public bool Program()
        {
            string programIdentifier;
            GetNextToken();
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.PROGRAM)
            {
                Console.WriteLine("Program needs to start with program token");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                Console.WriteLine("Didn't provide provide identifier for program.");
                return false;
            }
            programIdentifier = CurrentToken.Lexeme;
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.SEMICOLON)
            {
                Console.WriteLine("Missing semicolon after program identifier");
                return false;
            }
            SetupFiles(programIdentifier);
            GetNextToken();
            if (!Block())
            {
                Console.WriteLine("Error in Block");
            }
            if (CurrentToken.Type != Scanner.Type.DOT)
            {
                WriteError("Didn't end program with dot");
                return false;
            }
            return true;
        }

        // <block> ::= <variable declaration part>
        //             <procedure declaration part>
        //             <statement part>
        public bool Block()
        {
            if (CurrentToken.Type == Scanner.Type.VARTOK)
            {
                if (!VariableDeclarationSection())
                {
                    return false;
                }
            }
            if (CurrentToken.Type == Scanner.Type.PROCEDURE)
            {
                if (!ProcedureDeclarationSection())
                {
                    return false;
                }
            }
            if (CurrentToken.Type == Scanner.Type.BEGINTOK)
            {
                if (!Statement())
                {
                    return false;
                }
            }
            return true;
        }

        #region Variable Section
        // <variable declaration section> ::= var <variable declaration> ; <more vars> | <empty-string>
        public bool VariableDeclarationSection()
        {
            return false;
        }

        // <more vars> ::= <variable declaration> ; <more vars> | <empty-string>
        public bool MoreVariables()
        {
            return false;
        }

        // <variable declaration> ::= <identifier> <more decls>
        public bool VariableDeclaration()
        {
            return false;
        }

        // <more decls>	::=	: <type> | , <variable declaration>
        public bool MoreDeclarations()
        {
            return false;
        }

        // <type> ::= <simple type> | <array type>
        public bool Type()
        {
            return false;
        }

        // <array type> ::= array [ <index range> of <simple type>  
        public bool ArrayType()
        {
            return false;
        }

        // <index range> ::= <integer constant> . . <integer constant> <index list>
        public bool IndexRange()
        {
            return false;
        }

        // <index list>	::=	, <integer constant> . . <integer constant> <index list> | ]
        public bool IndexList()
        {
            return false;
        }

        // <simple type> ::= <type identifier>
        // <type identifier> ::= int | boolean | string
        public bool SimpleType()
        {
            return false;
        }
        #endregion

        #region Procedure Section
        // <procedure declaration section> ::=	<procedure declaration> ; <procedure declaration part> | <empty-string>
        public bool ProcedureDeclarationSection()
        {
            return false;
        }
        // <procedure declaration> ::= procedure <identifier> ( <parameter list> ; <block>
        public bool ProcedureDeclaration()
        {
            return false;
        }
        // <parameter list>	::=	<type identifier> <param passing> | )
        public bool ParameterList()
        {
            return false;
        }

        // <param passing> ::= <pass by value> | * <pass by reference>
        public bool ParameterPassing()
        {
            return false;
        }

        //<pass by value> ::= <identifier> <more params>
        public bool PassByValue()
        {
            return false;
        }

        //<pass by reference>	::=	<identifier> <more params>
        public bool PassByReference()
        {
            return false;
        }

        //<more params> ::= , <type identifier> <param passing> | )
        public bool MoreParameters()
        {
            return false;
        }
        #endregion

        #region Statement Section
        //<statement part> ::= <compound statement>
        //<compound statement> ::= begin<statement> <more stmts> end
        // NOTE: The final statement before an END is not terminated by a semicolon.
        public bool StatementSection()
        {
            return false;
        }
        //<more stmts> ::= ; <statement> <more stmts> | <empty-string>
        public bool MoreStatements()
        {
            return false;
        }
        //<statement>	::=	<simple statement>  | <structured statement>
        public bool Statement()
        {
            return false;
        }
        //<simple statement> ::= <assignment statement> | <procedure call> | <read statement> | <write statement>
        public bool SimpleStatement()
        {
            return false;
        }
        //<assignment statement> ::= <variable> := <expression>
        public bool AssignmentStatement()
        {
            return false;
        }

        // <procedure call>	::=	<procedure identifier> ( <arg list>
        // <procedure identifier>	::=	<identifier>
        public bool ProcedureCall()
        {
            return false;
        }

        // <arg list> ::= <expression> <more args> | )
        public bool ArgumentList()
        {
            return false;
        }

        //<more args>	::=	, <expression> <more args> | )
        public bool MoreArguments()
        {
            return false;
        }

        // <read statement>	::=	read ( <variable> )
        public bool ReadStatement()
        {
            return false;
        }

        //<write statement>	::=	write( <expression> )
        public bool WriteStatement()
        {
            return false;
        }

        // <structured statement>	::=	<compound statement>   |
        //                              <if statement>   |
        //                              <case statement>   |
        //                              <while statement>
        public bool StructuredStatement()
        {
            return false;
        }

        // <if statement> ::= if <expression> then <statement> <else part>
        public bool IfStatement()
        {
            return false;
        }

        // <else part> ::= else <statement> | <empty-string>
        public bool ElsePart()
        {
            return false;
        }

        // <case statement>	::=	switch ( <variable identifier> ) <case part>
        public bool CaseStatement()
        {
            return false;
        }

        // <case part> ::= case <expression> : <compound statement> <case part> | default : <compound statement>
        public bool CasePart()
        {
            return false;
        }

        // <while statement> ::= while <expression> do < compound statement>
        public bool WhileStatement()
        {
            return false;
        }

        // <expression>	::=	<simple expression> <rel exp>
        public bool Expression()
        {
            return false;
        }
        //<rel exp> ::= <rel op> <simple expression> | <empty-string>
        public bool RelationalExpression()
        {
            return false;
        }

        // <simple expression> ::= <sign> <term> <add term>
        public bool SimpleExpression()
        {
            return false;
        }

        // <add term> ::= <add op> <term> <add term> | <empty-string>
        public bool AddTerm()
        {
            return false;
        }

        // <term> ::= <factor> <mul factor>
        public bool Term()
        {
            return false;
        }

        // <mul factor>	::=	<mul op> <factor> <mul factor> | <empty-string>
        public bool MultiplyFactor()
        {
            return false;
        }

        // <factor>	::=	<variable> | <constant> | (   <expression>   ) | not<factor>
        public bool Factor()
        {
            return false;
        }

        // <rel op>	::=	= | <> | < | <= | >= | >
        public bool RelationalOperator()
        {
            return false;
        }

        // <sign> ::= + | - | <empty-string>
        public bool Sign()
        {
            return false;
        }

        // <add op>	::=	+ | - | or
        public bool AddOperation()
        {
            return false;
        }

        // <mul op>	::=	* | / | and
        public bool MultiplyOperation()
        {
            return false;
        }

        // <variable> ::= <variable identifier> <indexed var>
        public bool Variable()
        {
            return false;
        }

        // <indexed var> ::=	[ <expression> <array idx> | <empty-string>
        public bool IndexedVariable()
        {
            return false;
        }

        // <array idx> ::= , <expression> <array idx> | ]
        public bool ArrayIndex()
        {
            return false;
        }

        // <variable identifier> ::= <identifier>
        public bool VariableIdentifier()
        {
            return false;
        }
        #endregion

        // Read in the input file.
        // Generate two files.If the program's name (i.e., the identifier contained in the file) is 'xxx' then generate the following files:
        //    xxx.asm - where all the assembly output will be.
        //    xxx.err - where all the errors will be - if there are no errors then the file will be empty.However, there should always be an error file created even if it is empty.
        //    The one exception to generating the two files is when there is an error and the compiler cannot determine the program name.
        // Print out the symbol name (lexeme) and its type.
        // Use the program name to generate the correct file names.
        // Keep track of all identifiers , do not add a symbol already in the symbol table
        // Update symbols with their type as they are declared
        // Implement the assignment operation without expression evaluation (required test files will not contain arithmetic expressions, only constants)
        // Implement arithmetic expression. However, you do need to make sure that any variables used are being declared or have already been declared. You only need to implement the assignment statement, allowing 
        //      for an integer constant to be assigned to an int and a string constant to be assigned to a string. 
    }
}
