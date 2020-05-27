namespace WordMathTranspiler.MathMLParser.Nodes.Data
{
    public class EmptyNode : Node
    {
        /// <summary>
        /// Node that contains no data
        /// </summary>
        public EmptyNode() { }

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            return false;
        }
        public override string PrintHelper()
        {
            return "EmptyNode";
        }

        public override string DotHelper(ref int id)
        {
            string emptyId = $"E{id++}";
            return $"{emptyId}|{emptyId}[label=\"Empty\"];\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            EmptyNode item = obj as EmptyNode;
            return item != null;
        }
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
    }
}
