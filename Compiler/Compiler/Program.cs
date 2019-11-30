using Compiler.Models;
using System;
using System.Diagnostics;
using static Compiler.Models.Scanner;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Scanner scanner = new Scanner("bob.pas");
            Parser parser = new Parser(scanner);
            //Token token = new Token();
            //while (token.Type != Scanner.Type.EOFTOK)
            //{
            //    token = scanner.GetNextToken();
            //    if (token.Type.ToString().Length < 4)
            //    {
            //        Console.WriteLine("Token Type: " + token.Type + "\t\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
            //    }
            //    else
            //    {
            //        Console.WriteLine("Token Type: " + token.Type + "\tLexeme: " + token.Lexeme + "\tLine#: " + token.Line + "\tColumn#: " + token.Column);
            //    }
            //}
            stopWatch.Stop();
            Console.WriteLine("Time Elapsed in Seconds: " + stopWatch.Elapsed.TotalSeconds);

        }
    }
}
