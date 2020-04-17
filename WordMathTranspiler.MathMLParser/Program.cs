using System;
using System.Xml.Linq;

namespace WordMathTranspiler.MathMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Specify a .mathml.xml file.");
                return;
            }

            XDocument doc = XDocument.Load(args[0]);
            MlParser parser = new MlParser();
            parser.Parse(doc.Root);
        }
    }
}
