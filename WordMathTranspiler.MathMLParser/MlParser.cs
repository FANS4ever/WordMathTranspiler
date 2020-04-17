using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.MathMLParser
{
    /// <summary>
    /// Refactor all node methods to use local variables and to return new nodes
    /// </summary>
    public class MlParser
    {
        private Node _root; // Going to rename this to workingNode so we can work on multiple statements
        private int _nodeCount;

        public MlParser() {
            this._root = new EmptyNode();
            this._nodeCount = 0;
        }

        public Node Parse(XElement doc)
        {
            var statements = doc.Elements();

            // temp for testing 2 is broken
            var el = statements.ElementAt(0);
            var parsedTree = ParseStatement(el);

            Console.WriteLine(parsedTree.PrettyPrint(0));

            return parsedTree;
        }

        private static Node ParseStatement(XElement tokenNode)
        {
            if (tokenNode.Name.LocalName == "math")
            {
                return ParseStatement(tokenNode.Elements());
            }
            else
            {
                return ParseStatement(Enumerable.Repeat(tokenNode, 1));
            }
        }

        private static Node ParseStatement(IEnumerable<XElement> tokenNodes)
        {
            if (tokenNodes == null)
            {
                throw new ArgumentNullException();
            }

            List<Node> processedNodes = new List<Node>();
            var el = tokenNodes;
            int elCount = tokenNodes.Count();
            for (int i = 0; i < elCount; i++)
            {
                var currentXNode = el.ElementAtOrDefault(i);
                switch (currentXNode.Name.LocalName)
                {
                    // Expanded statement
                    case "mrow":
                        processedNodes.Add(ParseMrow(currentXNode));
                        break;
                    case "msup":
                        processedNodes.Add(ParseMSUP(currentXNode));
                        break;
                    // Operator
                    case "mo":
                        if (i + 1 < elCount)
                        {
                            // If we start parsing an operator we let the next instance of ParseStatement handle
                            // the rest of the tokenNodes thats why we return here.
                            return ParseMO(CreateMultiplicationTree(processedNodes), currentXNode.Value, ParseStatement(el.Skip(i + 1)));
                        }
                        else
                        {
                            throw new Exception("Unexpected end of input");
                        }
                    // Identifier
                    case "mi":
                        processedNodes.Add(ParseMIVar(currentXNode));
                        break;
                    // Number
                    case "mn":
                        processedNodes.Add(ParseMN(currentXNode));
                        break;
                    default:
                        throw new NotImplementedException("Unsupported node type " + currentXNode.Name.LocalName);
                }
            }
            return CreateMultiplicationTree(processedNodes);
        }


        /// <summary>
        /// Handles mrow elements. Mrow elements contain statements. A math function is also a statement (egzample: Sin(x))
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node ParseMrow(XElement el)
        {
            bool isMathFunction(string val)
            {
                return val == "sin" || val == "cos" ||
                       val == "tan" || val == "sec" || 
                       val == "sech"|| val == "csc" ||
                       val == "csch"|| val == "cot" || 
                       val == "coth";
            }


            var children = el.Elements();
            int childCount = children.Count();

            switch (childCount)
            {
                case 0:
                    return new EmptyNode();
                case 1:
                    return ParseStatement(children.First());
                case 2:
                    if (isMathFunction(children.First().Value))
                    {
                        return new InvocationNode(children.First().Value, ParseStatement(Enumerable.Repeat(children.ElementAt(1), 1)));
                    }
                    else
                    {
                        return ParseStatement(children);
                    }
                default:
                    if (isMathFunction(children.First().Value))
                    {
                        return new InvocationNode(
                            children.First().Value, 
                            ParseStatement(children.Skip(2))
                        );
                    }
                    else
                    {
                        return ParseStatement(children);
                    }
            }
        }

        /// <summary>
        /// Handle mo elements
        /// </summary>
        /// <param name="left"></param>
        /// <param name="op"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static Node ParseMO(Node left, string op, Node right)
        {
            if (left == null || left is EmptyNode)
            {
                throw new ArgumentException("Left argument is empty!");
            }

            if (right == null || right is EmptyNode)
            {
                throw new ArgumentException("Right argument is empty!");
            }

            switch (op)
            {
                case "=":
                case ":=":
                    if (left is VarNode)
                    {
                        return new AssignNode(left as VarNode, right);
                    }
                    else
                    {
                        throw new ArgumentException("Left argument is not a variable.");
                    }
                case "+":
                case "-":
                case "*":
                case "/":
                    return new BinOpNode(left, op, right);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Handle msup elements.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node ParseMSUP(XElement el)
        {
            var supElements = el.Elements();
            int count = supElements.Count();
            if (count == 2)
            {
                var baseEl = supElements.ElementAt(0);
                var supEl = supElements.ElementAt(1);
                return new SupNode(ParseStatement(baseEl), ParseStatement(supEl));
            }
            else
            {
                throw new Exception("Sup expected 2 elements but got " + count);
            }
        }

        /// <summary>
        /// Handle mi variables.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node ParseMIVar(XElement el)
        {
            if (!el.IsEmpty && !string.IsNullOrWhiteSpace(el.Value))
            {
                return new VarNode(el.Value);
            }
            else
            {
                Console.WriteLine("Warning: Possible error in syntax empty identifier element in XML");
                return new EmptyNode();
            }
        }

        /// <summary>
        /// Handle mn elements. Only numbers appear as mn elements.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node ParseMN(XElement el)
        {
            if (!el.IsEmpty)
            {
                long l;
                float f;
                if (long.TryParse(el.Value, out l))
                {
                    return new NumNode(l);
                } 
                else if (float.TryParse(el.Value, out f))
                {
                    return new FloatNode(f);
                } 
                else
                {
                    throw new Exception("Failed to parse number node.");
                }
            }
            else
            {
                Console.WriteLine("Warning: Possible error in syntax empty number element in XML");
                return new EmptyNode();
            }
        }

        /// <summary>
        /// Create multiplication tree for nodes in list
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static Node CreateMultiplicationTree(List<Node> list)
        {
            switch (list.Count)
            {
                case 0:
                    return new EmptyNode();
                case 1:
                    return list[0];
                default:
                    Node result = new EmptyNode();
                    int notEmptyIndex = -1;
                    for (int i = 1; i < list.Count; i++)
                    {
                        if (result is EmptyNode)
                        {
                            if (list[i] is EmptyNode) // If current node is empty
                            {
                                if (notEmptyIndex == -1 && !(list[i - 1] is EmptyNode)) // If previous node is not empty and no waiting nodes
                                {
                                    notEmptyIndex = i - 1;
                                }
                                continue;
                            }

                            if (notEmptyIndex > -1) // If we have a non empty node waiting
                            {
                                result = ParseMO(list[notEmptyIndex], "*", list[i]);
                                notEmptyIndex = -1;
                            }
                            else // If no nodes are waiting
                            {
                                if (list[i - 1] is EmptyNode) // If previous node is empty
                                {
                                    continue;
                                }
                                result = ParseMO(list[i - 1], "*", list[i]);
                            }
                        }
                        else
                        {
                            if (list[i] is EmptyNode) // Remove empty nodes
                            {
                                continue;
                            }
                            BinOpNode temp = result as BinOpNode;
                            temp.right = ParseMO(temp.right, "*", list[i]);
                            result = temp;
                        }
                    }
                    return result;
            }
        }
    }
}