using System.Xml.Linq;
using WordMathTranspiler.MathMLParser;
using WordMathTranspiler.MathMLParser.Nodes;

namespace WordMathTranspiler.CodeGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            var mlParser = new MlParser();
            Node root = mlParser.Parse(XDocument.Load("E:/Libraries/Desktop/BBP/WordMathTranspiler/WordMathTranspiler.MathMLParser/Resources/TestData.xml").Root);
            CodeGenerator cGen = new CodeGenerator();
            var tree = cGen.Generate(root);
            CodeCompiler.GenerateExecutable(new[] { tree });
        }
    }
}
