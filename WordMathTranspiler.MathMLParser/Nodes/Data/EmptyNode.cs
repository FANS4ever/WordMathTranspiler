namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class EmptyNode : Node
    {
        /// <summary>
        /// Node that contains no data
        /// </summary>
        public EmptyNode() { }

        public override bool IsFloatPointOperation()
        {
            return false;
        }

        public override string Print()
        {
            return "EmptyNode";
        }
    }
}
