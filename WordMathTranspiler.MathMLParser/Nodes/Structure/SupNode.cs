using System;
using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class SupNode : Node
    {
        public Node baseEl { get; set; }
        public Node supEl { get; set; }
        public SupNode(Node baseEl, Node supEl)
        {
            this.baseEl = baseEl;
            this.supEl = supEl;
        }
        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            int newIndent = indent + 6;
            string lp = baseEl.PrettyPrint(newIndent, true, seperatorIndent > 0 ? seperatorIndent : indent);
            string rp = supEl.PrettyPrint(newIndent, useVerticalSeperator, seperatorIndent);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Sup");
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.AppendLine(" ├─L: " + lp);
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.Append(" └─R: " + rp);

            return sb.ToString();
        }
    }
}
