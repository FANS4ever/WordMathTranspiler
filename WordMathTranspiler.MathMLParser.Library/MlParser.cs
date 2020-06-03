using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.MathMLParser
{
    public class MlParser
    {
        public static Exception Error(string message, IXmlLineInfo lineInfo)
        {
            return new Exception("[MlParser] - " + message + (lineInfo.HasLineInfo() ? " Line:" + lineInfo.LineNumber : ""));
        }

        #region Tag handlers
        /// <summary>
        /// Handle mi variables.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node HandleMi(MlLexer lex)
        {
            if (!string.IsNullOrWhiteSpace(lex.Node.Value))
            {
                var value = lex.Node.Value;
                lex.Eat("mi");
                return new IdentifierNode(value);
            }
            else
            {
                throw Error("Possible error in syntax. Found empty identifier tag (<mi>) in XML.", lex.GetLineInfo());
            }
        }

        private static Node HandleMtext(MlLexer lex)
        {
            if (!string.IsNullOrWhiteSpace(lex.Node.Value))
            {
                var value = lex.Node.Value;
                lex.Eat("mtext");
                return new IdentifierNode(value);
            }
            else
            {
                throw Error("Possible error in syntax. Found empty identifier tag (<mtext>) in XML.", lex.GetLineInfo());
            }
        }

        /// <summary>
        /// Handle mn elements. Only numbers appear as mn elements.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node HandleMn(MlLexer lex)
        {
            if (!string.IsNullOrWhiteSpace(lex.Node.Value))
            {
                long l;
                double f;
                if (long.TryParse(lex.Node.Value, out l))
                {
                    lex.Eat("mn");
                    return new NumNode(l);
                }
                else if (double.TryParse(lex.Node.Value.Replace(',', '.'), out f))
                {
                    lex.Eat("mn");
                    return new NumNode(f);
                }
                else
                {
                    throw Error("Failed to parse number tag (<mn>).", lex.GetLineInfo());
                }
            }
            else
            {
                throw Error("Possible error in syntax. Found empty number tag (<mn>) in XML.", lex.GetLineInfo());
            }
        }

        private static Node HandleMrow(MlLexer lex)
        {
            var deepLex = lex.GetDeepLexer();
            lex.Eat("mrow");
            return Expr(deepLex);
        }

        private static Node HandleMfenced(MlLexer lex)
        {
            var deepLex = lex.GetDeepLexer();
            lex.Eat("mfenced");
            return Expr(deepLex);
        }

        private static Node HandleMfrac(MlLexer lex)
        {
            MlLexer fracLex = lex.GetDeepLexer();
            lex.Eat("mfrac");

            // Expects 2 <mrow> inside <mfrac>
            var leftLex = fracLex.GetDeepLexer();
            fracLex.Eat("mrow");
            var rightLex = fracLex.GetDeepLexer();
            fracLex.Eat("mrow");

            return new BinOpNode(
                Expr(leftLex), 
                "/", 
                Expr(rightLex)
            );
        }

        private static Node HandleMsup(MlLexer lex)
        {
            MlLexer supLex = lex.GetDeepLexer();
            lex.Eat("msup");

            // Expects 2 <mrow> inside <msup>
            var baseLex = supLex.GetDeepLexer();
            supLex.Eat("mrow");
            var powerLex = supLex.GetDeepLexer();
            supLex.Eat("mrow");

            return new InvocationNode("pow", new List<Node> { 
                Expr(baseLex), 
                Expr(powerLex) 
            });
        }

        private static Node HandleMsqrt(MlLexer lex)
        {
            MlLexer sqrtLex = lex.GetDeepLexer();
            lex.Eat("msqrt");
            return new InvocationNode("sqrt", Expr(sqrtLex));
        }

        private static Node HandleMroot(MlLexer lex)
        {
            MlLexer rootLex = lex.GetDeepLexer();
            lex.Eat("mroot");
            // Expects 2 <mrow> inside <mroot>
            var exprLex = rootLex.GetDeepLexer();
            rootLex.Eat("mrow");
            var powerLex = rootLex.GetDeepLexer();
            rootLex.Eat("mrow");
            return new InvocationNode("root", new List<Node> { 
                Expr(exprLex),
                Expr(powerLex),
            });
        }
        #endregion

        #region AST Build logic
        /// <summary>
        /// factor : REAL (mn)
        ///        | INTEGER (mn)
        ///        | LPAREN expr RPAREN (mo expr mo)
        ///        | variable (mtext, mi)
        ///        | mrow
        ///        | mfrac
        /// </summary>
        /// <param name="lex"></param>
        /// <returns></returns>
        private static Node Factor(MlLexer lex)
        {
            switch (lex.Node.Name)
            {
                case "mn":
                    return HandleMn(lex);
                case "mtext":
                    return HandleMtext(lex);
                case "mi":
                    return HandleMi(lex);
                case "mfenced":
                    return HandleMfenced(lex);
                case "mrow":
                    return HandleMrow(lex);
                case "mo":
                    switch (lex.Node.Value)
                    {
                        case "-":
                        case "+":
                            string value = lex.Node.Value;
                            lex.Eat("mo");
                            return new UnaryOpNode(value, Factor(lex));
                        case "(":
                            lex.Eat("mo");
                            Node node = Expr(lex);
                            lex.Eat("mo"); // Try eating ')'. Test if its a closing bracket?
                            return node;
                        default:
                            throw Error($"Can't factor operator {lex.Node.Value}.", lex.GetLineInfo());
                    }
                case "mfrac":
                    return HandleMfrac(lex);
                case "msup":
                    return HandleMsup(lex);
                case "msqrt":
                    return HandleMsqrt(lex);
                case "mroot":
                    return HandleMroot(lex);
                case "mtable":
                    throw new NotImplementedException("Todo: Add cases support");
                default:
                    throw Error($"Unsupported tag {lex.Node.Name}", lex.GetLineInfo());
            }
        }
        /// <summary>
        /// term : factor ((MUL | DIV) factor)*
        /// </summary>
        /// <param name="lex"></param>
        /// <returns></returns>
        private static Node Term(MlLexer lex)
        {
            Node node = Factor(lex);
            while (!lex.IsFinished && 
                   !(lex.Node.Name == "mo" && 
                     (lex.Node.Value == "+" || lex.Node.Value == "-" || lex.Node.Value == ")")))
            {
                if (lex.Node.Name == "mo")
                {
                    string value = lex.Node.Value;
                    lex.Eat("mo");
                    switch (value)
                    {
                        case "*":
                        case "/":
                            node = new BinOpNode(node, value, Factor(lex));
                            continue;
                        case "\u2061": // FUNCTION APPLICATION operator in unicode
                            IdentifierNode fn = node as IdentifierNode;
                            if (fn == null)
                            {
                                throw Error("Values can only be assigned to a variable node.", lex.GetLineInfo());
                            }

                            // Helper
                            MlLexer UnnestParameterList(MlLexer lex)
                            {
                                if (lex.NodeCount > 1)
                                {
                                    return lex;
                                }
                                switch (lex.Node.Name)
                                {
                                    case "mfenced":
                                        MlLexer mfLex = lex.GetDeepLexer();
                                        if (mfLex.NodeCount == 1)
                                        {
                                            lex.Eat("mfenced");
                                            return UnnestParameterList(mfLex);
                                        }
                                        return mfLex;
                                    case "mrow":
                                        MlLexer mrLex = lex.GetDeepLexer();
                                        if (mrLex.NodeCount == 1)
                                        {
                                            lex.Eat("mrow");
                                            return UnnestParameterList(mrLex);
                                        }
                                        return mrLex;
                                }
                                return lex;
                            }

                            MlLexer argLex = UnnestParameterList(lex.GetDeepLexer());
                            lex.Eat("mrow");
                            List<Node> args = new List<Node>();
                            while (!argLex.IsFinished)
                            {
                                args.Add(Expr(argLex));
                            }
                            node = new InvocationNode(fn.Name, args);
                            continue;
                        case ",":
                            return node;
                    }
                }
                //If not an operator assume multiplication
                node = new BinOpNode(node, "*", Factor(lex)); 
            }

            return node;
        }
        /// <summary>
        /// expr : term ((PLUS | MINUS) term)*
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private static Node Expr(MlLexer lex)
        {
            Node node = Term(lex);

            while (!lex.IsFinished && (lex.Node.Name == "mo" && (lex.Node.Value == "+" || lex.Node.Value == "-")))
            {
                string value = lex.Node.Value;
                lex.Eat("mo"); // Eat "+" | "-"
                node = new BinOpNode(node, value, Term(lex));
            }

            return node;
        }
        private static List<Node> ParameterList(MlLexer lex)
        {
            MlLexer UnnestParameterList(MlLexer lex)
            {
                switch (lex.Node.Name)
                {
                    case "mfenced":
                        MlLexer mfLex = lex.GetDeepLexer();
                        lex.Eat("mfenced");
                        return UnnestParameterList(mfLex);
                    case "mrow":
                        MlLexer mrLex = lex.GetDeepLexer();
                        lex.Eat("mrow");
                        return UnnestParameterList(mrLex);
                    default:
                        return lex;
                }
            }

            Node CreateParameterNode(MlLexer lex)
            {
                switch (lex.Node.Name)
                {
                    case "mtext":
                        return HandleMtext(lex);
                    case "mi":
                        return HandleMi(lex);
                    default:
                        throw Error($"Tag {lex.Node.Name} can't be parsed as a parameter.", lex.GetLineInfo());
                }
            }

            MlLexer paramLex = UnnestParameterList(lex);
            Node node = CreateParameterNode(paramLex);
            List<Node> nodeList = new List<Node>() { node };
            while (!paramLex.IsFinished && paramLex.Node.Name == "mo" && paramLex.Node.Value == ",")
            {
                paramLex.Eat("mo"); // Eat ","
                node = CreateParameterNode(paramLex);
                nodeList.Add(node);
            }

            return nodeList;
        }
        private static Node FunctionDeclaration(MlLexer lex)
        {
            MlLexer mrowLex = lex.GetDeepLexer();
            lex.Eat("mrow");
            IdentifierNode funcNameNode = Factor(mrowLex) as IdentifierNode;
            mrowLex.Eat("mo"); // \u2061 FUNCTION APPLICATION operator in unicode
            List<Node> parameters = ParameterList(mrowLex);
            lex.Eat("mo"); // Eat '='
            return new FuncDeclNode(funcNameNode.Name, parameters, Expr(lex));
        }
        private static Node VariableDeclarationMi(MlLexer lex)
        {
            Node left = HandleMi(lex);
            lex.Eat("mo"); // Eat '='
            Node expr = Expr(lex);
            return new AssignNode((IdentifierNode)left, expr);
        }
        private static Node VariableDeclarationMtext(MlLexer lex)
        {
            Node left = HandleMtext(lex);
            lex.Eat("mo"); // Eat '='
            Node expr = Expr(lex);
            return new AssignNode((IdentifierNode)left, expr);
        }
        private static Node Declarations(MlLexer lex)
        {
            switch (lex.Node.Name)
            {
                case "mi":
                    return VariableDeclarationMi(lex);
                case "mtext":
                    return VariableDeclarationMtext(lex);
                case "mrow":
                    return FunctionDeclaration(lex);
                default:
                    throw Error($"Can't assign to tag <{lex.Node.Name}>", lex.GetLineInfo());
            }
        }
        private static Node StatementList(MlLexer lex, bool ignoreBrokenStatements = false)
        {
            Node root = new EmptyNode();
            Node prevNode = new EmptyNode();
            while (!lex.IsFinished && lex.Node.Name == "math")
            {
                try
                {
                    MlLexer statementLex = lex.GetDeepLexer();
                    lex.Eat("math");
                    Node current = new EmptyNode();
                    var laNode = statementLex.Peek();
                    if (laNode != null &&
                        laNode.Name == "mo" &&
                        laNode.Value == "=")
                    {
                        current = new StatementNode(Declarations(statementLex), new EmptyNode());
                    }
                    else
                    {
                        current = new StatementNode(Expr(statementLex), new EmptyNode());
                    }

                    root = root is EmptyNode ? current : root;
                    if (prevNode is EmptyNode)
                    {
                        prevNode = current;
                    }
                    else
                    {
                        ((StatementNode)prevNode).Next = current;
                        prevNode = current;
                    }
                }
                catch (Exception e)
                {
                    if (ignoreBrokenStatements)
                    {
                        Console.WriteLine("[SKIPSTATEMENT][ERROR] {0}", e.Message);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }

            return root;
        }
        #endregion
        public static Node Parse(XElement root, bool ignoreBrokenStatements = false)
        {
            MlLexer lex = new MlLexer(root);
            return StatementList(lex, ignoreBrokenStatements);
        }
        public static Node Parse(string path, bool ignoreBrokenStatements = false)
        {
            return Parse(XDocument.Load(path, LoadOptions.SetLineInfo).Root, ignoreBrokenStatements);
        }
    }
}