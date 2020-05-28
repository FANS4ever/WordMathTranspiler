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
            "pow", // x^2 | 2 args
            "sqrt" // Math.Sqrt(x) | 1 arg
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

        #region Node overrides
        public override bool IsFloatPointOperation()
        {
            // For now assume that its true
            return true;
        }
        public override string TextPrint()
        {
            string iResult = $"{Fn}(";
            for (int i = 0; i < Args.Count; i++)
            {
                var arg = Args[i];
                iResult += arg.TextPrint() + (i != Args.Count - 1 ? ", " : "");
            }
            return $"{iResult})";
        }
        public override string TreePrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("┌ " + Fn);

            if (Args.Count > 1)
            {
                for (int i = 0; i < Args.Count; i++)
                {
                    if (i == Args.Count - 1)
                    {
                        sb.Append("└─Arg" + i + ": " + IndentHelper(Args[i].TreePrint(), 7 + i.ToString().Length));
                    }
                    else
                    {
                        sb.AppendLine("├─Arg" + i + ": " + IndentHelper(Args[i].TreePrint(), indentCount: 7 + i.ToString().Length, drawSeperator: true));
                    }
                }
            }
            else
            {
                sb.Append("└─Arg1: " + IndentHelper(Args[0].TreePrint(), 8));
            }
            return sb.ToString();
        }
        public override string DotPrint(ref int id)
        {
            string invocId = $"invoc{id++}";
            string invocDecl = $"{invocId}[label=\"{Fn}\"]\n";
            string invocResult = invocDecl;
            for (int i = 0; i < Args.Count; i++)
            {
                var argData = Args[i].DotPrint(ref id).Split('|');
                invocResult += $"{argData[1]}{invocId} -> {argData[0]};\n";
            }
            return $"{invocId}|{invocResult}";
        }
        #endregion

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
