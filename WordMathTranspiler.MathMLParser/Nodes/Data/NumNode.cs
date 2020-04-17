namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class NumNode : Node
    {
        public long Value { get; set; } 
        /// <summary>
        /// Number node.
        /// </summary>
        /// <param name="value">Constant value</param>
        public NumNode(long value)
        {
            this.Value = value;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            return this.Value.ToString();
        }
    }
}
