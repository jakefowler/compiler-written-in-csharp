using Compiler.Models;
using System;
using static Compiler.Models.Scanner;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Scanner scanner = new Scanner("input-file.txt");
            Token token = new Token();
            while (token.Type != "EOFTOK")
            {
                token = scanner.GetNextToken();
                Console.WriteLine("Token Type: " + token.Type + "\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
            }
        }
    }
}
