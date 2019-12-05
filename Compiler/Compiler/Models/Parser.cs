using System;
using System.Collections;
using System.Collections.Generic;
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
        private Stack<string> _stack;
        private Stack<int> _scopeStack;
        private int _scope = 0;
        public Hashtable SymbolTable { get; set; }
        public Scanner.Token CurrentToken { get; set; }
        public Scanner.Token NextToken { get; set; }
        public struct Symbol
        {
            public string Identifier;
            public string Type;
            public Stack<int> Scope;
            public List<Tuple<string, string>> Dimensions;
            public string Store;
            public List<string> ParameterType;
            public List<string> PassByType;
            public int Column;
            public int Line;
            public override string ToString()
            {
                StringBuilder output = new StringBuilder();
                output.Append("Symbol: " + Identifier);
                output.Append("\tType: " + Type);
                output.Append("\tStore: " + Store);
                output.Append("\tLine: " + Line);
                output.Append("\tColumn: " + Column);
                if (ParameterType != null && ParameterType.Count == PassByType.Count)
                {
                    output.Append("\tParameters: " + ParameterType.Count + " (");
                    for (int i = 0; i < ParameterType.Count; i++)
                    {
                        output.Append(ParameterType[i] + "-" + PassByType[i] + ",");
                    }
                    output.Length--;
                    output.Append(")");
                }
                output.Append("\tScopes: ");
                foreach (var s in Scope)
                {
                    output.Append(s + ",");
                }
                output.Length--;

                if (Dimensions != null)
                {
                    output.Append("\tNumber of Dimensions: " + Dimensions.Count + " (");
                    foreach(Tuple<string, string> dim in Dimensions)
                    {
                        output.Append(dim.Item1 + ".." + dim.Item2 + ",");
                    }
                    // remove the last comma
                    output.Length--;
                    output.Append(")");
                }
                return output.ToString();
            }
        }

        public Parser(Scanner scanner, string path = "")
        {
            _scanner = scanner;
            _path = path;
            _stack = new Stack<string>();
            _scopeStack = new Stack<int>();
            SymbolTable = new Hashtable();
            Program();
            _errorFile.Close();
            _assemblyFile.Close();
        }

        public void PrintSymbolTable()
        {
            foreach (Symbol val in SymbolTable.Values)
            {
                Console.WriteLine(val.ToString());
            }
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

        public bool TransferStackToTable()
        {
            if (_stack.Count > 0)
            {
                var type = _stack.Pop();
                if (type == Scanner.Type.ARRAYTOK.ToString())
                {
                    type = _stack.Pop();
                    var count = _stack.Count;
                    string upperBound = null;
                    string identifier = null;
                    var dimensions = new List<Tuple<string, string>>();

                    for (int i = 0; i < count; i++)
                    {
                        if (i == count - 1)
                        {
                            identifier = _stack.Pop();
                            break;
                        }
                        if (upperBound == null)
                        {
                            upperBound = _stack.Pop();
                        }
                        else
                        {
                            dimensions.Add(Tuple.Create(_stack.Pop(), upperBound));
                            upperBound = null;
                        }
                    }
                    Symbol symbol = new Symbol()
                    {
                        Type = type,
                        Identifier = identifier,
                        Scope = new Stack<int>(),
                        Dimensions = dimensions,
                        Store = "array"
                    };
                    symbol.Scope.Push(_scope);
                    SymbolTable.Add(identifier, symbol);
                }
                else
                {
                    var count = _stack.Count;
                    for (int i = 0; i < count; i++)
                    {
                        string identifier = _stack.Pop();

                        if (SymbolTable.Contains(identifier))
                        {
                            Symbol symbol = (Symbol)SymbolTable[identifier];
                            if (symbol.Scope.Peek() != _scope)
                            {
                                symbol.Scope.Push(_scope);
                            }
                            else
                            {
                                WriteError("Declared the same veriable multiple times in the same scope");
                                Console.WriteLine("Declared the same veriable multiple times in the same scope");
                                return false;
                            }
                        }
                        else
                        {
                            Symbol symbol = new Symbol()
                            {
                                Identifier = identifier.ToString(),
                                Type = type.ToString(),
                                Scope = new Stack<int>(),
                                Store = "scalar"
                            };
                            symbol.Scope.Push(_scope);
                            SymbolTable.Add(identifier.ToString(), symbol);
                        }
                    }
                }
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
            GetNextToken();
            if (!Block())
            {
                Console.WriteLine("Error in Block");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.DOT)
            {
                WriteError("Didn't end program with dot");
                return false;
            }
            Console.WriteLine("Finished Parsing Program");
            return true;
        }

        // <block> ::= <variable declaration part>
        //             <procedure declaration part>
        //             <statement part>
        public bool Block()
        {
            _scope++;
            _scopeStack.Push(_scope);
            if (CurrentToken.Type == Scanner.Type.VARTOK)
            {
                if (!VariableDeclarationSection())
                {
                    WriteError("VariableDeclarationSection");
                    return false;
                }
            }
            if (CurrentToken.Type == Scanner.Type.PROCEDURE)
            {
                if (!ProcedureDeclarationSection())
                {
                    WriteError("ProcedureDeclarationSection");
                    return false;
                }
            }
            if (CurrentToken.Type == Scanner.Type.BEGINTOK)
            {
                if (!StatementSection())
                {
                    WriteError("StatementSection");
                    return false;
                }
            }
            _scopeStack.Pop();
            return true;
        }

        #region Variable Section
        // <variable declaration section> ::= var <variable declaration> ; <more vars> | <empty-string>
        public bool VariableDeclarationSection()
        {
            if (CurrentToken.Type != Scanner.Type.VARTOK)
            {
                // var section is optional
                return true;
            }
            else
            {
                GetNextToken();
                if (!VariableDeclaration())
                {
                    WriteError("Variable Declaration returned false");
                    return false;
                }
                if (CurrentToken.Type != Scanner.Type.SEMICOLON)
                {
                    WriteError("Didn't end variable declaration in semi colon");
                    return false;
                }
                else
                {
                    GetNextToken();
                    if (CurrentToken.Type == Scanner.Type.IDENT)
                    {
                        if (!MoreVariables())
                        {
                            WriteError("MoreVariables");
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        // <empty-string>
                        return true;
                    }
                }
            }
        }

        // <more vars> ::= <variable declaration> ; <more vars> | <empty-string>
        public bool MoreVariables()
        {
            if (!VariableDeclaration())
            {
                WriteError("Variable Declaration");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.SEMICOLON)
            {
                WriteError("Didn't end variable declaration with a semi colon");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type == Scanner.Type.IDENT)
            {
                if (!MoreVariables())
                {
                    WriteError("More variables");
                    return false;
                }
                return true;
            }
            // <empty-string>
            return true;
        }

        // <variable declaration> ::= <identifier> <more decls>
        public bool VariableDeclaration()
        {
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Variable Declaration didn't contain an identifier");
                return false;
            }
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (!MoreDeclarations())
            {
                WriteError("MoreDeclarations");
                return false;
            }
            if (!TransferStackToTable())
            {
                WriteError("Failed to put variables in symbol table");
                return false;
            }

            return true;
        }

        // <more decls>	::=	: <type> | , <variable declaration>
        public bool MoreDeclarations()
        {
            if (CurrentToken.Type == Scanner.Type.COLON)
            {
                GetNextToken();
                if (!Type())
                {
                    WriteError("Type returned with error");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.COMMA)
            {
                GetNextToken();
                if (!VariableDeclaration())
                {
                    WriteError("Variable Declaration returned an error");
                    return false;
                }
                return true;
            }
            WriteError("More Declarations didn't find a colon or comma");
            return false;
        }

        // <type> ::= <simple type> | <array type>
        public bool Type()
        {
            if (CurrentToken.Type == Scanner.Type.ARRAYTOK)
            {
                if (!ArrayType())
                {
                    WriteError("ArrayType");
                    return false;
                }
                _stack.Push(Scanner.Type.ARRAYTOK.ToString());
                return true;
            }
            else
            {
                if (!SimpleType())
                {
                    WriteError("SimpleType");
                    return false;
                }
                return true;
            }
        }

        // <array type> ::= array [ <index range> of <simple type>  
        public bool ArrayType()
        {
            if (CurrentToken.Type != Scanner.Type.ARRAYTOK)
            {
                WriteError("missing array keyword");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LBRACK)
            {
                WriteError("Missing left square bracket");
                return false;
            }
            GetNextToken();
            if (!IndexRange())
            {
                WriteError("Index range returned an error");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.OFTOK)
            {
                WriteError("Missing of keyword after array[]");
                return false;
            }
            GetNextToken();
            if (!SimpleType())
            {
                WriteError("Error in simple type for array");
                return false;
            }
            return true;
        }

        // <index range> ::= <integer constant> . . <integer constant> <index list>
        public bool IndexRange()
        {
            if (CurrentToken.Type != Scanner.Type.INTCONST)
            {
                WriteError("Missing first integer for index range of array");
                return false;
            }
            // lowerbound
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.RANGE)
            {
                WriteError("Missing range identifier .. in array");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.INTCONST)
            {
                WriteError("Missing last integer for index range of array");
                return false;
            }
            // upperbound
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (!IndexList())
            {
                WriteError("Index list for array returned error");
                return false;
            }
            return true;
        }

        // <index list>	::=	, <integer constant> . . <integer constant> <index list> | ]
        public bool IndexList()
        {
            if (CurrentToken.Type == Scanner.Type.RBRACK)
            {
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type != Scanner.Type.COMMA)
            {
                WriteError("Missing comma in index list");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.INTCONST)
            {
                WriteError("Missing int constant in index list");
                return false;
            }
            // lowerbound
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.RANGE)
            {
                WriteError("Missing range identifier .. in array");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.INTCONST)
            {
                WriteError("Missing int constant in index list");
                return false;
            }
            // upperbound
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (!IndexList())
            {
                WriteError("Nested index List returned false");
                return false;
            }
            return true;
        }

        // <simple type> ::= <type identifier>
        // <type identifier> ::= int | boolean | string
        public bool SimpleType()
        {
            if (CurrentToken.Type == Scanner.Type.INTTOK)
            {
                _stack.Push(CurrentToken.Type.ToString());
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.BOOLTOK)
            {
                _stack.Push(CurrentToken.Type.ToString());
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.STRINGTOK)
            {
                _stack.Push(CurrentToken.Type.ToString());
                GetNextToken();
                return true;
            }
            WriteError("Missing simple type");
            return false;
        }
        #endregion

        #region Procedure Section
        // <procedure declaration section> ::=	<procedure declaration> ; <procedure declaration section> | <empty-string>
        public bool ProcedureDeclarationSection()
        {
            // <empty-string>
            if (CurrentToken.Type != Scanner.Type.PROCEDURE)
            {
                return true;
            }
            if (!ProcedureDeclaration())
            {
                WriteError("ProcedureDeclaration");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.SEMICOLON)
            {
                WriteError("Missing semi colon after procedure declaration");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type == Scanner.Type.PROCEDURE)
            {
                if (!ProcedureDeclarationSection())
                {
                    WriteError("ProcedureDeclarationSection");
                    return false;
                }
            }
            // <empty-string>
            return true;
        }
        // <procedure declaration> ::= procedure <identifier> ( <parameter list> ; <block>
        public bool ProcedureDeclaration()
        {
            if (CurrentToken.Type != Scanner.Type.PROCEDURE)
            {
                WriteError("Missing procedure keyword");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Missing procedure identifier");
                return false;
            }
            var procIdentifier = CurrentToken.Lexeme;
            int col = CurrentToken.Column;
            int line = CurrentToken.Line;
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LPAREN)
            {
                WriteError("Missing Left Parenthesis after procedure identifier");
                return false;
            }
            GetNextToken();
            if (!ParameterList())
            {
                WriteError("ParameterList");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.SEMICOLON)
            {
                WriteError("Missing semi colon after parameter list ()");
                return false;
            }
            // have them all on the stack here ------------------------------------------------
            List<string> parameterType = new List<string>();
            List<string> passByType = new List<string>();
            string store = null;
            if (_stack.Count > 0)
            {
                int count = _stack.Count;
                int i = 0;
                while(i < count)
                {
                    string identifier = _stack.Pop();
                    i++;
                    if (_stack.Peek() == "*")
                    {
                        passByType.Add("ref");
                        store = "rparam";
                        _stack.Pop();
                        i++;
                    }
                    else
                    {
                        passByType.Add("val");
                        store = "vparam";
                    }
                    string type = _stack.Pop();
                    i++;
                    parameterType.Add(type);
                    Symbol symbol = new Symbol()
                    {
                        Identifier = identifier,
                        Type = type,
                        Store = store,
                        Scope = new Stack<int>()
                    };
                    symbol.Scope.Push(_scope);
                    SymbolTable.Add(identifier, symbol);
                }
            }
            Symbol procSymbol = new Symbol()
            {
                Identifier = procIdentifier,
                Type = "none",
                Store = "proc",
                Scope = new Stack<int>(),
                Column = col,
                Line = line,
                ParameterType = parameterType,
                PassByType = passByType
            };
            procSymbol.Scope.Push(_scope);
            SymbolTable.Add(procIdentifier, procSymbol);
            GetNextToken();
            if (!Block())
            {
                WriteError("Block in procedure " + procIdentifier + "returned error");
                return false;
            }
            return true;
        }

        // <parameter list>	::=	<type identifier> <param passing> | )
        // <simple type> was combined with <type identifier>
        public bool ParameterList()
        {
            if (CurrentToken.Type == Scanner.Type.RPAREN)
            {
                GetNextToken();
                return true;
            }
            if (!SimpleType())
            {
                WriteError("SimpleType");
                return false;
            }
            if (!ParameterPassing())
            {
                WriteError("ParameterPassing");
                return false;
            }
            return true;
        }

        // <param passing> ::= <pass by value> | * <pass by reference>
        public bool ParameterPassing()
        {
            if (CurrentToken.Type == Scanner.Type.ASTRSK)
            {
                _stack.Push(CurrentToken.Lexeme);
                GetNextToken();
                if (!PassByReference())
                {
                    WriteError("PassByReference");
                    return false;
                }
                return true;
            }
            if (!PassByValue())
            {
                WriteError("PassByValue");
                return false;
            }
            return true;
        }

        // <pass by value> ::= <identifier> <more params>
        public bool PassByValue()
        {
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Missing Identifier in pass by value parameter");
                return false;
            }
            // push onto the stack
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();
            if (!MoreParameters())
            {
                WriteError("MoreParameters");
                return false;
            }
            return true;
        }

        // <pass by reference>	::=	<identifier> <more params>
        public bool PassByReference()
        {
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Missing Identifier in pass by reference parameter");
                return false;
            }
            _stack.Push(CurrentToken.Lexeme);
            GetNextToken();  
            if (!MoreParameters())
            {
                WriteError("MoreParameters");
                return false;
            }
            return true;
        }

        // <more params> ::= , <type identifier> <param passing> | )
        // <simple type> was combined with <type identifier>
        public bool MoreParameters()
        {
            if (CurrentToken.Type == Scanner.Type.RPAREN)
            {
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type != Scanner.Type.COMMA)
            {
                WriteError("Missing comma inbetween paremeters");
                return false;
            }
            GetNextToken();
            if (!SimpleType())
            {
                WriteError("SimpleType");
                return false;
            }
            if (!ParameterPassing())
            {
                WriteError("ParameterPassing");
                return false;
            }
            return true;
        }
        #endregion

        #region Statement Section
        // <statement part> ::= <compound statement>
        public bool StatementSection()
        {
            if (!CompoundStatement())
            {
                WriteError("CompoundStatement");
                return false;
            }
            return true;
        }

        // <compound statement> ::= begin<statement> <more stmts> end
        // NOTE: The final statement before an END is not terminated by a semicolon.
        public bool CompoundStatement()
        {
            if (CurrentToken.Type != Scanner.Type.BEGINTOK)
            {
                WriteError("Compound Statement needs to start with begin");
                return false;
            }
            GetNextToken();
            if (!Statement())
            {
                WriteError("Statement");
                return false;
            }
            if (!MoreStatements())
            {
                WriteError("MoreStatements");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.ENDTOK)
            {
                WriteError("Compound Statement needs to end with keyword end");
                return false;
            }
            GetNextToken();
            return true;
        }

        // <more stmts> ::= ; <statement> <more stmts> | <empty-string>
        public bool MoreStatements()
        {
            if (CurrentToken.Type != Scanner.Type.SEMICOLON)
            {
                // <empty-string>
                return true;
            }
            GetNextToken();
            if (!Statement())
            {
                WriteError("Statement");
                return false;
            }
            if (!MoreStatements())
            {
                WriteError("MoreStatements");
                return false;
            }
            return true;
        }

        // <statement>	::=	<simple statement>  | <structured statement>
        public bool Statement()
        {
            switch (CurrentToken.Type)
            {

                case Scanner.Type.IDENT:
                    if (NextToken.Type == Scanner.Type.ASSIGN || NextToken.Type == Scanner.Type.LPAREN)
                    {
                        if (!SimpleStatement())
                        {
                            WriteError("SimpleStatement");
                            return false;
                        }
                        return true;
                    }
                    return false;
                case Scanner.Type.READTOK:
                case Scanner.Type.WRITETOK:
                    if (!SimpleStatement())
                    {
                        WriteError("SimpleStatement");
                        return false;
                    }
                    return true;
                case Scanner.Type.BEGINTOK:
                case Scanner.Type.IFTOK:
                case Scanner.Type.SWITCHTOK:
                case Scanner.Type.WHILETOK:
                    if (!StructuredStatement())
                    {
                        WriteError("StructuredStatement");
                        return false;
                    }
                    return true;
                default:
                    WriteError("Invalid Statement");
                    return false;
            }
        }

        // <simple statement> ::= <assignment statement> | <procedure call> | <read statement> | <write statement>
        public bool SimpleStatement()
        {
            if (NextToken.Type == Scanner.Type.ASSIGN)
            {
                if (!AssignmentStatement())
                {
                    WriteError("AssignmentStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.IDENT && NextToken.Type == Scanner.Type.LPAREN)
            {
                if (!ProcedureCall())
                {
                    WriteError("ProcedureCall");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.READTOK)
            {
                if (!ReadStatement())
                {
                    WriteError("ReadStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.WRITETOK)
            {
                if (!WriteStatement())
                {
                    WriteError("WriteStatement");
                    return false;
                }
                return true;
            }
            WriteError("Invalid simple statement");
            return false;
        }

        // <assignment statement> ::= <variable> := <expression>
        public bool AssignmentStatement()
        {
            if (!Variable())
            {
                WriteError("Variable");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.ASSIGN)
            {
                WriteError("Missing assign := operation");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            return true;
        }

        // <procedure call>	::=	<procedure identifier> ( <arg list>
        // <procedure identifier>	::=	<identifier>
        public bool ProcedureCall()
        {
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Missing procedure identifier in procedure call");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LPAREN)
            {
                WriteError("Missing left parenthesis in procedure call");
                return false;
            }
            GetNextToken();
            if (!ArgumentList())
            {
                WriteError("ArgumentList");
                return false;
            }
            return true;
        }

        // <arg list> ::= <expression> <more args> | )
        public bool ArgumentList()
        {
            if (CurrentToken.Type == Scanner.Type.RPAREN)
            {
                GetNextToken();
                return true;
            }
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (!MoreArguments())
            {
                WriteError("MoreArguments");
                return false;
            }
            return true;
        }

        // <more args> ::= , <expression> <more args> | )
        public bool MoreArguments()
        {
            if (CurrentToken.Type == Scanner.Type.RPAREN)
            {
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type != Scanner.Type.COMMA)
            {
                WriteError("Missing comma in argument list");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (!MoreArguments())
            {
                WriteError("MoreArguments");
                return false;
            }
            return true;
        }

        // <read statement>	::=	read ( <variable> )
        public bool ReadStatement()
        {
            if (CurrentToken.Type != Scanner.Type.READTOK)
            {
                WriteError("Missing read keyword");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LPAREN)
            {
                WriteError("Missing left parethesis in read statement");
                return false;
            }
            GetNextToken();
            if (!Variable())
            {
                WriteError("Variable");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.RPAREN)
            {
                WriteError("Missing right parethesis in read statement");
                return false;
            }
            GetNextToken();
            return true;
        }

        // <write statement>	::=	write( <expression> )
        public bool WriteStatement()
        {
            if (CurrentToken.Type != Scanner.Type.WRITETOK)
            {
                WriteError("Missing write keyword");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LPAREN)
            {
                WriteError("Missing left parethesis in write statement");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.RPAREN)
            {
                WriteError("Missing right parethesis in write statement");
                return false;
            }
            GetNextToken();
            return true;
        }

        // <structured statement>	::=	<compound statement>   |
        //                              <if statement>   |
        //                              <case statement>   |
        //                              <while statement>
        public bool StructuredStatement()
        {

            if (CurrentToken.Type == Scanner.Type.BEGINTOK)
            {
                if (!CompoundStatement())
                {
                    WriteError("CompoundStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.IFTOK)
            {
                if (!IfStatement())
                {
                    WriteError("IfStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.SWITCHTOK)
            {
                if (!CaseStatement())
                {
                    WriteError("CaseStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.WHILETOK)
            {
                if (!WhileStatement())
                {
                    WriteError("WhileStatement");
                    return false;
                }
                return true;
            }
            WriteError("Invalid structured statement");
            return false;
        }

        // <if statement> ::= if <expression> then <statement> <else part>
        public bool IfStatement()
        {
            if (CurrentToken.Type != Scanner.Type.IFTOK)
            {
                WriteError("Missing if keyword");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.THENTOK)
            {
                WriteError("Missing then keyword");
                return false;
            }
            GetNextToken();
            if (!Statement())
            {
                WriteError("Statement");
                return false;
            }
            if (!ElsePart())
            {
                WriteError("ElsePart");
                return false;
            }
            return true;
        }

        // <else part> ::= else <statement> | <empty-string>
        public bool ElsePart()
        {
            if (CurrentToken.Type != Scanner.Type.ELSETOK)
            {
                // <empty-string>
                return true;
            }
            GetNextToken();
            if (!Statement())
            {
                WriteError("Statement");
                return false;
            }
            return true;
        }

        // <case statement>	::=	switch ( <variable identifier> ) <case part>
        public bool CaseStatement()
        {
            
            if (CurrentToken.Type != Scanner.Type.SWITCHTOK)
            {
                WriteError("Missing switch keyword");
                return false;
            }
            GetNextToken();
            if (CurrentToken.Type != Scanner.Type.LPAREN)
            {
                WriteError("Missing left parenthesis");
                return false;
            }
            GetNextToken();
            if (!VariableIdentifier())
            {
                WriteError("VariableIdentifier");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.RPAREN)
            {
                WriteError("Missing right parenthesis");
                return false;
            }
            GetNextToken();
            if (!CasePart())
            {
                WriteError("CasePart");
                return false;
            }
            return true;
        }

        // <case part> ::= case <expression> : <compound statement> <case part> | default : <compound statement>
        public bool CasePart()
        {
            if (CurrentToken.Type == Scanner.Type.DEFAULTTOK)
            {
                GetNextToken();
                if (CurrentToken.Type != Scanner.Type.COLON)
                {
                    WriteError("Missing colon after case expression");
                    return false;
                }
                GetNextToken();
                if (!CompoundStatement())
                {
                    WriteError("CompoundStatement");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type != Scanner.Type.CASETOK)
            {
                WriteError("Missing case keyword");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.COLON)
            {
                WriteError("Missing colon after case expression");
                return false;
            }
            GetNextToken();
            if (!CompoundStatement())
            {
                WriteError("CompoundStatement");
                return false;
            }
            if (!CasePart())
            {
                WriteError("CasePart");
                return false;
            }
            return true;
        }

        // <while statement> ::= while <expression> do <compound statement>
        public bool WhileStatement()
        {
            if (CurrentToken.Type != Scanner.Type.WHILETOK)
            {
                WriteError("Missing while keyword");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (CurrentToken.Type != Scanner.Type.DOTOK)
            {
                WriteError("Missing do keyword");
                return false;
            }
            GetNextToken();
            if (!CompoundStatement())
            {
                WriteError("CompoundStatement");
                return false;
            }
            return true;
        }

        // <expression>	::=	<simple expression> <rel exp>
        public bool Expression()
        {
            if (!SimpleExpression())
            {
                WriteError("SimpleExpression");
                return false;
            }
            if (!RelationalExpression())
            {
                WriteError("RelationalExpression");
                return false;
            }
            return true;
        }
        // <rel exp> ::= <rel op> <simple expression> | <empty-string>
        public bool RelationalExpression()
        {
            // <empty-string>
            switch (CurrentToken.Type)
            {
                case Scanner.Type.EQL:
                case Scanner.Type.NEQ:
                case Scanner.Type.LESS:
                case Scanner.Type.LEQ:
                case Scanner.Type.GEQ:
                case Scanner.Type.GREATER:
                    break;
                default:
                    return true;
            }
            if (!RelationalOperator())
            {
                WriteError("RelationalOperator");
                return false;
            }
            if (!SimpleExpression())
            {
                WriteError("SimpleExpression");
                return false;
            }
            return true;
        }

        // <simple expression> ::= <sign> <term> <add term>
        public bool SimpleExpression()
        {
            if (!Sign())
            {
                WriteError("Sign");
                return false;
            }
            if (!Term())
            {
                WriteError("Term");
                return false;
            }
            if (!AddTerm())
            {
                WriteError("AddTerm");
                return false;
            }
            return true;
        }

        // <add term> ::= <add op> <term> <add term> | <empty-string>
        public bool AddTerm()
        {
            // <empty-string>
            switch (CurrentToken.Type)
            {
                case Scanner.Type.PLUS:
                case Scanner.Type.MINUS:
                case Scanner.Type.ORTOK:
                    break;
                default:
                    return true;
            }
            if (!AddOperation())
            {
                WriteError("AddOperation");
                return false;
            }
            if (!Term())
            {
                WriteError("Term");
                return false;
            }
            if (!AddTerm())
            {
                WriteError("AddTerm");
                return false;
            }
            return true;
        }

        // <term> ::= <factor> <mul factor>
        public bool Term()
        {
            if (!Factor())
            {
                WriteError("Factor");
                return false;
            }
            if (!MultiplyFactor())
            {
                WriteError("MultiplyFactor");
                return false;
            }
            return true;
        }

        // <mul factor>	::=	<mul op> <factor> <mul factor> | <empty-string>
        public bool MultiplyFactor()
        {
            // <empty-string>
            switch (CurrentToken.Type)
            {
                case Scanner.Type.ASTRSK:
                case Scanner.Type.SLASH:
                case Scanner.Type.ANDOP:
                case Scanner.Type.IDENT:
                case Scanner.Type.INTCONST:
                case Scanner.Type.STRCONST:
                case Scanner.Type.LPAREN:
                case Scanner.Type.NOTTOK:
                    break;
                default:
                    return true;
            }
            if (!MultiplyOperation())
            {
                WriteError("MultiplyOperation");
                return false;
            }
            if (!Factor())
            {
                WriteError("Factor");
                return false;
            }
            if (!MultiplyFactor())
            {
                WriteError("MultiplyFactor");
                return false;
            }
            return true;
        }

        // <factor>	::=	<variable> | <constant> | (   <expression>   ) | not<factor>
        public bool Factor()
        {
            // handle or's
            if (CurrentToken.Type == Scanner.Type.IDENT)
            {
                if (!Variable())
                {
                    WriteError("Variable");
                    return false;
                }
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.INTCONST)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.STRCONST)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.FALSETOK)
            {
                Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.TRUETOK)
            {
                Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.LPAREN)
            {
                GetNextToken();
                if (!Expression())
                {
                    WriteError("Expression");
                    return false;
                }
                if (CurrentToken.Type != Scanner.Type.RPAREN)
                {
                    WriteError("Missing right parenthesis");
                    return false;
                }
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.NOTTOK)
            {
                GetNextToken();
                if (!Factor())
                {
                    WriteError("Not factor returned an error");
                    return false;
                }
                return true;
            }
            WriteError("Not a valid factor");
            return false;
        }

        // <rel op>	::=	= | <> | < | <= | >= | >
        public bool RelationalOperator()
        {
            
            if (CurrentToken.Type == Scanner.Type.EQL)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.NEQ)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.LESS)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.LEQ)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.GEQ)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.GREATER)
            {
                Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            WriteError("Missing relational operator");
            return false;
        }

        // <sign> ::= + | - | <empty-string>
        public bool Sign()
        {
            if (CurrentToken.Type == Scanner.Type.PLUS)
            {
                Console.WriteLine(CurrentToken.Lexeme + " sign");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.MINUS)
            {
                Console.WriteLine(CurrentToken.Lexeme + " sign");
                GetNextToken();
                return true;
            }
            // <empty-string>
            return true;
        }

        // <add op>	::=	+ | - | or
        public bool AddOperation()
        {
            if (CurrentToken.Type == Scanner.Type.PLUS)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.MINUS)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.ORTOK)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            WriteError("Missing operation");
            return false;
        }

        // <mul op>	::=	* | / | and
        public bool MultiplyOperation()
        {
            if (CurrentToken.Type == Scanner.Type.ASTRSK)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.SLASH)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.ANDOP)
            {
                Console.WriteLine(CurrentToken.Lexeme + " operation");
                GetNextToken();
                return true;
            }
            WriteError("Missing operation");
            return false;
        }

        // <variable> ::= <variable identifier> <indexed var>
        public bool Variable()
        {
            if (!VariableIdentifier())
            {
                WriteError("VariableIdentifier");
                return false;
            }
            if (!IndexedVariable())
            {
                WriteError("IndexedVariable");
                return false;
            }
            return true;
        }

        // <indexed var> ::=	[ <expression> <array idx> | <empty-string>
        public bool IndexedVariable()
        {
            if (CurrentToken.Type != Scanner.Type.LBRACK)
            {
                // <empty-string>
                return true;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (!ArrayIndex())
            {
                WriteError("ArrayIndex");
                return false;
            }
            return true;
        }

        // <array idx> ::= , <expression> <array idx> | ]
        public bool ArrayIndex()
        {
            if (CurrentToken.Type == Scanner.Type.RBRACK)
            {
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type != Scanner.Type.COMMA)
            {
                WriteError("Missing comma in array index");
                return false;
            }
            GetNextToken();
            if (!Expression())
            {
                WriteError("Expression");
                return false;
            }
            if (!ArrayIndex())
            {
                WriteError("ArrayIndex");
                return false;
            }
            return true;
        }

        // <variable identifier> ::= <identifier>
        public bool VariableIdentifier()
        {
            if (CurrentToken.Type != Scanner.Type.IDENT)
            {
                WriteError("Identifier not found");
                return false;
            }
            Console.WriteLine(CurrentToken.Lexeme + " Identifier to be put into symbol table ");
            GetNextToken();
            return true;
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
