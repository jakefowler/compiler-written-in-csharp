﻿namespace Compiler.Models
{
    internal partial class Scanner
    {
        public enum Type
        {
            ANDOP,
            ARRAYTOK,
            BEGINTOK,
            BOOLTOK,
            CASETOK,
            DEFAULTTOK,
            DOTOK,
            ELSETOK,
            ENDTOK,
            FALSETOK,
            IFTOK,
            INTTOK,
            NOTTOK,
            OFTOK,
            ORTOK,
            PROCEDURE,
            PROGRAM,
            READTOK,
            STRINGTOK,
            SWITCHTOK,
            THENTOK,
            TRUETOK,
            VARTOK,
            WHILETOK,
            WRITETOK,
            RANGE,
            ASSIGN,
            LEQ,
            GEQ,
            NEQ,
            INTCONST,
            STRCONST,
            PLUS,
            MINUS,
            EQL,
            LPAREN,
            RPAREN,
            SEMICOLON,
            COLON,
            COMMA,
            LBRACK,
            RBRACK,
            DOT,
            LESS,
            GREATER,
            ASTRSK,
            SLASH,
            IDENT,
            ILLEGAL,
            EOFTOK
        }
    }
}