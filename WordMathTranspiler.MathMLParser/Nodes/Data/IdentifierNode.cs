using static WordMathTranspiler.MathMLParser.Nodes.Data.NumNode;

namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class IdentifierNode : Node
    {
        public NumType Type { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Variable name node.
        /// </summary>
        /// <param name="name">Name of the variable</param>
        public IdentifierNode(string name)
        {
            Name = name;
        }

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            return false;
        }
        public override string PrintHelper()
        {
            return Name;
        }
        public override string DotHelper(ref int id)
        {
            string identId = $"identifier{id++}";
            return $"{identId}|{identId}[label=\"{Name}\"];\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            IdentifierNode item = obj as IdentifierNode;

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
