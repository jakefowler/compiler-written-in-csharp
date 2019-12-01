﻿using System;
using System.Collections;
using System.IO;

namespace Compiler.Models
{
    internal partial class Scanner
    {
        private int _lineLoc;
        private string _lineText;
        private int _lineNum;
        private bool _processingLine;
        public StreamReader Reader { get; set; }

        public static Hashtable ReservedWords = new Hashtable()
        {
            { "and", Type.ANDOP },
            { "array", Type.ARRAYTOK },
            { "begin", Type.BEGINTOK },
            { "boolean", Type.BOOLTOK },
            { "case", Type.CASETOK },
            { "default", Type.DEFAULTTOK },
            { "do", Type.DOTOK },
            { "else", Type.ELSETOK },
            { "end", Type.ENDTOK },
            { "false", Type.FALSETOK },
            { "if", Type.IFTOK },
            { "int", Type.INTTOK },
            { "not", Type.NOTTOK },
            { "of", Type.OFTOK },
            { "or", Type.ORTOK },
            { "procedure", Type.PROCEDURE },
            { "program", Type.PROGRAM },
            { "read", Type.READTOK },
            { "string", Type.STRINGTOK },
            { "switch", Type.SWITCHTOK },
            { "then", Type.THENTOK },
            { "true", Type.TRUETOK },
            { "var", Type.VARTOK },
            { "while", Type.WHILETOK },
            { "write", Type.WRITETOK },
        };

        public struct Token
        {
            public Type? Type;
            public string Lexeme;
            public int Line;
            public int Column;
        }

        public Scanner(string filePath)
        {
            Reader = new StreamReader(filePath);
            _lineNum = 0;
            _lineText = null;
        }

        public void PrintToken(Token token)
        {
            if (token.Type.ToString().Length < 4)
            {
                Console.WriteLine("Token Type: " + token.Type + "\t\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
            }
            else
            {
                Console.WriteLine("Token Type: " + token.Type + "\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
            }
        }

        public Token GetNextToken()
        {
            if (!Reader.EndOfStream || _processingLine)
            {
                // Clear whitespace before token
                while (_lineText == null || _lineText == "" || _lineLoc >= _lineText.Length)
                {
                    _lineText = Reader.ReadLine();
                    _lineNum++;
                    _lineLoc = 0;
                    _processingLine = true;
                }
                while (_lineLoc < _lineText.Length && _lineText[_lineLoc] == ' ' || _lineText[_lineLoc] == '\t')
                {
                    _lineLoc++;
                    if (_lineLoc >= _lineText.Length)
                    {
                        _lineText = Reader.ReadLine();
                        _lineNum++;
                        _lineLoc = 0;
                        _processingLine = true;
                    }
                }
                Token token = new Token
                {
                    Line = _lineNum,
                    Column = _lineLoc + 1
                };
                // Single line comments
                if (_lineLoc < _lineText.Length - 1)
                {
                    while (_lineText[_lineLoc] == '/' && _lineText[_lineLoc + 1] == '/')
                    {
                        _lineText = Reader.ReadLine();
                        _lineNum++;
                        _lineLoc = 0;
                        _processingLine = true;
                        token.Line = _lineNum;
                        token.Column = _lineLoc + 1;
                    }
                    // Clear leading whitespace after new line
                    while (_lineLoc < _lineText.Length && _lineText[_lineLoc] == ' ' || _lineText[_lineLoc] == '\t')
                    {
                        _lineLoc++;
                        token.Column = _lineLoc + 1;
                    }
                }
                // Multi line comments
                if (_lineLoc < _lineText.Length - 1)
                {
                    if (_lineText[_lineLoc] == '/' && _lineText[_lineLoc + 1] == '*')
                    {
                        _lineLoc += 2;
                        bool legalComment = false;
                        while (!Reader.EndOfStream || _processingLine)
                        {
                            if (_lineLoc >= _lineText.Length)
                            {
                                _lineText = Reader.ReadLine();
                                _lineNum++;
                                _lineLoc = 0;
                                _processingLine = true;
                                token.Line = _lineNum;
                                token.Column = _lineLoc + 1;
                            }
                            if (_lineText[_lineLoc] == '*' && _lineLoc < _lineText.Length - 1 && _lineText[_lineLoc + 1] == '/')
                            {
                                _lineLoc += 2;
                                legalComment = true;
                                if (_lineLoc >= _lineText.Length)
                                {
                                    _lineText = Reader.ReadLine();
                                    _lineNum++;
                                    _lineLoc = 0;
                                    _processingLine = true;
                                    token.Line = _lineNum;
                                    token.Column = _lineLoc + 1;
                                }
                                break;
                            }
                            _lineLoc++;
                            if (Reader.EndOfStream && _lineLoc >= _lineText.Length)
                            {
                                _processingLine = false;
                                break;
                            }
                        }
                        if (!legalComment)
                        {
                            token.Type = Type.ILLEGAL;
                            return token;
                        }
                        // Clear whitespace after multi line comment
                        while (_lineLoc < _lineText.Length && _lineText[_lineLoc] == ' ' || _lineText[_lineLoc] == '\t')
                        {
                            _lineLoc++;
                            token.Column = _lineLoc + 1;
                            if (_lineLoc >= _lineText.Length)
                            {
                                _lineText = Reader.ReadLine();
                                _lineNum++;
                                _lineLoc = 0;
                                _processingLine = true;
                                token.Line = _lineNum;
                                token.Column = _lineLoc + 1;
                            }
                        }
                    }
                }
                // Multi character special characters
                if (_lineLoc < _lineText.Length - 1)
                {
                    if (_lineText[_lineLoc] == '.' && _lineText[_lineLoc + 1] == '.')
                    {
                        token.Lexeme = "..";
                        token.Type = Type.RANGE;
                        _lineLoc += 2;
                    }
                    else if (_lineText[_lineLoc] == ':' && _lineText[_lineLoc + 1] == '=')
                    {
                        token.Lexeme = ":=";
                        token.Type = Type.ASSIGN;
                        _lineLoc += 2;
                    }
                    else if (_lineText[_lineLoc] == '<' && _lineText[_lineLoc + 1] == '=')
                    {
                        token.Lexeme = "<=";
                        token.Type = Type.LEQ;
                        _lineLoc += 2;
                    }
                    else if (_lineText[_lineLoc] == '>' && _lineText[_lineLoc + 1] == '=')
                    {
                        token.Lexeme = ">=";
                        token.Type = Type.GEQ;
                        _lineLoc += 2;
                    }
                    else if (_lineText[_lineLoc] == '<' && _lineText[_lineLoc + 1] == '>')
                    {
                        token.Lexeme = "<>";
                        token.Type = Type.NEQ;
                        _lineLoc += 2;
                    }
                    // Integer constants and detecting if number starts with 0
                    else if (char.IsDigit(_lineText[_lineLoc]))
                    {
                        if (_lineText[_lineLoc] == '0' && _lineLoc < _lineText.Length && char.IsDigit(_lineText[_lineLoc + 1]))
                        {
                            token.Type = Type.ILLEGAL;
                            while (char.IsDigit(_lineText[_lineLoc]))
                            {
                                token.Lexeme += _lineText[_lineLoc];
                                _lineLoc++;
                            }
                            return token;
                        }
                        while (char.IsDigit(_lineText[_lineLoc]))
                        {
                            token.Lexeme += _lineText[_lineLoc];
                            _lineLoc++;
                        }
                        token.Type = Type.INTCONST;
                    }
                    // String constants and making sure the string ends correctly
                    else if (_lineText[_lineLoc] == '"')
                    {
                        token.Lexeme += '"';
                        token.Type = Type.STRCONST;
                        _lineLoc++;
                        while (_lineText[_lineLoc] != '"')
                        {
                            token.Lexeme += _lineText[_lineLoc];
                            if (Reader.EndOfStream && (_lineText == null || _lineLoc >= _lineText.Length - 1))
                            {
                                _processingLine = false;
                                token.Type = Type.ILLEGAL;
                                return token;
                            }
                            if (_lineLoc >= _lineText.Length - 1)
                            {
                                _lineText = Reader.ReadLine();
                                _lineNum++;
                                _lineLoc = 0;
                                _processingLine = true;
                            }
                            _lineLoc++;
                        }
                        token.Lexeme += _lineText[_lineLoc];
                        _lineLoc++;
                        if (_lineLoc >= _lineText.Length)
                        {
                            _lineText = null;
                        }
                        return token;
                    }
                }
                // Single character symbols
                if (token.Lexeme == null)
                {
                    switch (_lineText[_lineLoc])
                    {
                        case '+':
                            _lineLoc++;
                            token.Lexeme = "+";
                            token.Type = Type.PLUS;
                            break;

                        case '-':
                            token.Lexeme = "-";
                            // negative integer constants
                            if (_lineLoc < _lineText.Length - 1 && char.IsDigit(_lineText[_lineLoc + 1]))
                            {
                                _lineLoc++;
                                while (char.IsDigit(_lineText[_lineLoc]) && _lineLoc < _lineText.Length)
                                {
                                    token.Lexeme += _lineText[_lineLoc];
                                    _lineLoc++;
                                }
                                token.Type = Type.INTCONST;
                                break;
                            }
                            _lineLoc++;
                            token.Type = Type.MINUS;
                            break;

                        case '=':
                            _lineLoc++;
                            token.Lexeme = "=";
                            token.Type = Type.EQL;
                            break;

                        case '(':
                            _lineLoc++;
                            token.Lexeme = "(";
                            token.Type = Type.LPAREN;
                            break;

                        case ')':
                            _lineLoc++;
                            token.Lexeme = ")";
                            token.Type = Type.RPAREN;
                            break;

                        case ';':
                            _lineLoc++;
                            token.Lexeme = ";";
                            token.Type = Type.SEMICOLON;
                            break;

                        case ':':
                            _lineLoc++;
                            token.Lexeme = ":";
                            token.Type = Type.COLON;
                            break;

                        case ',':
                            _lineLoc++;
                            token.Lexeme = ",";
                            token.Type = Type.COMMA;
                            break;

                        case '[':
                            _lineLoc++;
                            token.Lexeme = "[";
                            token.Type = Type.LBRACK;
                            break;

                        case ']':
                            _lineLoc++;
                            token.Lexeme = "]";
                            token.Type = Type.RBRACK;
                            break;

                        case '.':
                            _lineLoc++;
                            token.Lexeme = ".";
                            token.Type = Type.DOT;
                            break;

                        case '<':
                            _lineLoc++;
                            token.Lexeme = "<";
                            token.Type = Type.LESS;
                            break;

                        case '>':
                            _lineLoc++;
                            token.Lexeme = ">";
                            token.Type = Type.GREATER;
                            break;

                        case '*':
                            _lineLoc++;
                            token.Lexeme = "*";
                            token.Type = Type.ASTRSK;
                            break;

                        case '/':
                            _lineLoc++;
                            token.Lexeme = "/";
                            token.Type = Type.SLASH;
                            break;
                    }
                }
                // Identifiers
                if (token.Lexeme == null && _lineLoc < _lineText.Length && char.IsLetter(_lineText[_lineLoc]))
                {
                    while (_lineLoc < _lineText.Length && _lineText[_lineLoc] != ' ' && (char.IsLetter(_lineText[_lineLoc]) || char.IsDigit(_lineText[_lineLoc]) || _lineText[_lineLoc] == '_'))
                    {
                        token.Lexeme += char.ToLower(_lineText[_lineLoc]);
                        _lineLoc++;
                    }
                }
                if (_lineLoc >= _lineText.Length)
                {
                    _lineText = null;
                    _lineLoc = 0;
                    _processingLine = false;
                }
                // Reserved words
                if (token.Lexeme != null && token.Type == null)
                {
                    if (ReservedWords.ContainsKey(token.Lexeme))
                    {
                        token.Type = (Type)ReservedWords[token.Lexeme];
                    }
                    else
                    {
                        token.Type = Type.IDENT;
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
                    token.Type = Type.ILLEGAL;
                    _lineLoc++;
                    return token;
                }
            }
            return new Token() { Type = Type.EOFTOK, Lexeme = "", Line = _lineNum, Column = _lineLoc };
        }
    }
}