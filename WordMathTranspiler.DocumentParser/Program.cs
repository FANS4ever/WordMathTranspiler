using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WordMathTranspiler.DocumentParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Specify a .docx file.");
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
                var xmlFilePath = Path.ChangeExtension(args[0], ".mathml.xml");
                Console.WriteLine("Generating " + Path.GetFileName(xmlFilePath) + "...");
                parser.ParseDocumentToFile(args[0], Path.ChangeExtension(args[0], ".mathml.xml"));
                Console.WriteLine("Generation succeeded.");
            }
            catch (FileNotFoundException)
            {
                // Generate appsettings if it was not found
                // consider generating omml?
                Console.WriteLine("Failed loading appsettings.json.");
            }
        }
    }
}