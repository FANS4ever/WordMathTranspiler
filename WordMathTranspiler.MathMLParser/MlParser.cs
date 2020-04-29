using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.MathMLParser
{
    public class MlParser
    {
        public MlParser() {}

        public Node Parse(string path)
        {
            return Parse(XDocument.Load(path, LoadOptions.SetLineInfo).Root);
        }
        private Node Parse(XElement doc)
        {
            var statements = doc.Elements().ToList();

            Node parsedTreeRoot = new EmptyNode();
            Node parsedExpression = new EmptyNode();
            Dictionary<string, NumNode.NumType> symbolTable = new Dictionary<string, NumNode.NumType>();
            for (int i = 0; i < statements.Count; i++)
            {
                if (parsedTreeRoot is EmptyNode)
                {
                    parsedTreeRoot = ParseStatement(statements[i], symbolTable);
                    parsedExpression = parsedTreeRoot;
                    continue;
                }
                (parsedExpression as StatementNode).Next = ParseStatement(statements[i], symbolTable);
                if ((parsedExpression as StatementNode).Next is StatementNode)
                {
                    parsedExpression = (parsedExpression as StatementNode).Next;
                }
            }

            return parsedTreeRoot;
        }

        private static Node ParseStatement(XElement token, Dictionary<string, NumNode.NumType> symbolTable)
        {
            if (token == null)
            {
                return new EmptyNode();
            }
    
            var subTokens = token.Elements().ToList();

            if (subTokens.Count >= 3 &&
                subTokens[0].Name.LocalName.Equals("mi") &&
                !subTokens[0].IsEmpty &&
                subTokens[1].Name.LocalName.Equals("mo") &&
                !subTokens[1].IsEmpty && subTokens[1].Value == "=")
            {
                var statement = new AssignNode(
                    variable: new VarNode(subTokens[0].Value),
                    expression: ParseExpression(subTokens.Skip(2).ToList())
                );

                // Add symbol with type
                // Create class for symbol table? Will need more information for function declarations
                symbolTable[subTokens[0].Value] = statement.IsFloatPointOperation() ? NumNode.NumType.Float : NumNode.NumType.Int;
                statement.Var.Type = symbolTable[subTokens[0].Value];

                return new StatementNode(statement, new EmptyNode());
            }
            else
            {
                return new StatementNode(ParseExpression(subTokens.ToList()), new EmptyNode());
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
                var cToken = tokens[i];
                switch (cToken.Name.LocalName)
                {
                    // Mrow - groups elements (Found when calling math function)
                    case "mrow":
                        stack.Add(ParseMrow(cToken));
                        continue;
                    // Msup - element power (x^2) Rewrite
                    case "msup":
                        stack.Add(ParseMsup(cToken));
                        continue;
                    // MFrac - division element
                    case "mfrac":
                        stack.Add(ParseMfrac(cToken));
                        continue;
                    // Mo - assignment operator and binary operators
                    case "mo":
                        if (cToken.Value == "(")
                        {
                            // Opening bracket signifies a new term
                            stack.Add(ParseAsTerm(tokens, ref i));
                            continue;
                        } 
                        else if (cToken.Value == ")")
                        {
                            // Closing bracket cant appear because opening bracket parses both as a term
                            throw new Exception("Unexpected closing bracket!");
                        }

                        // Handle binary operations
                        // Check if there is a rightside argument
                        if (i + 1 < tokens.Count)
                        {
                            switch (cToken.Value)
                            {
                                case "*":
                                case "/":
                                    var mul = CreateMultiplicationTree(stack);
                                    // Clear list because we parsed it to one node
                                    stack.Clear(); 
                                    // Parse the next element as a term and continue loop
                                    // Dont use recursion to preserve order of operations
                                    stack.Add(new BinOpNode(mul, cToken.Value, ParseAsTerm(tokens.Skip(++i).ToList(), ref i)));
                                    continue;
                                case "+":
                                case "-":
                                    // Return value because the right side argument handles the rest of the elements
                                    // Use recursion to preserve order of operations
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
                        throw new NotImplementedException("Unsupported MathML tag " + cToken.Name.LocalName);
                }
            }
            return CreateMultiplicationTree(stack);
        }

        /// <summary>
        /// Handles brackets. Parses elements inside brackets as a term
        /// </summary>
        /// <param name="tokens">List of tokens starting with opening bracket</param>
        /// <param name="offsetIndex">Integer thats going to be offset to the end of the term in the specified tokens list</param>
        /// <returns></returns>
        private static Node ParseAsTerm(List<XElement> tokens, ref int offsetIndex)
        {
            if (tokens.Count == 0)
            {
                return new EmptyNode();
            }

            var cToken = tokens[0];
            if (cToken.Value == "(") // Handle brackets
            {
                var closeIndex = tokens.FindIndex((x) => x.Name.LocalName == "mo" && x.Value == ")");
                if (closeIndex == -1)
                {
                    throw new Exception("Missing closing bracket");
                }
                else
                {
                    // Push index to term end
                    offsetIndex += closeIndex;
                    // Parse term as expression
                    return ParseExpression(tokens.GetRange(1, closeIndex - 1));
                }
            }
            else
            {
                return ParseExpression(tokens[0]);
            }
        }

        /// <summary>
        /// Handles mrow elements. Mrow elements contain statements. A math function is also a statement (example: sin(x))
        /// </summary>
        /// <param name="el">Mrow as root element</param>
        /// <returns>Parsed node tree</returns>
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
                    return ParseExpression(children.First());
                case 2:
                    if (isMathFunction(children.First().Value))
                    {
                        return new InvocationNode(
                            children.First().Value,
                            ParseExpression(children.ElementAt(1))
                        );
                    }
                    break;
                default:
                    if (isMathFunction(children.First().Value))
                    {
                        return new InvocationNode(
                            children.First().Value,
                            ParseExpression(children.Skip(2).ToList())
                        );
                    }
                    break;
            }
            return ParseExpression(children.ToList());
        }

        /// <summary>
        /// Handle msup elements.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node ParseMsup(XElement el)
        {
            var supElements = el.Elements();
            int count = supElements.Count();
            if (count == 2)
            {
                var baseEl = supElements.ElementAt(0);
                var supEl = supElements.ElementAt(1);
                return new InvocationNode("pow", new List<Node> { ParseExpression(baseEl), ParseExpression(supEl) });
            }
            else
            {
                throw new Exception("Sup expected 2 elements but got " + count);
            }
        }

        private static Node ParseMfrac(XElement el)
        {
            var elements = el.Elements();
            int count = elements.Count();
            if (count == 2)
            {
                var left = elements.ElementAt(0);
                var right = elements.ElementAt(1);
                return new BinOpNode(ParseExpression(left), "/", ParseExpression(right));
            }
            else
            {
                throw new Exception("Frac expected 2 elements but got " + count);
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
                IXmlLineInfo info = el;
                if (info.HasLineInfo())
                {
                    Console.WriteLine("Warning: Possible error in syntax empty identifier element in XML. Line: {0}", info.LineNumber);
                }

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
                double f;
                if (long.TryParse(el.Value, out l))
                {
                    return new NumNode(l);
                } 
                else if (double.TryParse(el.Value.Replace(',', '.'), out f))
                {
                    return new NumNode(f);
                } 
                else
                {
                    throw new Exception("Failed to parse number node.");
                }
            }
            else
            {
                IXmlLineInfo info = el;
                if (info.HasLineInfo())
                {
                    Console.WriteLine("Warning: Possible error in syntax empty number element in XML. Line: {0}", info.LineNumber);
                }
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
                            temp.RightExpr = new BinOpNode(temp.RightExpr, "*", list[i]);
                            result = temp;
                        }
                    }
                    break;
            }
            return result;
        }
    }
}