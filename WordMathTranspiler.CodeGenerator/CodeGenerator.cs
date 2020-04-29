using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.CodeGenerator
{
    public class CodeGenerator
    {
        private AdhocWorkspace workspace;
        private SyntaxGenerator generator;

        public CodeGenerator()
        {
            Init();
        }

        public void Init()
        {
            this.workspace = new AdhocWorkspace();
            this.generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        }

        public SyntaxTree Generate(Node root)
        {
            var namespaceNode   = generator.NamespaceDeclaration("TranspiledMML", BuildProgram(root));
            var usingDirectives = generator.NamespaceImportDeclaration("System");
            var compilationUnit = generator.CompilationUnit(usingDirectives, namespaceNode).NormalizeWhitespace();

            Console.WriteLine(compilationUnit);
            return compilationUnit.SyntaxTree;
        }

        private SyntaxNode BuildProgram(Node root)
        {
            if (root is StatementNode)
            {
                List<SyntaxNode> declared = new List<SyntaxNode>();
                var currentStatement = (root as StatementNode);

                do
                {
                    declared.Add(RecursiveVisit(currentStatement.Body, ref declared));
                    declared.Add(CreateConsole(generator, RecursivePrintStatement(currentStatement.Body)));

                    if (currentStatement.Type == StatementNode.StatementType.DeclarationStatement)
                    {
                        string varName = (currentStatement.Body as AssignNode).Var.Name;
                        declared.Add(CreateConsole(generator, varName + " = {0}", new string[] { varName }));
                    }
                    else
                    {
                        declared.Add(CreateConsole(generator, "A = {0}", new string[] { "A" }));
                    }

                    currentStatement = currentStatement.Next as StatementNode;
                } while (currentStatement is StatementNode);

                var mainMethodNode = CreateEntryPoint(generator, declared);
                var classNode = CreateClass(generator, "Program", new SyntaxNode[] { mainMethodNode });
                return classNode;
            }
            else
            {
                throw new Exception("Expected root StatementNode but got " + root.GetType());
            }
        }

        /// <summary>
        /// Recursivelly builds C# code by traversing ast
        /// </summary>
        /// <param name="root">Root node of an AST tree for a statement</param>
        private SyntaxNode RecursiveVisit(Node root, ref List<SyntaxNode> declared)
        {
            switch (root)
            {
                case EmptyNode emptyNode:
                    throw new Exception("Unexpected empty node inside statement");

                case NumNode numNode:
                    return generator.LiteralExpression(numNode.Value);

                case AssignNode assignNode:
                    return generator.LocalDeclarationStatement(
                        generator.TypeExpression(assignNode.IsFloatPointOperation() ? SpecialType.System_Double : SpecialType.System_Int64),
                        assignNode.Var.Name,
                        RecursiveVisit(assignNode.Expr, ref declared)
                    );
                case InvocationNode mathFNode:
                    var identifier = generator.IdentifierName("Math");
                    var expression = generator.MemberAccessExpression(identifier, "Sin");
                    SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();
                    foreach (var arg in mathFNode.Args)
                    {
                        //var nameExpression = generator.IdentifierName(argName);
                        var nameArg = generator.Argument(RecursiveVisit(arg, ref declared));
                        arguments = arguments.Add(nameArg);
                    }
                    return generator.InvocationExpression(expression, arguments);
                case BinOpNode operatorNode:
                    switch (operatorNode.Op)
                    {
                        case "+":
                            return generator.AddExpression(
                                RecursiveVisit(operatorNode.LeftExpr, ref declared),
                                RecursiveVisit(operatorNode.RightExpr, ref declared)
                            );
                        case "-":
                            return generator.SubtractExpression(
                                RecursiveVisit(operatorNode.LeftExpr, ref declared),
                                RecursiveVisit(operatorNode.RightExpr, ref declared)
                            );  
                        case "*":
                            return generator.MultiplyExpression(
                                 RecursiveVisit(operatorNode.LeftExpr, ref declared),
                                 RecursiveVisit(operatorNode.RightExpr, ref declared)
                            );
                        case "/":
                            return generator.DivideExpression(
                                generator.CastExpression(
                                    generator.TypeExpression(SpecialType.System_Double),
                                    RecursiveVisit(operatorNode.LeftExpr, ref declared)
                                ),
                                RecursiveVisit(operatorNode.RightExpr, ref declared)
                            );
                        default:
                            throw new NotImplementedException("Operator " + operatorNode.Op + " is not implemented yet.");
                    }
                case SupNode supNode:
                    //RecursiveVisit(supNode.baseEl);
                    //RecursiveVisit(supNode.supEl);
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException("Node type not implemented yet.");
            }
        }

        /// <summary>
        /// Parses an AST tree for a statement to a human readable format.
        /// </summary>
        /// <param name="root">Root node of an AST tree for a statement</param>
        /// <returns>human readable string</returns>
        private static string RecursivePrintStatement(Node root)
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
                    return RecursivePrintStatement(assignNode.Var) + " = " + RecursivePrintStatement(assignNode.Expr);
                case InvocationNode mathFNode:
                    string result = mathFNode.Fn + "(";
                    for (int i = 0; i < mathFNode.Args.Count; i++)
                    {
                        var arg = mathFNode.Args[i];
                        result += RecursivePrintStatement(arg) + (i != mathFNode.Args.Count - 1 ? ", " : "");
                    }
                    return result + ")";
                case BinOpNode operatorNode:
                    return "(" + RecursivePrintStatement(operatorNode.LeftExpr) + ") " + operatorNode.Op.ToString() + " (" + RecursivePrintStatement(operatorNode.RightExpr) + ")";
                case SupNode supNode:
                    return "(" + RecursivePrintStatement(supNode.Base) + ")^(" + RecursivePrintStatement(supNode.Sup) + ")";
                default:
                    throw new NotImplementedException("Node type not implemented yet.");
            }

        }

        #region SyntaxNode creation helpers
        /// <summary>
        /// Creates a syntax node that represents the main method
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="statements"></param>
        /// <returns></returns>
        public static SyntaxNode CreateEntryPoint(SyntaxGenerator generator, IEnumerable<SyntaxNode> statements)
        {
            var mainMethod = generator.MethodDeclaration(
                name: "Main",
                modifiers: DeclarationModifiers.Static,
                statements: statements
            );

            return mainMethod;
        }
        /// <summary>
        /// Creates a SyntaxNode that represents a class
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="className"></param>
        /// <param name="members"></param>
        /// <returns></returns>
        public static SyntaxNode CreateClass(SyntaxGenerator generator, string className, IEnumerable<SyntaxNode> members)
        {
            return generator.ClassDeclaration(
              name: className, 
              typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.None,
              baseType: null,
              interfaceTypes: null,
              members: members
            );
        }
        /// <summary>
        /// Creates a syntax node that represents a Console.WriteLine
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="format"></param>
        /// <param name="argumentNames"></param>
        /// <returns></returns>
        public static SyntaxNode CreateConsole(SyntaxGenerator generator, object format = null, IEnumerable<string> argumentNames = null)
        {
            var identifier = generator.IdentifierName("Console");
            var expression = generator.MemberAccessExpression(identifier, "WriteLine");

            SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();

            var stringExpression = generator.LiteralExpression(format);
            var stringArg = generator.Argument(stringExpression);
            arguments = arguments.Add(stringArg);

            if (argumentNames != null)
            {
                foreach (var argName in argumentNames)
                {
                    var nameExpression = generator.IdentifierName(argName);
                    var nameArg = generator.Argument(nameExpression);
                    arguments = arguments.Add(nameArg);
                }
            }
            
            return generator.InvocationExpression(expression, arguments);
        }
        #endregion
    }
}
