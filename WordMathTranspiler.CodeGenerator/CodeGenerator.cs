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
            workspace = new AdhocWorkspace();
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
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
                List<SyntaxNode> compiledStatements = new List<SyntaxNode>();
                var currentStatement = (root as StatementNode);

                do
                {
                    var compiledStatement = RecursiveVisit(currentStatement.Body, ref compiledStatements);
                    compiledStatements.Add(CreateConsole(generator, "Evaluating -> " + currentStatement.Body.TextPrint()));
                    if (currentStatement.Type == StatementNode.StatementType.DeclarationStatement)
                    {
                        compiledStatements.Add(compiledStatement);
                        string varName = (currentStatement.Body as AssignNode).Var.Name;
                        compiledStatements.Add(CreateConsole(generator, varName + " = {0}", new string[] { varName }));
                    }
                    else
                    {
                        // Non declaration statements get printed to screen
                        compiledStatements.Add(CreateConsole(generator, compiledStatement));
                    }

                    currentStatement = currentStatement.Next as StatementNode;
                } while (currentStatement is StatementNode);

                var mainMethodNode = CreateEntryPoint(generator, compiledStatements);
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
                case IdentifierNode varNode:
                    switch (varNode.Name)
                    {
                        case "π": // 3.14...
                            return generator.MemberAccessExpression(
                                generator.IdentifierName("Math"), "PI"
                            );
                        default:
                            return generator.IdentifierName(varNode.Name);
                    }
                case AssignNode assignNode:
                    return generator.LocalDeclarationStatement(
                        generator.TypeExpression(assignNode.IsFloatPointOperation() ? SpecialType.System_Double : SpecialType.System_Int64),
                        assignNode.Var.Name,
                        RecursiveVisit(assignNode.Expr, ref declared)
                    );
                case InvocationNode invocationNode:
                    if (invocationNode.IsBuiltinFunction)
                    {
                        var identifier = generator.IdentifierName("Math");
                        var expression = generator.MemberAccessExpression(
                            identifier,
                            invocationNode.Fn?.Substring(0, 1).ToString().ToUpper() + invocationNode.Fn?.Substring(1).ToLower() //First letter to uppercase
                        );

                        SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();
                        foreach (var arg in invocationNode.Args)
                        {
                            //Identifier for var nodes?
                            //var nameExpression = generator.IdentifierName(argName);
                            var nameArg = generator.Argument(RecursiveVisit(arg, ref declared));
                            arguments = arguments.Add(nameArg);
                        }
                        return generator.InvocationExpression(expression, arguments);
                    }
                    else
                    {
                        throw new NotImplementedException("Custom functions not implemented yet.");
                    }
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

            // If syntax node is passed as the format argument print the syntax node
            if (format is SyntaxNode)
            {
                return generator.InvocationExpression(expression, new SyntaxNode[] { format as SyntaxNode });
            }
            else
            {
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
        }
        #endregion
    }
}
