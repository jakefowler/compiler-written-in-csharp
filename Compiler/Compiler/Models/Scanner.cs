using System.Collections;
using System.IO;

namespace Compiler.Models
{
    internal class Scanner
    {
        private readonly string _filePath;
        private int _curLineLoc;
        private string _curLine;
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
            _curLine = null;
        }

        public Token GetNextToken()
        {
            if (!Reader.EndOfStream || _processingLine)
            {
                while (_curLine == null)
                {
                    _curLine = Reader.ReadLine();
                    _lineNum++;
                    _processingLine = true;
                }
                Token token = new Token
                {
                    Line = _lineNum,
                    Column = _curLineLoc
                };

                while (_curLineLoc != _curLine.Length && _curLine[_curLineLoc] != ' ')
                {
                    token.Lexeme += _curLine[_curLineLoc];
                    _curLineLoc++;
                }
                _curLineLoc++;
                if (_curLineLoc >= _curLine.Length)
                {
                    _curLine = null;
                    _curLineLoc = 0;
                    _processingLine = false;
                }
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
            return new Token() { Type = "EOFTOK", Lexeme = "", Line = _lineNum, Column = _curLineLoc };
        }
    }
}