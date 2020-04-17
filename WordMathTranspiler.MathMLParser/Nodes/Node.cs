using System;
using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes
{
    /// <summary>
    /// Base node class
    /// </summary>
    public abstract class Node
    {
        protected void AdjustSeperator(StringBuilder sb, bool useVerticalSeperator, int indent, int seperatorIndent)
        {
            if (useVerticalSeperator)
            {
                if (indent < seperatorIndent)
                {
                    throw new Exception("seperatorIndent cant be larger than indent!");
                }
                var indentAfterSeperator = indent - seperatorIndent;
                sb.Append(' ', seperatorIndent + 1 > 0 ? seperatorIndent + 1 : 0);
                sb.Append('|');
                sb.Append(' ', (indentAfterSeperator - 2) > 0 ? indentAfterSeperator - 2 : 0);
            }
            else
            {
                sb.Append(' ', indent > 0 ? indent : 0);
            }
        }
        public abstract string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0);
    }
}
