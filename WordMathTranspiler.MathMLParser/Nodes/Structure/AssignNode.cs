using System.Text;
using WordMathTranspiler.MathMLParser.Nodes.Data;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for assign operations
    /// </summary>
    public class AssignNode : Node
    {
        public IdentifierNode Var { get; set; }
        public Node Expr { get; set; }
        public AssignNode(IdentifierNode variable, Node expression) {
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
            sb.AppendLine("├─L: " + IndentHelper(Var.Print(), drawSeperator: true));
            sb.Append("└─R: " + IndentHelper(Expr.Print()));
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            AssignNode item = obj as AssignNode;

            if (item == null)
            {
                return false;
            }

            return Var.Equals(item.Var) && Expr.Equals(item.Expr);
        }

        public override int GetHashCode()
        {
            return Var.GetHashCode() ^ Expr.GetHashCode();
        }
    }
}
