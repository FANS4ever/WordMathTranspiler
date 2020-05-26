using System.Collections.Generic;
using System.Data;
using System.Text;
using WordMathTranspiler.MathMLParser.Nodes.Data;

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

        public override bool IsFloatPointOperation()
        {
            return Body.IsFloatPointOperation();
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ Statement type:" + Type.ToString() + " isFloat: " + IsFloatPointOperation());
            sb.AppendLine("├─L: " + IndentHelper(Body.Print(), vSeperator: true));
            sb.Append("└─R: " + IndentHelper(Next.Print()));
            return sb.ToString();
        }

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
