using System;
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
            { "case", "CASETOK" },
            { "default", "DEFAULTTOK" },
            { "do", "DOTOK" },
            { "else", "ELSETOK" },
            { "end", "ENDTOK" },
            { "if", "IFTOK" },
            { "not", "NOTTOK" },
            { "of", "OFTOK" },
            { "or", "ORTOK" },
            { "procedure", "PROCEDURE" },
            { "program", "PROGRAM" },
            { "read", "READTOK" },
            { "switch", "SWITCHTOK" },
            { "then", "THENTOK" },
            { "var", "VARTOK" },
            { "while", "WHILETOK" },
            { "write", "WRITETOK" },
            { "int", "INTTOK" },
            { "string", "STRINGTOK" },
            { "boolean", "BOOLTOK" },
            { "true", "TRUETOK" },
            { "false", "FALSETOK" },
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
                    _processingLine = true;
                }
                Token token = new Token
                {
                    Line = _lineNum,
                    Column = _lineLoc
                };
                while (_lineLoc < _lineText.Length && _lineText[_lineLoc] == ' ' || _lineText[_lineLoc] == '\t')
                {
                    _lineLoc++;
                }
                if (_lineLoc < _lineText.Length)
                {
                    if (_lineLoc < _lineText.Length - 1)
                    {
                        if (_lineText[_lineLoc] == '/' && _lineText[_lineLoc + 1] == '/')
                        {
                            _lineText = Reader.ReadLine();
                            _lineNum++;
                            _processingLine = true;
                        }
                    }
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
                    }
                }
                if (token.Lexeme == null)
                {
                    while (_lineLoc < _lineText.Length && _lineText[_lineLoc] != ' ' && (char.IsLetter(_lineText[_lineLoc]) || char.IsDigit(_lineText[_lineLoc])))
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
            }
            return new Token() { Type = "EOFTOK", Lexeme = "", Line = _lineNum, Column = _lineLoc };
        }
    }
}