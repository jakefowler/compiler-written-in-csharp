﻿using Compiler.Models;
using System;
using System.Diagnostics;
using static Compiler.Models.Scanner;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "rude.pas";
            bool runScanner = false;
            bool runParser = false;
            if(args.Length < 1)
            {
                Console.WriteLine("Pass in file name with -f filename, -s to run the scanner, and -p to run the parser");
                runParser = true;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-f" && i < args.Length - 1)
                {
                    filename = args[i + 1];
                    i++;
                }
                if(args[i] == "-s")
                {
                    runScanner = true;
                }
                if(args[i] == "-s")
                {
                    runParser = true;
                }
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            if (runParser)
            {
                Scanner scanner = new Scanner(filename);
                Parser parser = new Parser(scanner);
            }
            if (runScanner)
            {
                Scanner scanner = new Scanner(filename);
                Token token = new Token();
                while (token.Type != Scanner.Type.EOFTOK)
                {
                    token = scanner.GetNextToken();
                    scanner.PrintToken(token);
                }
            }
            stopWatch.Stop();
            Console.WriteLine("Time Elapsed in Seconds: " + stopWatch.Elapsed.TotalSeconds);

        }
    }
}
