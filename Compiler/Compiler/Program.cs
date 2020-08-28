using Compiler.Models;
using System;
using System.Diagnostics;
using System.Linq;
using static Compiler.Models.Scanner;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "mathTest.pas";
            bool runScanner = false;
            bool runParser = false;
            bool verbose = false;
            if (args.Length < 1)
            {
                Console.WriteLine("Pass in file name with -f filename, -s to run the scanner, -p to run the parser, and -v for printing verbose information.");
                runParser = true;
                verbose = true;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-f" && i < args.Length - 1)
                {
                    filename = args[i + 1];
                    i++;
                }
                if (args[i] == "-s")
                {
                    runScanner = true;
                }
                if (args[i] == "-p")
                {
                    runParser = true;
                }
                if (args[i] == "-v")
                {
                    verbose = true;
                }
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            if (runParser)
            {
                Scanner scanner = new Scanner(filename);
                Parser parser = new Parser(scanner);
                if (verbose)
                {
                    parser.PrintSymbolTable();
                }
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
            string baseFilename = filename.Substring(0, filename.Length - 4).ToLower();
            stopWatch.Stop();
            Console.WriteLine("Time Elapsed in Seconds: " + stopWatch.Elapsed.TotalSeconds);
            Process process = new Process();
            process.StartInfo.FileName = "nasm.exe";
            process.StartInfo.Arguments = $"-f win32 {baseFilename}.asm";
            process.Start();
            process.WaitForExit();

            Process linkProcess = new Process();
            linkProcess.StartInfo.FileName = "link.exe";
            linkProcess.StartInfo.Arguments = $"/OUT:{baseFilename}.exe msvcrtd.lib {baseFilename}.obj";
            linkProcess.Start();
            linkProcess.WaitForExit();

            Process outputProcess = new Process();
            outputProcess.StartInfo.FileName = $"{baseFilename}.exe";
            outputProcess.Start();
            outputProcess.WaitForExit();
        }
    }
}
