using System.Text;
using WordMathTranspiler.MathMLParser.Nodes.Data;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for assign operations
    /// </summary>
    public class AssignNode : Node
    {
        public VarNode variable { get; set; }
        public Node expression { get; set; }
        public AssignNode(VarNode variable, Node expression) {
            this.variable = variable;
            this.expression = expression;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            int newIndent = indent + 6;
            string lp = variable.PrettyPrint(newIndent, true, seperatorIndent > 0 ? seperatorIndent : indent);
            string rp = expression.PrettyPrint(newIndent, useVerticalSeperator, seperatorIndent);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("(=)");
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.AppendLine(" ├─L: " + lp);
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.Append(" └─R: " + rp);
            
            return sb.ToString();
        }
    }
}
