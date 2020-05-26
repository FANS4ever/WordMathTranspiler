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

        public override bool Equals(object obj)
        {
            VarNode item = obj as VarNode;

            if (item == null)
            {
                return false;
            }

            return item.Name.Equals(Name) && item.Type.Equals(Type);
        }

        public override int GetHashCode()
        {
            return (Name, Type).GetHashCode();
        }
    }
}
