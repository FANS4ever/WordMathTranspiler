using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;

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

                Console.WriteLine("Generating AST...");
                Node ast = new EmptyNode();
                // Ignore errors
                var ignoreErrorsSection = config.GetSection("ignoreErrors");
                if (ignoreErrorsSection.Exists() && ignoreErrorsSection.Value.Equals("True"))
                {
                    ast = MlParser.Parse(docxFilePath, true);
                }
                else
                {
                    ast = MlParser.Parse(docxFilePath);
                }
                Console.WriteLine("Finished generating AST.");

                // Result type
                var resultTypeSection = config.GetSection("resultType");
                var resultType = resultTypeSection.Exists() ? resultTypeSection.Value : "Text";

                // Write to console
                var writeToConsoleSection = config.GetSection("writeResultToConsole");
                if (writeToConsoleSection.Exists() && writeToConsoleSection.Value.Equals("True"))
                {
                    Console.WriteLine("Printing result to console... \n");
                    switch (resultType)
                    {
                        case "Text":
                            Console.WriteLine(ast.TreePrint());
                            break;
                        case "DOT":
                            Console.WriteLine(Node.BuildDotGraph(ast));
                            break;
                        case "JSON":
                            Console.WriteLine(Node.SerializeJson(ast));
                            break;
                        default:
                            Console.WriteLine("Result type not recognized, printing as text...");
                            Console.WriteLine(ast.TreePrint());
                            break;
                    }
                    Console.WriteLine();
                }

                // Write to file
                var writeToFileSection = config.GetSection("writeResultToFile");
                if (writeToFileSection.Exists() && writeToFileSection.Value.Equals("True"))
                {
                    Console.WriteLine("Writing result to file...");
                    using (StreamWriter outputToFile = new StreamWriter(Path.ChangeExtension(docxFilePath, "ast.txt")))
                    {
                        switch (resultType)
                        {
                            case "Text":
                                outputToFile.WriteLine(ast.TreePrint());
                                break;
                            case "DOT":
                                outputToFile.WriteLine(Node.BuildDotGraph(ast));
                                break;
                            case "JSON":
                                outputToFile.WriteLine(Node.SerializeJson(ast));
                                break;
                            default:
                                Console.WriteLine("Result type not recognized, writing as text...");
                                outputToFile.WriteLine(ast.TreePrint());
                                break;
                        }
                    }
                }

                Console.WriteLine("Finished.");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("[ERROR] Failed loading appsettings.json.");
            }
            catch (Exception x)
            {
                Console.WriteLine("[ERROR] {0}", x.Message);
            }
            ProgramFinished();
        }

        public static void ProgramFinished()
        {
            Console.WriteLine("Press ENTER button to continue...");
            Console.ReadLine();
        }
    }
}
