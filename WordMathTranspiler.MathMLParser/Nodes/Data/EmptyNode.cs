namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class EmptyNode : Node
    {
        /// <summary>
        /// Node that contains no data
        /// </summary>
        public EmptyNode() { }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            return "(EmptyNode)";
        }
    }
}
