﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace WordMathTranspiler.MathMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string docxFilePath = "";
            if (args.Length == 0)
            {
                Console.Write("Specify a *.mathml.xml file: ");
                docxFilePath = Console.ReadLine();
            }
            else
            {
                docxFilePath = args[0];
            }

            if (Path.GetExtension(docxFilePath) != ".xml")
            {
                Console.WriteLine("Unsupported file type {0}", Path.GetExtension(docxFilePath));
                ProgramFinished();
                return;
            }

            try
            {
                Console.WriteLine("Loading appsettings.json...");
                var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
                var config = builder.Build();

                MlParser parser = new MlParser();
                Console.WriteLine("Generating AST...");
                var astRoot = parser.Parse(args[0]);


                // Move to a settings method
                var writeToFile = config.GetSection("writeResultToFile");
                if (writeToFile.Exists() && writeToFile.Value.Equals("True"))
                {
                    using (StreamWriter outputToFile = new StreamWriter(Path.ChangeExtension(docxFilePath, "ast.txt")))
                    {
                        outputToFile.WriteLine(astRoot.Print());
                    }
                }

                // Move to a settings method
                var writeToConsole = config.GetSection("writeResultToConsole");
                if (writeToConsole.Exists() && writeToConsole.Value.Equals("True"))
                {
                    Console.WriteLine(astRoot.Print());
                }


                Console.WriteLine("Finished generating AST");
                ProgramFinished();
            }
            catch (FileNotFoundException)
            {
                // Generate appsettings if it was not found
                Console.WriteLine("[ERROR] Failed loading appsettings.json.");
            }
            catch (Exception x)
            {
                Console.WriteLine("[ERROR] {0}", x.Message);
            }
        }

        public static void ProgramFinished()
        {
            Console.WriteLine("Press ENTER button to continue...");
            Console.ReadLine();
        }
    }
}
