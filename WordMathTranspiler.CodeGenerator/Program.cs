using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using WordMathTranspiler.MathMLParser;
using WordMathTranspiler.DocumentParser;

namespace WordMathTranspiler.CodeGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            string docxFilePath = "";
            if (args.Length == 0)
            {
                Console.Write("Specify a *.docx file: ");
                docxFilePath = Console.ReadLine();
            }
            else
            {
                docxFilePath = args[0];
            }

            if (Path.GetExtension(docxFilePath) != ".docx")
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

                // Programming language
                var languageSection = config.GetSection("language");
                var compileLanguage = languageSection.Exists() && languageSection.Value == "VisualBasic" ? languageSection.Value : "CSharp";

                // Namespace
                var namespaceSection = config.GetSection("namespace");
                var namespaceName = namespaceSection.Exists() && !namespaceSection.Equals(string.Empty) ? namespaceSection.Value : "TranspiledMML";

                // Class name
                var classNameSection = config.GetSection("className");
                var className = classNameSection.Exists() && !classNameSection.Equals(string.Empty) ? classNameSection.Value : "Program";

                Console.WriteLine("Reading file...");
                DocxParser.ParseDocumentToFile(docxFilePath, Path.ChangeExtension(docxFilePath, ".mathml.xml"));

                Console.WriteLine("Building AST...");
                var ast = MlParser.Parse(Path.ChangeExtension(docxFilePath, ".mathml.xml"));

                Console.WriteLine("Generating code...");
                var syntaxTree = (new CodeGenerator(compileLanguage)).Generate(ast, namespaceName, className);


                using (StreamWriter outputToFile = new StreamWriter(Path.ChangeExtension(docxFilePath, $"{compileLanguage}.txt")))
                {
                    switch (compileLanguage)
                    {
                        case "VisualBasic":
                            outputToFile.WriteLine(VBCompUnitHelper.GetCompUnit(syntaxTree));
                            break;
                        default:
                            outputToFile.WriteLine(CSharpCompUnitHelper.GetCompUnit(syntaxTree));
                            break;
                    }
                }

                Console.WriteLine("Generating executables...");
                CodeCompiler.GenerateExecutable(new[] { syntaxTree }, Path.GetDirectoryName(docxFilePath), compileLanguage, $"{namespaceName}.{className}");
                Console.WriteLine("Finished.");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("[ERROR] Failed at loading appsettings.json.");
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