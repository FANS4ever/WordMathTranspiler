using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class StatementNode : Node
    {
        public enum StatementType
        {
            None,
            DeclarationStatement
        }
        public StatementType Type {
            get {
                switch (Body)
                {
                    case AssignNode a:
                        return StatementType.DeclarationStatement;
                    default:
                        return StatementType.None;
                }
            }
        }

        /// <summary>
        /// Statement body
        /// </summary>
        public Node Body { get; set; }
        /// <summary>
        /// Next statement in sequence
        /// </summary>
        public Node Next { get; set; }
        public StatementNode(Node body, Node next)
        {
            Body = body;
            Next = next;
        }

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            return Body.IsFloatPointOperation();
        }
        public override string TextPrint()
        {
            return $"{Body.TextPrint()}\n{Next.TextPrint()}";
        }
        public override string TreePrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ Statement type:" + Type.ToString() + " isFloat: " + IsFloatPointOperation());
            sb.AppendLine("├─L: " + IndentHelper(Body.TreePrint(), drawSeperator: true));
            sb.Append("└─R: " + IndentHelper(Next.TreePrint()));
            return sb.ToString();
        }
        public override string DotPrint(ref int id)
        {
            string statId = $"stat{id++}";
            string statDecl = $"{statId}[label=\"Statement\"];\n";
            var statBodyData = Body.DotPrint(ref id).Split('|');
            var statNextData = Next.DotPrint(ref id).Split('|');
            return $"{statId}|{statDecl}{statBodyData[1]}{statNextData[1]}{statId} -> {statBodyData[0]};\n{statId} -> {statNextData[0]};\n";
        }
        #endregion

        public override bool Equals(object obj)
        {
            StatementNode item = obj as StatementNode;

            if (item == null)
            {
                return false;
            }

            return Body.Equals(item.Body) && Next.Equals(item.Next);
        }
        public override int GetHashCode()
        {
            return Body.GetHashCode() ^ Next.GetHashCode();
        }
    }
}
