using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    class StatementNode : Node
    {
        public Node left { get; set; }
        public Node right { get; set; }
        public StatementNode(Node left, Node right)
        {
            this.left = left;
            this.right = right;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            int newIndent = indent + 6;
            string lp = left.PrettyPrint(newIndent, true, seperatorIndent > 0 ? seperatorIndent : indent);
            string rp = right.PrettyPrint(newIndent, useVerticalSeperator, seperatorIndent);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("(Statement)");
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.AppendLine(" ├─L: " + lp);
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.Append(" └─R: " + rp);

            return sb.ToString();
        }
    }
}
