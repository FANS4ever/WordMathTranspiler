using static WordMathTranspiler.MathMLParser.Nodes.Data.NumNode;

namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class VarNode : Node
    {
        public NumType Type { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Variable name node.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        public VarNode(string name)
        {
            Name = name;
        }

        public override bool IsFloatPointOperation()
        {
            return false;
        }

        public override string Print()
        {
            return Name;
        }
    }
}
