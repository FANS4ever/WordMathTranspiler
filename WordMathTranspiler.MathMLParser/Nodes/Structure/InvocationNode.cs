using System;
using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class InvocationNode : Node
    {
        public string fn { get; set; }
        public Node arg { get; set; } // Change to list?

        /// <summary>
        /// Function invocation node.
        /// </summary>
        /// <param name="fn">function name</param>
        /// <param name="arg">function arguments</param>
        public InvocationNode(string fn, Node arg)
        {
            this.fn = fn;
            this.arg = arg;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            int newIndent = indent + 8;
            string arg = this.arg.PrettyPrint(newIndent, useVerticalSeperator, seperatorIndent);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine('(' + this.fn + ')');
            AdjustSeperator(sb, useVerticalSeperator, indent, seperatorIndent);
            sb.Append(" └─Arg: " + arg);

            return sb.ToString();
        }
    }
}
