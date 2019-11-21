using System.Collections;
using System.IO;

namespace Compiler.Models
{
    internal class Scanner
    {
        private readonly string _filePath;
        private int _lineLoc;
        private string _lineText;
        private int _lineNum;
        private bool _processingLine;
        public StreamReader Reader { get; set; }

        public Hashtable ReservedWords = new Hashtable()
        {
            { "and", "ANDOP" },
            { "array", "ARRAYTOK" },
            { "begin", "BEGINTOK" },
            { "boolean", "BOOLTOK" },
            { "case", "CASETOK" },
            { "default", "DEFAULTTOK" },
            { "do", "DOTOK" },
            { "else", "ELSETOK" },
            { "end", "ENDTOK" },
            { "false", "FALSETOK" },
            { "if", "IFTOK" },
            { "int", "INTTOK" },
            { "not", "NOTTOK" },
            { "of", "OFTOK" },
            { "or", "ORTOK" },
            { "procedure", "PROCEDURE" },
            { "program", "PROGRAM" },
            { "read", "READTOK" },
            { "string", "STRINGTOK" },
            { "switch", "SWITCHTOK" },
            { "then", "THENTOK" },
            { "true", "TRUETOK" },
            { "var", "VARTOK" },
            { "while", "WHILETOK" },
            { "write", "WRITETOK" },
        };

        public struct Token
        {
            public string Type;
            public string Lexeme;
            public int Line;
            public int Column;
        }

        public Scanner(string filePath)
        {
            _filePath = filePath;
            Reader = new StreamReader(filePath);
            _lineNum = 0;
            _lineText = null;
        }

        public Token GetNextToken()
        {
            if (!Reader.EndOfStream || _processingLine)
            {
                while (_lineText == null || _lineText == "")
                {
                    _lineText = Reader.ReadLine();
                    _lineNum++;
                    _lineLoc = 0;
                    _processingLine = true;
                }
                while (_lineLoc < _lineText.Length && _lineText[_lineLoc] == ' ' || _lineText[_lineLoc] == '\t')
                {
                    _lineLoc++;
                }
                Token token = new Token
                {
                    Line = _lineNum,
                    Column = _lineLoc
                };
                if (_lineLoc < _lineText.Length)
                {
                    if (_lineLoc < _lineText.Length - 1)
                    {
                        // index problem with two lines in a row that are commented out
                        while (_lineText[_lineLoc] == '/' && _lineText[_lineLoc + 1] == '/')
                        {
                            _lineText = Reader.ReadLine();
                            _lineNum++;
                            _lineLoc = 0;
                            _processingLine = true;
                        }
                        if (_lineText[_lineLoc] == '.' && _lineText[_lineLoc + 1] == '.')
                        {
                            token.Lexeme = "..";
                            token.Type = "RANGE";
                            _lineLoc += 2;
                        }
                        else if (_lineText[_lineLoc] == ':' && _lineText[_lineLoc + 1] == '=')
                        {
                            token.Lexeme = ":=";
                            token.Type = "ASSIGN";
                            _lineLoc += 2;
                        }
                        else if (_lineText[_lineLoc] == '<' && _lineText[_lineLoc + 1] == '=')
                        {
                            token.Lexeme = "<=";
                            token.Type = "LEQ";
                            _lineLoc += 2;
                        }
                        else if (_lineText[_lineLoc] == '<' && _lineText[_lineLoc + 1] == '>')
                        {
                            token.Lexeme = "<>";
                            token.Type = "NEQ";
                            _lineLoc += 2;
                        }
                        else if (char.IsDigit(_lineText[_lineLoc]))
                        {
                            while (char.IsDigit(_lineText[_lineLoc]))
                            {
                                token.Lexeme += _lineText[_lineLoc];
                                _lineLoc++;
                            }
                            token.Type = "INTCONST";
                        }
                        else if (_lineText[_lineLoc] == '"')
                        {
                            token.Lexeme += '"';
                            token.Type = "STRCONST";
                            _lineLoc++;
                            // need to finish adding strings
                            while(_lineText[_lineLoc] != '"')
                            {
                                token.Lexeme += _lineText[_lineLoc];
                                _lineLoc++;
                                if (_lineLoc >= _lineText.Length - 1)
                                {
                                    _lineText = Reader.ReadLine();
                                    _lineNum++;
                                    _lineLoc = 0;
                                    _processingLine = true;
                                }
                            }
                            token.Lexeme += _lineText[_lineLoc];
                            _lineLoc++;
                            // need to do end of line check
                            return token;
                        }
                    }
                    if (token.Lexeme == null)
                    {
                        switch (_lineText[_lineLoc])
                        {
                            case '+':
                                _lineLoc++;
                                token.Lexeme = "+";
                                token.Type = "PLUS";
                                break;

                            case '-':
                                _lineLoc++;
                                token.Lexeme = "-";
                                token.Type = "MINUS";
                                break;

                            case '=':
                                _lineLoc++;
                                token.Lexeme = "=";
                                token.Type = "EQL";
                                break;

                            case '(':
                                _lineLoc++;
                                token.Lexeme = "(";
                                token.Type = "LPAREN";
                                break;

                            case ')':
                                _lineLoc++;
                                token.Lexeme = ")";
                                token.Type = "RPAREN";
                                break;

                            case ';':
                                _lineLoc++;
                                token.Lexeme = ";";
                                token.Type = "SEMICOLON";
                                break;

                            case ':':
                                _lineLoc++;
                                token.Lexeme = ":";
                                token.Type = "COLON";
                                break;

                            case ',':
                                _lineLoc++;
                                token.Lexeme = ",";
                                token.Type = "COMMA";
                                break;

                            case '[':
                                _lineLoc++;
                                token.Lexeme = "[";
                                token.Type = "LBRACK";
                                break;

                            case ']':
                                _lineLoc++;
                                token.Lexeme = "]";
                                token.Type = "RBRACK";
                                break;

                            case '.':
                                _lineLoc++;
                                token.Lexeme = ".";
                                token.Type = "DOT";
                                break;

                            case '<':
                                _lineLoc++;
                                token.Lexeme = "<";
                                token.Type = "LESS";
                                break;

                            case '>':
                                _lineLoc++;
                                token.Lexeme = ">";
                                token.Type = "GREATER";
                                break;

                            case '*':
                                _lineLoc++;
                                token.Lexeme = "*";
                                token.Type = "ASTRSK";
                                break;

                            case '/':
                                _lineLoc++;
                                token.Lexeme = "/";
                                token.Type = "SLASH";
                                break;
                        }
                    }
                }
                // check to make sure it starts with a letter
                if (token.Lexeme == null && _lineLoc < _lineText.Length && char.IsLetter(_lineText[_lineLoc]))
                {
                    while (_lineLoc < _lineText.Length && _lineText[_lineLoc] != ' ' && (char.IsLetter(_lineText[_lineLoc]) || char.IsDigit(_lineText[_lineLoc]) || _lineText[_lineLoc] == '_'))
                    {
                        token.Lexeme += _lineText[_lineLoc];
                        _lineLoc++;
                    }
                }
                if (_lineLoc >= _lineText.Length)
                {
                    _lineText = null;
                    _lineLoc = 0;
                    _processingLine = false;
                }
                if (token.Lexeme != null && token.Type == null)
                {
                    if (ReservedWords.ContainsKey(token.Lexeme))
                    {
                        token.Type = ReservedWords[token.Lexeme].ToString();
                    }
                    else
                    {
                        token.Type = "IDENT";
                    }
                    return token;
                }
                if (token.Lexeme != null && token.Type != null)
                {
                    return token;
                }
                else
                {
                    token.Lexeme += _lineText[_lineLoc];
                    token.Type = "ILLEGAL";
                    _lineLoc++;
                    return token;
                }
            }
            return new Token() { Type = "EOFTOK", Lexeme = "", Line = _lineNum, Column = _lineLoc };
        }
    }
}