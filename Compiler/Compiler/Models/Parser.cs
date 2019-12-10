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
        private Stack<Scanner.Token> _stack;
        private int _scope = 0;
        // used to generate a unique name for temporary values like string constants and expressions
        private int _genStrCounter = 0;
        private int _genIntCounter = 0;
        public Hashtable SymbolTable { get; set; }
        public Scanner.Token CurrentToken { get; set; }
        public Scanner.Token NextToken { get; set; }
        public StringBuilder CodeSectionAsm;
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
            public string Value;
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
                    if (ParameterType.Count > 0)
                    {
                        output.Length--;
                    }
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
                if (Value != null)
                {
                    output.Append("\tValue: " + Value);
                }
                return output.ToString();
            }
        }

        public Parser(Scanner scanner, string path = "")
        {
            _scanner = scanner;
            _path = path;
            _stack = new Stack<Scanner.Token>();
            SymbolTable = new Hashtable();
            CodeSectionAsm = new StringBuilder();
            Program();
            WriteAssembly();
            _errorFile.Close();
            _assemblyFile.Close();
        }

        public void PrintSymbolTable()
        {
            Console.WriteLine("Symbol Table:");
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

        public void WriteAssembly()
        {
            if (_assemblyFile.BaseStream != null)
            {
                WriteExports();
                WriteImports();
                WriteInitializedData();
                WriteUninitializedData();
                WriteAsmCode();
            }
            else
            {
                Console.WriteLine("Assembly file is closed");
            }
        }

        private void WriteHeader(string header)
        {
            _assemblyFile.WriteLine(";-----------------------------");
            _assemblyFile.WriteLine("; " + header);
            _assemblyFile.WriteLine(";-----------------------------");
        }

        private void WriteExports()
        {
            WriteHeader("emports");
            _assemblyFile.WriteLine("global _main");
            _assemblyFile.WriteLine("EXPORT _main");
        }

        private void WriteImports()
        {
            WriteHeader("imports");
            _assemblyFile.WriteLine("extern _printf");
            _assemblyFile.WriteLine("extern _scanf");
            _assemblyFile.WriteLine("extern _ExitProcess@4");
        }

        private void WriteInitializedData()
        {
            WriteHeader("initialized data");
            _assemblyFile.WriteLine("section .data USE32");
            _assemblyFile.WriteLine("\tstringPrinter:\tdb\t\"%s\",0");
            _assemblyFile.WriteLine("\tnumberPrinter:\tdb\t\"%d\",0x0d,0x0a,0");
            _assemblyFile.WriteLine("\tformatIntIn:\tdb\t\"%d\",0");
            _assemblyFile.WriteLine("\tformatStrIn:\tdb\t\"%s\",0");
            foreach(Symbol symbol in SymbolTable.Values)
            {
                if (symbol.Type == "string" && symbol.Value != null)
                {
                    _assemblyFile.WriteLine("\t" + symbol.Identifier + ":\tdb\t" + symbol.Value + ",0x0d,0x0a,0");
                }
            }
        }

        private void WriteUninitializedData()
        {
            WriteHeader("uninitialized data");
            _assemblyFile.WriteLine("section .bss USE32");
            foreach(Symbol symbol in SymbolTable.Values)
            {
                if (symbol.Type == "int")
                {
                    _assemblyFile.WriteLine("\t" + symbol.Identifier + ":\tresd\t1");
                }
                else if (symbol.Type == "string" && symbol.Value == null)
                {
                    _assemblyFile.WriteLine("\t" + symbol.Identifier + ":\tresb\t128");
                }
            }
        }

        private void WriteAsmCode()
        {
            WriteHeader("code");
            _assemblyFile.WriteLine("section .code USE32");
            _assemblyFile.WriteLine("_main:");
            _assemblyFile.Write(CodeSectionAsm);
            _assemblyFile.WriteLine("exit:");
            _assemblyFile.WriteLine("\tmov\teax,\t0x0");
            _assemblyFile.WriteLine("\tcall\t_ExitProcess@4");
        }
        
        public bool TransferStackToTable()
        {
            if (_stack.Count > 0)
            {
                Scanner.Token topToken = _stack.Pop();
                if (topToken.Type == Scanner.Type.ARRAYTOK)
                {
                    string type = _stack.Pop().Lexeme;
                    int count = _stack.Count;
                    string upperBound = null;
                    string identifier = null;
                    var dimensions = new List<Tuple<string, string>>();
                    Scanner.Token token = new Scanner.Token();
                    for (int i = 0; i < count; i++)
                    {
                        if (i == count - 1)
                        {
                            token = _stack.Pop();
                            identifier = token.Lexeme;
                            break;
                        }
                        if (upperBound == null)
                        {
                            upperBound = _stack.Pop().Lexeme;
                        }
                        else
                        {
                            dimensions.Add(Tuple.Create(_stack.Pop().Lexeme, upperBound));
                            upperBound = null;
                        }
                    }
                    Symbol symbol = new Symbol()
                    {
                        Type = type,
                        Identifier = identifier,
                        Scope = new Stack<int>(),
                        Dimensions = dimensions,
                        Store = "array",
                        Line = token.Line,
                        Column = token.Column
                    };
                    symbol.Scope.Push(_scope);
                    SymbolTable.Add(identifier, symbol);
                }
                else
                {
                    var count = _stack.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var token = _stack.Pop();
                        string identifier = token.Lexeme;

                        if (SymbolTable.Contains(identifier))
                        {
                            Symbol symbol = (Symbol)SymbolTable[identifier];
                            if (symbol.Scope.Peek() != _scope)
                            {
                                symbol.Scope.Push(_scope);
                            }
                            else
                            {
                                WriteError("Declared the same veriable multiple times in the same scope -> " + identifier);
                                Console.WriteLine("Declared the same veriable multiple times in the same scope -> " + identifier);
                                return false;
                            }
                        }
                        else
                        {
                            Symbol symbol = new Symbol()
                            {
                                Identifier = identifier,
                                Type = topToken.Lexeme,
                                Scope = new Stack<int>(),
                                Store = "scalar",
                                Line = token.Line,
                                Column = token.Column
                            };
                            symbol.Scope.Push(_scope);
                            SymbolTable.Add(identifier, symbol);
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
            _scope++;
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
            _stack.Push(CurrentToken);
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
                _stack.Push(new Scanner.Token() { Type = Scanner.Type.ARRAYTOK });
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
            _stack.Push(CurrentToken);
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
            _stack.Push(CurrentToken);
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
            _stack.Push(CurrentToken);
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
            _stack.Push(CurrentToken);
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
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.BOOLTOK)
            {
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.STRINGTOK)
            {
                _stack.Push(CurrentToken);
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
            var procIdentToken = CurrentToken;
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
            List<string> parameterType = new List<string>();
            List<string> passByType = new List<string>();
            string store = null;
            if (_stack.Count > 0)
            {
                int count = _stack.Count;
                int i = 0;
                while(i < count)
                {
                    Scanner.Token identToken = _stack.Pop();
                    string identifier = identToken.Lexeme;
                    i++;
                    if (_stack.Peek().Lexeme == "*")
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
                    string type = _stack.Pop().Lexeme;
                    i++;
                    parameterType.Add(type);
                    Symbol symbol = new Symbol()
                    {
                        Identifier = identifier,
                        Type = type,
                        Store = store,
                        Scope = new Stack<int>(),
                        Line = identToken.Line,
                        Column = identToken.Column
                    };
                    symbol.Scope.Push(_scope);
                    if (SymbolTable.Contains(identifier))
                    {
                        Symbol sym = (Symbol)SymbolTable[identifier];
                        if (sym.Type != type)
                        {
                            Console.WriteLine("Error: Duplicate identifier of different type -> " + identifier);
                            WriteError("Duplicate identifier of different type -> " + identifier);
                            return false;
                        }
                    }
                    SymbolTable.Add(identifier, symbol);
                }
            }
            parameterType.Reverse();
            passByType.Reverse();
            Symbol procSymbol = new Symbol()
            {
                Identifier = procIdentToken.Lexeme,
                Type = "none",
                Store = "proc",
                Scope = new Stack<int>(),
                Column = procIdentToken.Column,
                Line = procIdentToken.Line,
                ParameterType = parameterType,
                PassByType = passByType
            };
            procSymbol.Scope.Push(_scope);
            if (SymbolTable.Contains(procIdentToken.Lexeme))
            {
                Console.WriteLine("Error: procedure already declared -> " + procIdentToken.Lexeme);
                WriteError("Error: procedure already declared -> " + procIdentToken.Lexeme);
                return false;
            }
            SymbolTable.Add(procIdentToken.Lexeme, procSymbol);
            GetNextToken();
            if (!Block())
            {
                WriteError("Block in procedure " + procIdentToken.Lexeme + "returned error");
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
                _stack.Push(CurrentToken);
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
            _stack.Push(CurrentToken);
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
            _stack.Push(CurrentToken);
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
            Scanner.Token varToAssign = _stack.Pop();
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
            if (_stack.Count > 1)
            {
                List<Scanner.Token> expression = new List<Scanner.Token>();
                // Constant Folding Optimization
                int stackCount = _stack.Count;
                while (stackCount > 1)
                {
                    var rightTok = _stack.Pop();
                    var opTok = _stack.Pop();
                    var leftTok = _stack.Pop();
                    if (rightTok.Type == Scanner.Type.INTCONST && leftTok.Type == Scanner.Type.INTCONST)
                    {
                        switch (opTok.Lexeme)
                        {
                            case "+":
                                leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) + Int32.Parse(rightTok.Lexeme)).ToString();
                                break;
                            case "-":
                                leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) - Int32.Parse(rightTok.Lexeme)).ToString();
                                break;
                        }
                    }
                    else
                    {
                        expression.Add(rightTok);
                        expression.Add(opTok);
                    }
                    stackCount -= 2;
                    _stack.Push(leftTok);
                }
                expression.Add(_stack.Pop());

                Scanner.Token firstTok = expression[expression.Count - 1];
                string first = firstTok.Type == Scanner.Type.IDENT ? "DWORD[" + firstTok.Lexeme + "]" : firstTok.Lexeme;
                CodeSectionAsm.AppendLine("\tmov\tesi,\t" + first);

                for (int i = expression.Count - 2; i >= 0; i--)
                {
                    Scanner.Token opTok = expression[i];
                    i--;
                    Scanner.Token rightTok = expression[i];
                    Scanner.Token leftTok = expression[i + 2];
                    string right = rightTok.Type == Scanner.Type.IDENT ? "DWORD[" + rightTok.Lexeme + "]" : rightTok.Lexeme;
                    if (opTok.Type == Scanner.Type.MINUS)
                    {
                        CodeSectionAsm.AppendLine("\tsub\tesi,\t" + right);
                    }
                    else if (opTok.Type == Scanner.Type.PLUS)
                    {
                        CodeSectionAsm.AppendLine("\tadd\tesi,\t" + right);
                    }
                }
                if (SymbolTable.Contains(varToAssign.Lexeme))
                {
                    Symbol symbol = (Symbol)SymbolTable[varToAssign.Lexeme];
                    if (!symbol.Scope.Contains(_scope))
                    {
                        WriteError("Invalid scope");
                        Console.WriteLine("Invalid scope");
                        return false;
                    }
                    CodeSectionAsm.AppendLine("\tmov\tDWORD[" + varToAssign.Lexeme + "],\tesi");
                }
            }
            else
            {
                // just regular assign
                if (_stack.Peek().Type == Scanner.Type.INTCONST)
                {
                    Scanner.Token intToken = _stack.Pop();
                    if (SymbolTable.Contains(varToAssign.Lexeme))
                    {
                        Symbol symbol = (Symbol)SymbolTable[varToAssign.Lexeme];
                        if (!symbol.Scope.Contains(_scope))
                        {
                            WriteError("tried to access variable in an invalid scope");
                            Console.WriteLine("tried to access variable in an invalid scope");
                            return false;
                        }
                        CodeSectionAsm.Append("\tmov\tDWORD[" + symbol.Identifier + "],\t" + intToken.Lexeme + "\n");
                    }
                }
                else if (_stack.Peek().Type == Scanner.Type.STRCONST)
                {
                    Scanner.Token strTok = _stack.Pop();
                    Symbol symbol = new Symbol()
                    {
                        Identifier = "_s" + _genStrCounter,
                        Type = "string",
                        Scope = new Stack<int>(),
                        Store = "scalar",
                        Column = strTok.Column,
                        Line = strTok.Line,
                        Value = strTok.Lexeme
                    };
                    symbol.Scope.Push(_scope);
                    _genStrCounter++;
                    CodeSectionAsm.Append("\tpush\t" + symbol.Identifier + "\n");
                    CodeSectionAsm.Append("\tpush\tstringPrinter\n");
                    CodeSectionAsm.Append("\tcall\t_printf\n");
                    CodeSectionAsm.Append("\tadd\tesp,\t0x08\n");
                    if (!SymbolTable.Contains(symbol.Identifier))
                    {
                        SymbolTable.Add(symbol.Identifier, symbol);
                    }
                }
                else if (_stack.Peek().Type == Scanner.Type.IDENT)
                {
                    Scanner.Token identTok = _stack.Pop();
                    if (SymbolTable.Contains(varToAssign.Lexeme) && SymbolTable.Contains(identTok.Lexeme))
                    {
                        Symbol assignSymbol = (Symbol)SymbolTable[varToAssign.Lexeme];
                        Symbol rightSymbol = (Symbol)SymbolTable[identTok.Lexeme];
                        if (!assignSymbol.Scope.Contains(_scope) || !rightSymbol.Scope.Contains(_scope))
                        {
                            WriteError("tried to access variable in an invalid scope");
                            Console.WriteLine("tried to access variable in an invalid scope");
                            return false;
                        }
                        if (assignSymbol.Type != rightSymbol.Type)
                        {
                            WriteError("Assigned variable to variable of different type");
                            Console.WriteLine("Assigned variable to variable of different type");
                            return false;
                        }
                        CodeSectionAsm.AppendLine("\tmov\teax,\tDWORD[" + rightSymbol.Identifier + "]");
                        CodeSectionAsm.AppendLine("\tmov\tDWORD[" + assignSymbol.Identifier + "],\teax");
                    }
                }
            }
            _stack.Clear();
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
            Scanner.Token varToken = _stack.Pop();
            if (SymbolTable.Contains(varToken.Lexeme))
            {
                Symbol symbol = (Symbol)SymbolTable[varToken.Lexeme];
                if (!symbol.Scope.Contains(_scope))
                {
                    WriteError("Tried to access variable in an invalid scope");
                    Console.WriteLine("Tried to access variable in an invalid scope");
                    return false;
                }
                CodeSectionAsm.AppendLine("\tpush\t" + symbol.Identifier);
                if (symbol.Type == "int")
                {
                    CodeSectionAsm.AppendLine("\tpush\tformatIntIn");
                }
                else if (symbol.Type == "string")
                {
                    CodeSectionAsm.AppendLine("\tpush\tformatStrIn");
                }
                else
                {
                    Console.WriteLine("Currently the compiler can only read in integers and strings");
                    return false;
                }
                CodeSectionAsm.AppendLine("\tcall\t_scanf");
                CodeSectionAsm.AppendLine("\tadd\tesp,\t0x08");
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
            Scanner.Token varToWrite = _stack.Pop();
            if (varToWrite.Type == Scanner.Type.INTCONST)
            {
                CodeSectionAsm.Append("\tpush\t" + varToWrite.Lexeme + "\n");
                CodeSectionAsm.Append("\tpush\tnumberPrinter\n");
                CodeSectionAsm.Append("\tcall\t_printf\n");
                CodeSectionAsm.Append("\tadd\tesp,\t0x08\n");
            }
            else if (varToWrite.Type == Scanner.Type.STRCONST)
            {
                Symbol symbol = new Symbol()
                {
                    Identifier = "_s" + _genStrCounter,
                    Type = "string",
                    Scope = new Stack<int>(),
                    Store = "scalar",
                    Column = varToWrite.Column,
                    Line = varToWrite.Line,
                    Value = varToWrite.Lexeme
                };
                symbol.Scope.Push(_scope);
                _genStrCounter++;
                CodeSectionAsm.Append("\tpush\t" + symbol.Identifier + "\n");
                CodeSectionAsm.Append("\tpush\tstringPrinter\n");
                CodeSectionAsm.Append("\tcall\t_printf\n");
                CodeSectionAsm.Append("\tadd\tesp,\t0x08\n");
                if (!SymbolTable.Contains(symbol.Identifier))
                {
                    SymbolTable.Add(symbol.Identifier, symbol);
                }
            }
            else if (varToWrite.Type == Scanner.Type.IDENT)
            {
                if (SymbolTable.Contains(varToWrite.Lexeme))
                {
                    Symbol symbol = (Symbol)SymbolTable[varToWrite.Lexeme];
                    if (!symbol.Scope.Contains(_scope))
                    {
                        WriteError("Tried to access variable in an invalid scope");
                        Console.WriteLine("Tried to access variable in an invalid scope");
                        return false;
                    }
                    if (symbol.Type == "int")
                    {
                        CodeSectionAsm.Append("\tpush\tDWORD[" + symbol.Identifier + "]\n");
                        CodeSectionAsm.Append("\tpush\tnumberPrinter\n");
                        CodeSectionAsm.Append("\tcall\t_printf\n");
                        CodeSectionAsm.Append("\tadd\tesp,\t0x08\n");
                    }
                    else if (symbol.Type == "string")
                    {
                        CodeSectionAsm.Append("\tpush\t" + symbol.Identifier + "\n");
                        CodeSectionAsm.Append("\tpush\tstringPrinter\n");
                        CodeSectionAsm.Append("\tcall\t_printf\n");
                        CodeSectionAsm.Append("\tadd\tesp,\t0x08\n");
                    }
                    else
                    {
                        Console.WriteLine("Currently the compiler can only handle printing strings and integers");
                    }
                }
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
            // dividing or multiplying here and adding the assembly code to keep order of precedence and just add and subtract at the top
            Scanner.Token rightTok = _stack.Pop();
            Scanner.Token opTok = _stack.Pop();
            Scanner.Token leftTok = _stack.Pop();
            string left = leftTok.Type == Scanner.Type.IDENT ? "DWORD[" + leftTok.Lexeme + "]" : leftTok.Lexeme;
            string right = rightTok.Type == Scanner.Type.IDENT ? "DWORD[" + rightTok.Lexeme + "]" : rightTok.Lexeme;
            // create temporary variable to store the answer
            Symbol tempSymbol = new Symbol()
            {
                Identifier = "_i" + _genIntCounter,
                Type = "int",
                Scope = new Stack<int>(),
                Line = opTok.Line,
                Column = opTok.Column
            };
            _genIntCounter++;
            tempSymbol.Scope.Push(_scope);
            SymbolTable.Add(tempSymbol.Identifier, tempSymbol);
            if (opTok.Type == Scanner.Type.ASTRSK)
            {
                // Constant Folding Optimization
                if (rightTok.Type == Scanner.Type.INTCONST && leftTok.Type == Scanner.Type.INTCONST)
                {
                    leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) * Int32.Parse(rightTok.Lexeme)).ToString();
                    _stack.Push(leftTok);
                }
                else
                {
                    CodeSectionAsm.AppendLine("\tmov\tedi,\t" + left);
                    CodeSectionAsm.AppendLine("\timul\tedi,\t" + right);
                    CodeSectionAsm.AppendLine("\tmov\tDWORD[" + tempSymbol.Identifier + "],\tedi");
                    _stack.Push(new Scanner.Token() { Type = Scanner.Type.IDENT, Lexeme = tempSymbol.Identifier, Column = opTok.Column, Line = opTok.Line });
                }
            }
            else if (opTok.Type == Scanner.Type.SLASH)
            {
                // Constant Folding Optimization
                if (rightTok.Type == Scanner.Type.INTCONST && leftTok.Type == Scanner.Type.INTCONST)
                {
                    leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) / Int32.Parse(rightTok.Lexeme)).ToString();
                    _stack.Push(leftTok);
                }
                else
                {
                    CodeSectionAsm.AppendLine("\txor\tedx,\tedx");
                    CodeSectionAsm.AppendLine("\tmov\teax,\t" + left);
                    CodeSectionAsm.AppendLine("\tmov\tecx,\t" + right);
                    CodeSectionAsm.AppendLine("\tdiv\tecx");
                    CodeSectionAsm.AppendLine("\tmov\tDWORD[" + tempSymbol.Identifier + "],\teax");
                    _stack.Push(new Scanner.Token() { Type = Scanner.Type.IDENT, Lexeme = tempSymbol.Identifier, Column = opTok.Column, Line = opTok.Line });
                }
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
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.STRCONST)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.FALSETOK)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.TRUETOK)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " " + CurrentToken.Type);
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
                // make a temporary variable for the expression
                // add assembly code for ( expression )
                // add the variable onto the stack to bubble up to be add or subtracted from the other variables
                if (_stack.Count > 1)
                {
                    Scanner.Token rightTok = _stack.Pop();
                    Scanner.Token opTok = _stack.Pop();
                    Scanner.Token leftTok = _stack.Pop();
                    string left = leftTok.Type == Scanner.Type.IDENT ? "DWORD[" + leftTok.Lexeme + "]" : leftTok.Lexeme;
                    string right = rightTok.Type == Scanner.Type.IDENT ? "DWORD[" + rightTok.Lexeme + "]" : rightTok.Lexeme;
                    // create temporary variable to store the answer
                    Symbol tempSymbol = new Symbol()
                    {
                        Identifier = "_i" + _genIntCounter,
                        Type = "int",
                        Scope = new Stack<int>(),
                        Line = opTok.Line,
                        Column = opTok.Column
                    };
                    _genIntCounter++;
                    tempSymbol.Scope.Push(_scope);
                    SymbolTable.Add(tempSymbol.Identifier, tempSymbol);
                    if (opTok.Type == Scanner.Type.MINUS)
                    {
                        // Constant Folding Optimization
                        if (rightTok.Type == Scanner.Type.INTCONST && leftTok.Type == Scanner.Type.INTCONST)
                        {
                            leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) - Int32.Parse(rightTok.Lexeme)).ToString();
                            _stack.Push(leftTok);
                        }
                        else
                        {
                            CodeSectionAsm.AppendLine("\tmov\tesi,\t" + left);
                            CodeSectionAsm.AppendLine("\tsub\tesi,\t" + right);
                            CodeSectionAsm.AppendLine("\tmov\tDWORD[" + tempSymbol.Identifier + "],\tesi");
                            _stack.Push(new Scanner.Token() { Type = Scanner.Type.IDENT, Lexeme = tempSymbol.Identifier, Column = opTok.Column, Line = opTok.Line });
                        }
                    }
                    else if (opTok.Type == Scanner.Type.PLUS)
                    {
                        // Constant Folding Optimization
                        if (rightTok.Type == Scanner.Type.INTCONST && leftTok.Type == Scanner.Type.INTCONST)
                        {
                            leftTok.Lexeme = (Int32.Parse(leftTok.Lexeme) + Int32.Parse(rightTok.Lexeme)).ToString();
                            _stack.Push(leftTok);
                        }
                        else
                        {
                            CodeSectionAsm.AppendLine("\tmov\tesi,\t" + left);
                            CodeSectionAsm.AppendLine("\tadd\tesi,\t" + right);
                            CodeSectionAsm.AppendLine("\tmov\tDWORD[" + tempSymbol.Identifier + "],\tesi");
                            _stack.Push(new Scanner.Token() { Type = Scanner.Type.IDENT, Lexeme = tempSymbol.Identifier, Column = opTok.Column, Line = opTok.Line });
                        }
                    }
                }
                else
                {
                    
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
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.NEQ)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.LESS)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.LEQ)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.GEQ)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.GREATER)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " relational operator");
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
                //Console.WriteLine(CurrentToken.Lexeme + " sign");
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.MINUS)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " sign");
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
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.MINUS)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.ORTOK)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
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
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.SLASH)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
                _stack.Push(CurrentToken);
                GetNextToken();
                return true;
            }
            if (CurrentToken.Type == Scanner.Type.ANDOP)
            {
                //Console.WriteLine(CurrentToken.Lexeme + " operation");
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
            _stack.Push(CurrentToken);
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
