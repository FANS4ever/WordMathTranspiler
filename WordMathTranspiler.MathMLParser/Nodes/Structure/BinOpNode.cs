﻿using System.CodeDom.Compiler;
using System.IO;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    /// <summary>
    /// Node used for math operations
    /// </summary>
    public class BinOpNode : Node
    {
        public Node LeftExpr { get; set; }
        public string Op {get; set;}
        public Node RightExpr { get; set; }
        public BinOpNode(Node left, string op, Node right)
        {
            LeftExpr = left;
            Op = op;
            RightExpr = right;
        }

        public override bool IsFloatPointOperation()
        {
            return Op == "/" || LeftExpr.IsFloatPointOperation() || RightExpr.IsFloatPointOperation();
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ " + Op);
            sb.AppendLine("├─L: " + IndentHelper(LeftExpr.Print(), drawSeperator: true));
            sb.Append("└─R: " + IndentHelper(RightExpr.Print()));
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            BinOpNode item = obj as BinOpNode;

            if (item == null)
            {
                return false;
            }

            return LeftExpr.Equals(item.LeftExpr) && RightExpr.Equals(item.RightExpr) && Op.Equals(item.Op);
        }

        public override int GetHashCode()
        {
            return LeftExpr.GetHashCode() ^ RightExpr.GetHashCode() ^ Op.GetHashCode();
        }
    }
}
