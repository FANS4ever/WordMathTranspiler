using Microsoft.CodeAnalysis;
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
            var usingDirectives = generator.NamespaceImportDeclaration("System");
            var namespaceNode   = generator.NamespaceDeclaration("TranspiledMML", BuildClass(root));
            var compilationUnit = generator.CompilationUnit(usingDirectives, namespaceNode).NormalizeWhitespace();

            Console.WriteLine(compilationUnit);
            return compilationUnit.SyntaxTree;
        }
        private SyntaxNode BuildClass(Node root)
        {
            List<SyntaxNode> methodDeclarations = BuildMethods(root);
            // Add custom name setting
            return generator.ClassDeclaration(
                name: "Program",
                typeParameters: null,
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.None,
                baseType: null,
                interfaceTypes: null,
                members: methodDeclarations
            );
        }
        private List<SyntaxNode> BuildMethods(Node root)
        {
            if (root is StatementNode)
            {
                List<SyntaxNode> classContext = new List<SyntaxNode>();
                List<SyntaxNode> mainContext = new List<SyntaxNode>();
                StatementNode currentStatement = root as StatementNode;
                while (currentStatement != null)
                {
                    switch (currentStatement.Body)
                    {
                        case AssignNode assignNode:
                            {
                                string methodName = $"Init{assignNode.Var.Name}";
                                SyntaxNode method = generator.MethodDeclaration(
                                    name: methodName,
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static,
                                    returnType: generator.TypeExpression(SpecialType.System_Double),
                                    statements: new SyntaxNode[] { generator.ReturnStatement(VisitExpression(assignNode.Expr)) }
                                );

                                //Define variable as a field and assign method as value.
                                //Add getter if order of math execution isnt important
                                SyntaxNode field = generator.FieldDeclaration(
                                    name: assignNode.Var.Name,
                                    type: generator.TypeExpression(SpecialType.System_Double),
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static,
                                    initializer: generator.InvocationExpression(generator.IdentifierName(methodName))
                                );
                                classContext.Add(field);
                                classContext.Add(method);

                                SyntaxNode console = CreateConsole(generator, $"{assignNode.Var.Name} = {{0}}", assignNode.Var.Name);
                                mainContext.Add(console);
                                break;
                            }
                        case FuncDeclNode funcDeclNode:
                            {
                                string methodName = $"Func{funcDeclNode.Name}";

                                SyntaxList<SyntaxNode> parameters = new SyntaxList<SyntaxNode>();
                                foreach (IdentifierNode node in funcDeclNode.Params)
                                {
                                    parameters = parameters.Add(
                                        generator.ParameterDeclaration(
                                            name: node.Name,
                                            type: generator.TypeExpression(SpecialType.System_Double)
                                        )
                                    );
                                }

                                SyntaxNode method = generator.MethodDeclaration(
                                    name: methodName,
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static,
                                    returnType: generator.TypeExpression(SpecialType.System_Double),
                                    parameters: parameters,
                                    statements: new SyntaxNode[] { generator.ReturnStatement(VisitExpression(funcDeclNode.Body)) }
                                );
                                classContext.Add(method);
                                break;
                            }
                        default:
                            {
                                SyntaxNode printFunction = CreateConsole(generator, currentStatement.Body.TextPrint());
                                SyntaxNode printValue = CreateConsole(generator, VisitExpression(currentStatement.Body));
                                mainContext.Add(printFunction);
                                mainContext.Add(printValue);
                                break;
                            }
                    }
                    currentStatement = currentStatement.Next as StatementNode;
                }

                // Create main method
                SyntaxNode main = generator.MethodDeclaration(
                    name: "Main",
                    modifiers: DeclarationModifiers.Static,
                    statements: mainContext
                );
                classContext.Add(main);

                return classContext;


                //do
                //{
                //    var compiledStatement = VisitNode(currentStatement.Body, ref builtMethods);
                //    builtMethods.Add(CreateConsole(generator, "Evaluating -> " + currentStatement.Body.TextPrint()));
                //    if (currentStatement.Type == StatementNode.StatementType.DeclarationStatement)
                //    {
                //        builtMethods.Add(compiledStatement);
                //        string varName = (currentStatement.Body as AssignNode).Var.Name;
                //        builtMethods.Add(CreateConsole(generator, varName + " = {0}", new string[] { varName }));
                //    }
                //    else
                //    {
                //        // Non declaration statements get printed to screen
                //        builtMethods.Add(CreateConsole(generator, compiledStatement));
                //    }

                //    currentStatement = currentStatement.Next as StatementNode;
                //} while (currentStatement is StatementNode);

                //// Create main method
                //SyntaxNode mainMethod = CreateEntryPoint(generator, mainMethodStatements);
                //methodDeclarations.Add(mainMethod);
                //return ;
            }
            else
            {
                throw new Exception("Expected root StatementNode but got " + root.GetType());
            }
        }
        #region Visitor methods
        private SyntaxNode VisitExpression(Node root)
        {
            switch (root)
            {
                case EmptyNode emptyNode:
                    throw new Exception("Unexpected empty node inside statement");
                case NumNode numNode:
                    return VisitNumNode(numNode);
                case IdentifierNode identNode:
                    return VisitIdentifierNode(identNode);
                case InvocationNode invocationNode:
                    return VisitInvocationNode(invocationNode);
                case BinOpNode opNode:
                    return VisitBinOpNode(opNode);
                case UnaryOpNode uOpNode:
                    return VisitUnaryOpNode(uOpNode);
                default:
                    throw new NotImplementedException("Node type not implemented yet.");
            }
        }
        private SyntaxNode VisitBinOpNode(BinOpNode node)
        {
            switch (node.Op)
            {
                case "+":
                    return generator.AddExpression(
                        VisitExpression(node.LeftExpr),
                        VisitExpression(node.RightExpr)
                    );
                case "-":
                    return generator.SubtractExpression(
                        VisitExpression(node.LeftExpr),
                        VisitExpression(node.RightExpr)
                    );
                case "*":
                    return generator.MultiplyExpression(
                         VisitExpression(node.LeftExpr),
                         VisitExpression(node.RightExpr)
                    );
                case "/":
                    return generator.DivideExpression(
                        generator.CastExpression(
                            generator.TypeExpression(SpecialType.System_Double),
                            VisitExpression(node.LeftExpr)
                        ),
                        VisitExpression(node.RightExpr)
                    );
                default:
                    throw new NotImplementedException("Operator " + node.Op + " is not implemented yet.");
            }
        }
        private SyntaxNode VisitInvocationNode(InvocationNode node)
        {
            if (node.IsBuiltinFunction)
            {
                var identifier = generator.IdentifierName("Math");
                var expression = generator.MemberAccessExpression(
                    identifier,
                    node.Fn?.Substring(0, 1).ToString().ToUpper() + node.Fn?.Substring(1).ToLower() //First letter to uppercase
                );

                SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();
                foreach (var arg in node.Args)
                {
                    //Identifier for var nodes?
                    //var nameExpression = generator.IdentifierName(argName);
                    var nameArg = generator.Argument(VisitExpression(arg));
                    arguments = arguments.Add(nameArg);
                }
                return generator.InvocationExpression(expression, arguments);
            }
            else
            {
                throw new NotImplementedException("Custom functions not implemented yet.");
            }
        }
        private SyntaxNode VisitUnaryOpNode(UnaryOpNode node)
        {
            switch (node.Op)
            {
                case "+":
                    return VisitExpression(node.Expr);
                case "-":
                    return generator.NegateExpression(VisitExpression(node.Expr));
                default:
                    throw new NotImplementedException();
            }
        }
        private SyntaxNode VisitIdentifierNode(IdentifierNode node)
        {
            switch (node.Name)
            {
                case "π": // 3.14...
                    return generator.MemberAccessExpression(
                        generator.IdentifierName("Math"), "PI"
                    );
                default:
                    return generator.IdentifierName(node.Name);
            }
        }
        private SyntaxNode VisitNumNode(NumNode node)
        {
            return generator.LiteralExpression(node.Value);
        }
        #endregion

        #region SyntaxNode creation helpers
        /// <summary>
        /// Creates a syntax node that represents a Console.WriteLine
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="format"></param>
        /// <param name="argumentNames"></param>
        /// <returns></returns>
        public static SyntaxNode CreateConsole(SyntaxGenerator generator, SyntaxNode identifierNode)
        {
            var identifier = generator.IdentifierName("Console");
            var expression = generator.MemberAccessExpression(identifier, "WriteLine");
            return generator.InvocationExpression(expression, identifierNode);
        }
        public static SyntaxNode CreateConsole(SyntaxGenerator generator, string format, params string[] identifiers)
        {
            var identifier = generator.IdentifierName("Console");
            var expression = generator.MemberAccessExpression(identifier, "WriteLine");
           
            SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();

            // Create the string argument 
            var stringExpression = generator.LiteralExpression(format);
            var stringArg = generator.Argument(stringExpression);
            arguments = arguments.Add(stringArg);

            // Create the template arguments
            foreach (var argName in identifiers)
            {
                var nameExpression = generator.IdentifierName(argName);
                var nameArg = generator.Argument(nameExpression);
                arguments = arguments.Add(nameArg);
            }

            return generator.InvocationExpression(expression, arguments);
        }
        #endregion
    }
}
