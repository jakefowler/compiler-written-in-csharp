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
            if (!Block())
            {
                Console.WriteLine("Error in Block");
            }
            if (CurrentToken.Type != Scanner.Type.DOT)
            {
                _errorFile.WriteLine("Didn't end program with dot");
                return false;
            }
            return true;
        }

        // <block> ::= <variable declaration part>
        //             <procedure declaration part>
        //             <statement part>
        public bool Block()
        {
            return false;
        }

        // <variable declaration part> ::= var <variable declaration> ; <more vars> | <empty-string>
        public bool VariableDeclaration()
        {
            return false;
        }

        // <procedure declaration part> ::=	<procedure declaration> ; <procedure declaration part> | <empty-string>
        // <procedure declaration> ::= procedure <identifier> ( <parameter list> ; <block>
        // <parameter list>	::=	<type identifier> <param passing> | )
        // <param passing> ::= <pass by value> | * <pass by reference>
        public bool Procedure()
        {
            return false;
        }

        //<statement part> ::= <compound statement>
        //<compound statement> ::= begin<statement> <more stmts> end
        // NOTE: The final statement before an END is not terminated by a semicolon.
        //<more stmts> ::= ; <statement> <more stmts> | <empty-string>
        //<statement>	::=	<simple statement>  | <structured statement>
        //<simple statement> ::= <assignment statement> | <procedure call> | <read statement> | <write statement>
        //<assignment statement> ::= <variable> := <expression>
        public bool Statement()
        {
            return false;
        }

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
