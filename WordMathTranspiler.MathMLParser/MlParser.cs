using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
            var statements = doc.Elements().ToList();

            // temp for testing 2 is broken
            var el = statements[1];
            var parsedTree = ParseStatement(el);

            Console.WriteLine(parsedTree.PrettyPrint(0));

            return parsedTree;
        }

        private static Node ParseStatement(XElement token)
        {
            if (token == null)
            {
                throw new ArgumentNullException();
            }

            // Left side of assignment
            var subTokens = token.Elements().ToList();
            if (subTokens.Count >= 3 && 
                subTokens[0].Name.LocalName.Equals("mi") &&
                subTokens[1].Name.LocalName.Equals("mo"))
            {
                // Test if value exists
                return new AssignNode(
                    variable: new VarNode(subTokens[0].Value),
                    expression: ParseExpression(subTokens.Skip(2).ToList())
                );
            }
            else
            {
                Console.WriteLine("Something is wrong in left side of expression");
                return new EmptyNode();
            }
        }

        private static Node ParseExpression(XElement token)
        {
            return ParseExpression(new List<XElement>() { token });
        }

        private static Node ParseExpression(List<XElement> tokens)
        {
            if (tokens.Count == 0)
            {
                return new EmptyNode();
            }

            // Used to make multiplication tree
            List<Node> stack = new List<Node>();

            // Go through all sub tokens
            for (int i = 0; i < tokens.Count; i++)
            {
                // Current token (Because getting element in list is O(n))
                var cToken = tokens[i];
                switch (cToken.Name.LocalName)
                {
                    // Mrow - groups elements (Found when calling math function)
                    case "mrow":
                        stack.Add(ParseMrow(cToken));
                        continue;
                    // Msup - element power (x^2)
                    case "msup":
                        stack.Add(ParseMSUP(cToken));
                        continue;
                    // Mo - assignment operator and binary operators
                    case "mo":
                        if (cToken.Value == "(") // Handle brackets
                        {
                            var closeIndex = tokens.FindIndex((x) => x.Name.LocalName == "mo" && x.Value == ")");
                            if (closeIndex == -1)
                            {
                                throw new Exception("Missing closing bracket");
                            }
                            else
                            {
                                // Parses brackets as sub expression
                                stack.Add(ParseExpression(tokens.GetRange(i + 1, closeIndex - i - 1)));
                                // Push index to continue point
                                i = closeIndex;
                                continue;
                            }
                        }

                        if (i + 1 < tokens.Count)
                        {
                            switch (cToken.Value)
                            {
                                case "*":
                                case "/":
                                    var mul = CreateMultiplicationTree(stack); 
                                    stack.Clear(); // Clear list because we parsed it to one node
                                    // Pushes index to next element because we use it in creating node
                                    stack.Add(new BinOpNode(mul, cToken.Value, ParseExpression(tokens[++i])));
                                    continue;
                                case "+":
                                case "-":
                                    // Return because left side argument handles the rest of the elements
                                    // No need to clear list like in * and / because we return
                                    return new BinOpNode(CreateMultiplicationTree(stack), cToken.Value, ParseExpression(tokens.Skip(i + 1).ToList()));
                                default:
                                    throw new Exception("Unsupported operation");
                            }
                        }
                        else
                        {
                            throw new Exception("Unexpected end of input");
                        }
                    // Mi - identifier element (function name or variable name)
                    case "mi":
                        stack.Add(ParseMIVar(cToken));
                        continue;
                    // Mn - number element
                    case "mn":
                        stack.Add(ParseMN(cToken));
                        continue;
                    default:
                        throw new NotImplementedException("Unsupported node type " + cToken.Name.LocalName);
                }
            }
            // No need to clear list because we return
            return CreateMultiplicationTree(stack);
        }

        /// <summary>
        /// Handles mrow elements. Mrow elements contain statements. A math function is also a statement (egzample: Sin(x))
        /// 
        /// Add support for multiple parameters?
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
                        return new InvocationNode(
                            children.First().Value, 
                            ParseStatement(children.ElementAt(1))
                        );
                    }
                    else
                    {
                        return ParseExpression(children.ToList());
                    }
                default:
                    if (isMathFunction(children.First().Value))
                    {
                        return new InvocationNode(
                            children.First().Value,
                            ParseExpression(children.Skip(2).ToList())
                        );
                    }
                    else
                    {
                        return ParseExpression(children.ToList());
                    }
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
            Node result = new EmptyNode();
            switch (list.Count)
            {
                case 1:
                    result = list[0];
                    break;
                default:
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
                                result = new BinOpNode(list[notEmptyIndex], "*", list[i]);
                                notEmptyIndex = -1;
                            }
                            else // If no nodes are waiting
                            {
                                if (list[i - 1] is EmptyNode) // If previous node is empty
                                {
                                    continue;
                                }
                                result = new BinOpNode(list[i - 1], "*", list[i]);
                            }
                        }
                        else
                        {
                            if (list[i] is EmptyNode) // Remove empty nodes
                            {
                                continue;
                            }
                            BinOpNode temp = result as BinOpNode;
                            temp.right = new BinOpNode(temp.right, "*", list[i]);
                            result = temp;
                        }
                    }
                    break;
            }
            return result;
        }
    }
}