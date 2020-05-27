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

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            return Expr.IsFloatPointOperation();
        }
        public override string PrintHelper()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ =");
            sb.AppendLine("├─L: " + IndentHelper(Var.PrintHelper(), drawSeperator: true));
            sb.Append("└─R: " + IndentHelper(Expr.PrintHelper()));
            return sb.ToString();
        }
        public override string DotHelper(ref int id)
        {
            string assignId = $"assign{id++}";
            string assignDecl = $"{assignId}[label=\"=\"];\n";
            var assignIdentData = Var.DotHelper(ref id).Split('|');
            var assignExprData = Expr.DotHelper(ref id).Split('|');
            return $"{assignId}|{assignDecl}{assignIdentData[1]}{assignExprData[1]}{assignId} -> {assignIdentData[0]};\n{assignId} -> {assignExprData[0]};\n";
        }
        #endregion

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
