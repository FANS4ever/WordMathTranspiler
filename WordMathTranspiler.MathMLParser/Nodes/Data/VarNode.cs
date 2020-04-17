namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class VarNode : Node
    {
        public string Name { get; set; }
        /// <summary>
        /// Variable name node.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        public VarNode(string name)
        {
            this.Name = name;
        }

        public override string PrettyPrint(int indent, bool useVerticalSeperator = false, int seperatorIndent = 0)
        {
            return this.Name;
        }
    }
}
