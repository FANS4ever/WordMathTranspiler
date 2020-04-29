using System;
using System.Xml.Linq;

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

            XDocument doc = XDocument.Load(args[0]);
            MlParser parser = new MlParser();
            parser.Parse(doc.Root);

            ProgramFinished();
        }

        public static void ProgramFinished()
        {
            Console.WriteLine("Press ENTER button to continue...");
            Console.ReadLine();
        }
    }
}
