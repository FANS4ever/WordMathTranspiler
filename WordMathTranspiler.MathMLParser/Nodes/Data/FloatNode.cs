namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class FloatNode : Node
    {
        public float Value { get; set; }

        /// <summary>
        /// Floating point number node.
        /// </summary>
        /// <param name="value">Constant float value</param>
        public FloatNode(float value)
        {
            this.Value = value;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            return this.Value.ToString();
        }
    }
}
