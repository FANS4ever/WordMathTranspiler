using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class SupNode : Node
    {
        public Node Base { get; set; }
        public Node Sup { get; set; }

        /// <summary>
        /// Remove this node and use InvocationNode instead
        /// </summary>
        /// <param name="baseEl"></param>
        /// <param name="supEl"></param>
        public SupNode(Node baseEl, Node supEl)
        {
            this.Base = baseEl;
            this.Sup = supEl;
        }

        public override bool IsFloatPointOperation()
        {
            return Base.IsFloatPointOperation() || Sup.IsFloatPointOperation();
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ Sup");
            sb.AppendLine("├─L: " + IndentHelper(Base.Print(), vSeperator: true));
            sb.Append("└─R: " + IndentHelper(Sup.Print()));
            return sb.ToString();
        }
    }
}
