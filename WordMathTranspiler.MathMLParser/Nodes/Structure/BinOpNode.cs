using System;
using System.Collections.Generic;
using System.Text;
using WordMathTranspiler.MathMLParser.Nodes;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for math operations
    /// </summary>
    public class BinOpNode : Node
    {
        public Node left { get; set; }
        public string op {get; set;}
        public Node right { get; set; }
        public BinOpNode(Node left, string op, Node right)
        {
            this.left = left;
            this.op = op;
            this.right = right;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            int newIndent = indent + 6;
            string lp = left.PrettyPrint(newIndent, true, seperatorIndent > 0 ? seperatorIndent : indent);
            string rp = right.PrettyPrint(newIndent, useVerticalSeperator, seperatorIndent);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine('(' + this.op + ')');
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.AppendLine(" ├─L: " + lp);
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.Append(" └─R: " + rp);

            return sb.ToString();
        }
    }
}
