﻿using Compiler.Models;
using System;
using static Compiler.Models.Scanner;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Scanner scanner = new Scanner("stest.pas");
            Token token = new Token();
            while (token.Type != "EOFTOK")
            {
                token = scanner.GetNextToken();
                if (token.Type.Length < 4)
                {
                    Console.WriteLine("Token Type: " + token.Type + "\t\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
                }
                else
                {
                    Console.WriteLine("Token Type: " + token.Type + "\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
                }
            }
        }
    }
}
