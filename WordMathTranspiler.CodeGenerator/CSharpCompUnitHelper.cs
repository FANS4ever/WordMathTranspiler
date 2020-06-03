using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WordMathTranspiler.CodeGenerator
{
    class CSharpCompUnitHelper
    {
        public static string GetCompUnit(SyntaxTree root) {
            return root.GetCompilationUnitRoot().ToString();
        }
    }
}
