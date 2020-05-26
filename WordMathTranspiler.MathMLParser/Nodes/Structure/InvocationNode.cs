using System.Collections.Generic;
using System.Text;

namespace WordMathTranspiler.MathMLParser.Nodes.Structure
{
    public class InvocationNode : Node
    {
        public static List<string> BuiltInFunctions = new List<string>() {
            "sin", "sec", "sech", // sin(x) | 1 arg
            "cos", "csc", "csch", // cos(x) | 1 arg
            "tan", "cot", "coth", // tan(x) | 1 arg
            "pow" // x^2 | 2 args
        };
        public bool IsBuiltinFunction {
            get {
                return BuiltInFunctions.Contains(Fn);
            } 
        }
        public string Fn { get; set; }
        public List<Node> Args { get; set; }

        public InvocationNode(string fn, Node arg)
        {
            Fn = fn;
            Args = new List<Node>() { arg };
        }

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
                    if (i == Args.Count - 1)
                    {
                        sb.Append("└─Arg" + i + ": " + IndentHelper(Args[i].Print(), 7 + i.ToString().Length));
                    }
                    else
                    {
                        sb.AppendLine("├─Arg" + i + ": " + IndentHelper(Args[i].Print(), count: 7 + i.ToString().Length, drawSeperator: true));
                    }
                }
            }
            else
            {
                sb.Append("└─Arg1: " + IndentHelper(Args[0].Print(), 8));
            }
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            InvocationNode item = obj as InvocationNode;

            if (item == null)
            {
                return false;
            }

            if (Args.Count == item.Args.Count)
            {
                for (int i = 0; i < Args.Count; i++)
                {
                    if (!Args[i].Equals(item.Args[i]))
                    {
                        return false;
                    }
                }
                return Fn.Equals(item.Fn);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Fn.GetHashCode() ^ Args.GetHashCode();
        }
    }
}
