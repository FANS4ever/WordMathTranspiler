using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    class UnaryOpNode: Node
    {
        public string Op { get; set; }
        public Node Expr { get; set; }
        public UnaryOpNode(string op, Node expression)
        {
            Op = op;
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
            sb.AppendLine("┌ Unary " + Op);
            sb.Append("└─Expr: " + IndentHelper(Expr.PrintHelper(), 8));
            return sb.ToString();
        }
        public override string DotHelper(ref int id)
        {
            string unOpId = $"unOp{id++}";
            string unOpDecl = $"{unOpId}[label=\"Unary:{Op}\"];\n";
            var unOpExprData = Expr.DotHelper(ref id).Split('|');
            return $"{unOpId}|{unOpDecl}{unOpExprData[1]}{unOpId} -> {unOpExprData[0]};\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            UnaryOpNode item = obj as UnaryOpNode;

            if (item == null)
            {
                return false;
            }

            return Op.Equals(item.Op) && Expr.Equals(item.Expr);
        }
        public override int GetHashCode()
        {
            return Op.GetHashCode() ^ Expr.GetHashCode();
        }
    }
}
