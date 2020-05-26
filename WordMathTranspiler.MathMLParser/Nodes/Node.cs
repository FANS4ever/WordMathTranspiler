using System;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;
using Newtonsoft.Json;

namespace WordMathTranspiler.MathMLParser.Nodes
{
    /// <summary>
    /// Base node class
    /// </summary>
    public abstract class Node
    {
        // Abstract methods to implement
        public abstract string Print();
        public abstract bool IsFloatPointOperation();
       
        //  Printing methods
        public static string TextPrint(Node root)
        {
            switch (root)
            {
                case EmptyNode emptyNode:
                    return "";
                case NumNode numNode:
                    return numNode.Value.ToString();
                case VarNode varNode:
                    return varNode.Name.ToString();
                case AssignNode assignNode:
                    return TextPrint(assignNode.Var) + " = " + TextPrint(assignNode.Expr);
                case InvocationNode invocatioNode:
                    string iResult = invocatioNode.Fn + "(";
                    for (int i = 0; i < invocatioNode.Args.Count; i++)
                    {
                        var arg = invocatioNode.Args[i];
                        iResult += TextPrint(arg) + (i != invocatioNode.Args.Count - 1 ? ", " : "");
                    }
                    return iResult + ")";
                case BinOpNode operatorNode:
                    string oResult = "";
                    if (operatorNode.LeftExpr is NumNode ||
                        operatorNode.LeftExpr is VarNode ||
                        operatorNode.LeftExpr is InvocationNode)
                    {
                        oResult += TextPrint(operatorNode.LeftExpr);
                    }
                    else
                    {
                        oResult += '(' + TextPrint(operatorNode.LeftExpr) + ')';
                    }

                    oResult += ' ' + operatorNode.Op + ' ';

                    if (operatorNode.RightExpr is NumNode ||
                        operatorNode.RightExpr is VarNode ||
                        operatorNode.RightExpr is InvocationNode)
                    {
                        oResult += TextPrint(operatorNode.RightExpr);
                    }
                    else
                    {
                        oResult += '(' + TextPrint(operatorNode.RightExpr) + ')';
                    }
                    return oResult;
                default:
                    throw new NotImplementedException("Node type not implemented yet.");
            }
        }
        /// <summary>
        /// Adds indentation to multiline strings.
        /// </summary>
        /// <param name="src">Input string</param>
        /// <param name="count">Indentation level (spaces). Default: 5</param>
        /// <param name="vSeperator">Should the method use the '│' vertical seperator. Default: false</param>
        /// <returns></returns>
        protected static string IndentHelper(string src, int count = 5, bool vSeperator = false)
        {
            string[] split = src.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length > 1)
            {
                string result = split[0];
                for (int i = 1; i < split.Length; i++)
                {
                    string item = split[i];
                    if (vSeperator)
                    {
                        result += Environment.NewLine + '│' + new string(' ', count - 1) + item;
                    }
                    else
                    {
                        result += Environment.NewLine + new string(' ', count) + item;
                    }
                }
                return result;
            }
            else
            {
                return split[0];
            }
        }
        public static string DOTPrint(Node root)
        {
            int idCounter = 0;
            return String.Format("digraph graphname {0}", "{\n" + DOTHelper(root, ref idCounter).Split('|')[1] + '}');
        }
        private static string DOTHelper(Node root, ref int id)
        {
            switch (root)
            {
                case EmptyNode emptyNode:
                    string emptyId = "E" + (id++);
                    return emptyId + '|' + emptyId + "[label=\"Empty\"];\n";
                case NumNode numNode:
                    string numId = "num" + (id++);
                    return numId + '|' + numId + string.Format("[label=\"{0}\"];\n", numNode.Value.ToString());
                case VarNode varNode:
                    string varId = "var" + (id++);
                    return varId + '|' + varId + string.Format("[label=\"{0}\"];\n", varNode.Name.ToString());
                case AssignNode assignNode:
                    string assignId = "assign" + (id++);
                    string assignDecl = assignId + "[label=\"=\"];\n";
                    var varData = DOTHelper(assignNode.Var, ref id).Split('|');
                    var exprData = DOTHelper(assignNode.Expr, ref id).Split('|');
                    return assignId + '|' + assignDecl + varData[1] + exprData[1] + assignId + " -> " + varData[0] + ";\n" + assignId + " -> " + exprData[0] + ";\n";
                case InvocationNode invocatioNode:
                    string invocId = "invoc" + (id++);
                    string invocDecl = invocId + string.Format("[label=\"{0}\"]", invocatioNode.Fn);
                    string result = invocDecl;
                    for (int i = 0; i < invocatioNode.Args.Count; i++)
                    {
                        var argData = DOTHelper(invocatioNode.Args[i], ref id).Split('|');
                        result += argData[1] + invocId + " -> " + argData[0] + ";\n";
                    }
                    return invocId + '|' + result;
                case BinOpNode operatorNode:
                    string opId = "op" + (id++);
                    string opDecl = opId + string.Format("[label=\"{0}\"];\n", operatorNode.Op);
                    var leftData = DOTHelper(operatorNode.LeftExpr, ref id).Split('|');
                    var rightData = DOTHelper(operatorNode.RightExpr, ref id).Split('|');
                    return opId + '|' + opDecl + leftData[1] + rightData[1] + opId + " -> " + leftData[0] + ";\n" + opId+ " -> " + rightData[0] + ";\n";
                case StatementNode statementNode:
                    string statId = "stat" + (id++);
                    string statDecl = statId + "[label=\"Statement\"];\n";
                    var bodyData = DOTHelper(statementNode.Body, ref id).Split('|');
                    var nextData = DOTHelper(statementNode.Next, ref id).Split('|');
                    return statId + '|' + statDecl + bodyData[1] + nextData[1] + statId + " -> " + bodyData[0] + ";\n" + statId + " -> " + nextData[0] + ";\n";
                default:
                    throw new NotImplementedException("Node type not implemented yet. Type: " + root);
            }
        }

        // Json serialization
        public static string SerializeJson(Node root)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            return JsonConvert.SerializeObject(root, settings);
        }
        public static Node DeserializeJson(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            return JsonConvert.DeserializeObject<StatementNode>(json, settings);
        }
    }
}
