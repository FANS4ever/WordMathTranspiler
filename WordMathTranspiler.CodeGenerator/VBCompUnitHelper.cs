using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace WordMathTranspiler.CodeGenerator
{
    class VBCompUnitHelper
    {
        public static string GetCompUnit(SyntaxTree root)
        {
            return root.GetCompilationUnitRoot().ToString();
        }
    }
}
