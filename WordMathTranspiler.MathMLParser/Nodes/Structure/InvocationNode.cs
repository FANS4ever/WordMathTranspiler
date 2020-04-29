using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class InvocationNode : Node
    {
        public string Fn { get; set; }
        public List<Node> Args { get; set; }

        public InvocationNode(string fn, Node arg)
        {
            Fn = fn;
            Args = new List<Node>() { arg };
        }

        /// <summary>
        /// Function invocation node.
        /// </summary>
        /// <param name="fn">function name</param>
        /// <param name="arg">function arguments</param>
        public InvocationNode(string fn, List<Node> args)
        {
            Fn = fn;
            Args = args;
        }

        public override bool IsFloatPointOperation()
        {
            // For now assume that its true
            return true;
        }

        public override string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ " + Fn);

            if (Args.Count > 1)
            {
                for (int i = 0; i < Args.Count; i++)
                {
                    var arg = Args[i];
                    if (i == Args.Count - 1)
                    {
                        sb.Append("└─Arg" + i + ": " + IndentHelper(Args[0].Print(), 8));
                    }
                    else
                    {
                        sb.Append("├─Arg" + i + ": " + IndentHelper(Args[0].Print(), count: 8, vSeperator: true));
                    }
                }
            }
            else
            {
                sb.Append("└─Arg1: " + IndentHelper(Args[0].Print(), 8));
            }
            return sb.ToString();
        }
    }
}
