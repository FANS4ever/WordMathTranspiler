using System.Text;
using WordMathTranspiler.MathMLParser.Nodes.Data;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for assign operations
    /// </summary>
    public class AssignNode : Node
    {
        public VarNode Var { get; set; }
        public Node Expr { get; set; }
        public AssignNode(VarNode variable, Node expression) {
            Var = variable;
            Expr = expression;
        }

        public override bool IsFloatPointOperation()
        {
            return Expr.IsFloatPointOperation();
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ =");
            sb.AppendLine("├─L: " + IndentHelper(Var.Print(), vSeperator: true));
            sb.Append("└─R: " + IndentHelper(Expr.Print()));
            return sb.ToString();
        }
    }
}
