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
            return new Exception("[MlParser]" + message + (lineInfo.HasLineInfo() ? " Line:" + lineInfo.LineNumber : ""));
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
                return new VarNode(value);
            }
            else
            {
                throw Error("[Error] - Possible error in syntax. Found empty identifier tag (<mi>) in XML.", lex.GetLineInfo());
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
                    throw Error("[Error] - Failed to parse number tag (<mn>).", lex.GetLineInfo());
                }
            }
            else
            {
                throw Error("[Error] - Possible error in syntax. Found empty number tag (<mn>) in XML.", lex.GetLineInfo());
            }
        }

        private static Node HandleMrow(MlLexer lex)
        {
            var deepLex = lex.GetDeepLexer();
            lex.Eat("mrow");
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

            return new InvocationNode(
                "pow", 
                new List<Node> { 
                    Expr(baseLex), 
                    Expr(powerLex) 
                }
            );
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
            Node node = new EmptyNode();
            switch (lex.Node.Name)
            {
                case "mn":
                    node = HandleMn(lex);
                    return node;
                case "mtext":
                case "mi":
                    node = HandleMi(lex);
                    return node;
                case "mrow":
                    node = HandleMrow(lex);
                    return node;
                case "mo":
                    if (lex.Node.Value == "(")
                    {
                        lex.Eat("mo");
                        node = Expr(lex);
                        lex.Eat("mo"); // Try eating ')'. Test if its a closing bracket?
                        return node;
                    }
                    Console.WriteLine("[Warning] Factor function got <mo> element thats not a bracket.");
                    break;
                case "mfrac":
                    node = HandleMfrac(lex);
                    return node;
                case "msup":
                    node = HandleMsup(lex);
                    return node;
            }
            return node;
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
                        case "\u2061":
                            VarNode fn = node as VarNode;
                            if (fn == null)
                            {
                                throw new Exception("Error - Values can only be assigned to a variable node");
                            }
                            node = new InvocationNode(fn.Name, Factor(lex));
                            continue;
                    }
                }

                node = new BinOpNode(node, "*", Factor(lex)); //If no operator assume multiplication
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
                lex.Eat("mo");
                node = new BinOpNode(node, value, Term(lex));
            }

            return node;
        }
        private static Node Assignment(MlLexer lex)
        {
            Node left = HandleMi(lex);
            //string op = lex.Current.Value; //Add test to see if theres an assignment operator?
            lex.Eat("mo"); // Eat '='
            Node expr = Expr(lex);
            return new AssignNode((VarNode)left, expr);
        }
        private static Node StatementList(MlLexer lex)
        {
            Node root = new EmptyNode();
            Node prevNode = new EmptyNode();
            while (!lex.IsFinished && lex.Node.Name == "math")
            {
                MlLexer statementLex = lex.GetDeepLexer();
                lex.Eat("math");
                Node current = new EmptyNode();
                var laNode = statementLex.LookAhead();
                if (laNode != null &&
                    laNode.Name == "mo" &&
                    laNode.Value == "=")
                {
                    current = new StatementNode(Assignment(statementLex), new EmptyNode());
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

            return root;
        }
        #endregion
        public static Node Parse(XElement root)
        {
            MlLexer lex = new MlLexer(root);
            return StatementList(lex);
        }
        public static Node Parse(string path)
        {
            return Parse(XDocument.Load(path, LoadOptions.SetLineInfo).Root);
        }
    }
}