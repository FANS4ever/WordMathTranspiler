using System.Text;
using WordMathTranspiler.MathMLParser.Nodes.Data;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for math operations
    /// </summary>
    public class BinOpNode : Node
    {
        public Node LeftExpr { get; set; }
        public string Op {get; set;}
        public Node RightExpr { get; set; }
        public BinOpNode(Node left, string op, Node right)
        {
            LeftExpr = left;
            Op = op;
            RightExpr = right;
        }

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            return Op == "/" || LeftExpr.IsFloatPointOperation() || RightExpr.IsFloatPointOperation();
        }
        public override string TextPrint()
        {
            string result = "";
            if (LeftExpr is NumNode ||
                LeftExpr is IdentifierNode ||
                LeftExpr is InvocationNode)
            {
                result += LeftExpr.TextPrint();
            }
            else
            {
                result += $"({LeftExpr.TextPrint()})";
            }

            result += $" {Op} ";

            if (RightExpr is NumNode ||
                RightExpr is IdentifierNode ||
                RightExpr is InvocationNode)
            {
                result += RightExpr.TextPrint();
            }
            else
            {
                result += $"({RightExpr.TextPrint()})";
            }
            return result;
        }
        public override string TreePrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ " + Op);
            sb.AppendLine("├─L: " + IndentHelper(LeftExpr.TreePrint(), drawSeperator: true));
            sb.Append("└─R: " + IndentHelper(RightExpr.TreePrint()));
            return sb.ToString();
        }
        public override string DotPrint(ref int id)
        {
            string opId = $"op{id++}";
            string opDecl = $"{opId}[label=\"{Op}\"];\n";
            var opLeftData = LeftExpr.DotPrint(ref id).Split('|');
            var opRightData = RightExpr.DotPrint(ref id).Split('|');
            return $"{opId}|{opDecl}{opLeftData[1]}{opRightData[1]}{opId} -> {opLeftData[0]};\n{opId} -> {opRightData[0]};\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            BinOpNode item = obj as BinOpNode;

            if (item == null)
            {
                return false;
            }

            return LeftExpr.Equals(item.LeftExpr) && RightExpr.Equals(item.RightExpr) && Op.Equals(item.Op);
        }
        public override int GetHashCode()
        {
            return LeftExpr.GetHashCode() ^ RightExpr.GetHashCode() ^ Op.GetHashCode();
        }
    }
}
