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
            Scanner scanner = new Scanner("pteststmt2.pas");
            Parser parser = new Parser(scanner);
            //Token token = new Token();
            //while (token.Type != Scanner.Type.EOFTOK)
            //{
            //    token = scanner.GetNextToken();
            //    scanner.PrintToken(token);
            //}
            stopWatch.Stop();
            Console.WriteLine("Time Elapsed in Seconds: " + stopWatch.Elapsed.TotalSeconds);

        }
    }
}
