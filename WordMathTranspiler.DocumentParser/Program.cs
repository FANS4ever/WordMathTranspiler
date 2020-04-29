using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WordMathTranspiler.DocumentParser
{
    class Program
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
                var configuration = builder.Build();
                var parser = new DocxParser(configuration);
                var xmlFilePath = Path.ChangeExtension(docxFilePath, ".mathml.xml");
                Console.WriteLine("Generating " + Path.GetFileName(xmlFilePath) + "...");
                parser.ParseDocumentToFile(docxFilePath, Path.ChangeExtension(docxFilePath, ".mathml.xml"));
                Console.WriteLine("Finished generating: {0}", Path.GetFullPath(Path.ChangeExtension(docxFilePath, ".mathml.xml")));
            }
            catch (FileNotFoundException)
            {
                // Generate appsettings if it was not found
                // consider generating omml2mml?
                Console.WriteLine("Failed loading appsettings.json.");
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