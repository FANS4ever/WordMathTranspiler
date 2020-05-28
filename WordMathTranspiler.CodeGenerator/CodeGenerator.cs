using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using WordMathTranspiler.MathMLParser.Nodes;
using WordMathTranspiler.MathMLParser.Nodes.Data;
using WordMathTranspiler.MathMLParser.Nodes.Structure;

namespace WordMathTranspiler.CodeGenerator
{
    public class CodeGenerator
    {
        private AdhocWorkspace workspace;
        private SyntaxGenerator generator;
        private Dictionary<string, int> symbolTable;

        public CodeGenerator()
        {
            Init();
        }
        public void Init()
        {
            workspace = new AdhocWorkspace();
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
            symbolTable = new Dictionary<string, int>();
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
                List<SyntaxNode> classContext = new List<SyntaxNode>() { CreateReadInputMethod(generator), CreateNthRootMethod(generator) };
                List<SyntaxNode> mainContext = new List<SyntaxNode>();

                SyntaxNode printStart = CreateConsole(
                    generator,
                    $"Simulation start."
                );
                mainContext.Add(printStart);

                StatementNode currentStatement = root as StatementNode;
                while (currentStatement != null)
                {
                    switch (currentStatement.Body)
                    {
                        case AssignNode assignNode:
                            {
                                SyntaxNode printExpression = CreateConsole(
                                    generator,
                                    $"Evaluating: {currentStatement.Body.TextPrint()}"
                                );
                                mainContext.Add(printExpression);

                                string methodName = $"Init{assignNode.Var.Name}";
                                SyntaxNode method = generator.MethodDeclaration(
                                    name: methodName,
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static,
                                    returnType: generator.TypeExpression(SpecialType.System_Double),
                                    statements: new SyntaxNode[] { generator.ReturnStatement(BuildExpression(assignNode.Expr)) }
                                );

                                //Define variable as a field and assign method as value.
                                //Add getter if order of math execution isnt important
                                //Readonly because we can't reassign a static variable in class context (if we did it in main it would cause bugs when used as a lib)
                                SyntaxNode field = generator.FieldDeclaration(
                                    name: assignNode.Var.Name,
                                    type: generator.TypeExpression(SpecialType.System_Double),
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static + DeclarationModifiers.ReadOnly, 
                                    initializer: generator.InvocationExpression(generator.IdentifierName(methodName))
                                );
                                classContext.Add(field);
                                classContext.Add(method);
                                symbolTable.Add(assignNode.Var.Name, 1);

                                SyntaxNode printResult = CreateConsole(generator, $"{assignNode.Var.Name} = {{0}}\n", assignNode.Var.Name);
                                mainContext.Add(printResult);
                                break;
                            }
                        case FuncDeclNode funcDeclNode:
                            {
                                SyntaxNode printDefinition = CreateConsole(
                                    generator,
                                    $"Define function: {currentStatement.Body.TextPrint()}\n"
                                );
                                mainContext.Add(printDefinition);

                                string methodName = $"Func{funcDeclNode.Name}";

                                // Create local symbol table
                                var globalSymbolTable = symbolTable;
                                symbolTable = new Dictionary<string, int>();
                                globalSymbolTable.ToList().ForEach(x => symbolTable.Add(x.Key, x.Value));

                                SyntaxList<SyntaxNode> parameters = new SyntaxList<SyntaxNode>();
                                foreach (IdentifierNode node in funcDeclNode.Params)
                                {
                                    parameters = parameters.Add(
                                        generator.ParameterDeclaration(
                                            name: node.Name,
                                            type: generator.TypeExpression(SpecialType.System_Double)
                                        )
                                    );

                                    symbolTable.Add(node.Name, 1);
                                }

                                SyntaxNode method = generator.MethodDeclaration(
                                    name: methodName,
                                    accessibility: Accessibility.Public,
                                    modifiers: DeclarationModifiers.Static,
                                    returnType: generator.TypeExpression(SpecialType.System_Double),
                                    parameters: parameters,
                                    statements: new SyntaxNode[] { generator.ReturnStatement(BuildExpression(funcDeclNode.Body)) }
                                );
                                classContext.Add(method);
                                // Return to global symbol table
                                symbolTable = globalSymbolTable;
                                break;
                            }
                        default:
                            {
                                SyntaxNode printExpression = CreateConsole(
                                    generator,
                                    $"Evaluating: {currentStatement.Body.TextPrint()}"
                                );
                                SyntaxNode printEvaluation = CreateConsole(
                                    generator, 
                                    $"Answer = {{0}}\n", 
                                    new SyntaxNode[] { BuildExpression(currentStatement.Body) }
                                );
                                mainContext.Add(printExpression);
                                mainContext.Add(printEvaluation);
                                break;
                            }
                    }
                    currentStatement = currentStatement.Next as StatementNode;
                }
                SyntaxNode printFinished = CreateConsole(
                    generator,
                    $"Simulation finish."
                );
                mainContext.Add(printFinished);

                // Create main method
                SyntaxNode main = generator.MethodDeclaration(
                    name: "Main",
                    modifiers: DeclarationModifiers.Static,
                    statements: mainContext
                );
                classContext.Add(main);

                return classContext;
            }
            else
            {
                throw new Exception("Expected root StatementNode but got " + root.GetType());
            }
        }
        private SyntaxNode BuildExpression(Node root)
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
        #region Visitor methods
        private SyntaxNode VisitBinOpNode(BinOpNode node)
        {
            switch (node.Op)
            {
                case "+":
                    return generator.AddExpression(
                        BuildExpression(node.LeftExpr),
                        BuildExpression(node.RightExpr)
                    );
                case "-":
                    return generator.SubtractExpression(
                        BuildExpression(node.LeftExpr),
                        BuildExpression(node.RightExpr)
                    );
                case "*":
                    return generator.MultiplyExpression(
                         BuildExpression(node.LeftExpr),
                         BuildExpression(node.RightExpr)
                    );
                case "/":
                    return generator.DivideExpression(
                        generator.CastExpression(
                            generator.TypeExpression(SpecialType.System_Double),
                            BuildExpression(node.LeftExpr)
                        ),
                        BuildExpression(node.RightExpr)
                    );
                default:
                    throw new NotImplementedException("Operator " + node.Op + " is not implemented yet.");
            }
        }
        private SyntaxNode VisitInvocationNode(InvocationNode node)
        {
            SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();
            foreach (var arg in node.Args)
            {
                var nameArg = generator.Argument(BuildExpression(arg));
                arguments = arguments.Add(nameArg);
            }

            if (node.IsBuiltinFunction)
            {
                return generator.InvocationExpression(
                    expression: generator.MemberAccessExpression(
                        expression: generator.IdentifierName("Math"),
                        memberName: node.Fn?.Substring(0, 1).ToString().ToUpper() + node.Fn?.Substring(1).ToLower() //First letter to uppercase
                    ),
                    arguments: arguments
                );
            }
            else if (node.Fn == "root") 
            {
                return generator.InvocationExpression(
                    expression: generator.IdentifierName("NthRoot"),
                    arguments: arguments
                );
            }
            else
            {
                return generator.InvocationExpression(
                    expression: generator.IdentifierName($"Func{node.Fn}"), 
                    arguments: arguments
                );
            }
        }
        private SyntaxNode VisitUnaryOpNode(UnaryOpNode node)
        {
            switch (node.Op)
            {
                case "+":
                    return BuildExpression(node.Expr);
                case "-":
                    return generator.NegateExpression(BuildExpression(node.Expr));
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
                    if (symbolTable.ContainsKey(node.Name))
                    {
                        return generator.IdentifierName(node.Name);
                    }
                    else 
                    {
                        return generator.InvocationExpression(
                            expression: generator.IdentifierName("ReadInput"), 
                            generator.Argument(
                                generator.LiteralExpression($"Enter value for {node.Name}: ")
                            )
                        );
                    }
            }
        }
        private SyntaxNode VisitNumNode(NumNode node)
        {
            return generator.LiteralExpression(node.Value);
        }
        #endregion

        #region SyntaxNode creation helpers
        public static SyntaxNode CreateReadInputMethod(SyntaxGenerator generator) {
            var identifier = generator.IdentifierName("Console");
            var expression = generator.MemberAccessExpression(identifier, "ReadLine");

            // double val
            var val = generator.LocalDeclarationStatement(
                type: generator.TypeExpression(SpecialType.System_Double),
                "val"
            );

            // double.TryParse(Console.ReadLine(), out val)
            var tryParse = generator.InvocationExpression(
                expression: generator.MemberAccessExpression(
                    generator.TypeExpression(SpecialType.System_Double),
                    "TryParse"
                ),
                arguments: new SyntaxNode[] { 
                    generator.InvocationExpression(expression),
                    generator.Argument(
                        RefKind.Out,
                        generator.IdentifierName("val")
                    ) 
                } 
            );

            // // If(double.TryParse(Console.ReadLine(), out val))
            // //      return val;
            // // else
            // //      throw new ArgumentException("Value must be a number")
            //var ifStatement = generator.IfStatement(
            //    condition: tryParse,
            //    trueStatements: new SyntaxNode[] { 
            //        generator.ReturnStatement(generator.IdentifierName("val"))
            //    },
            //    falseStatements: new SyntaxNode[] { 
            //        generator.ThrowStatement(
            //            generator.ObjectCreationExpression(
            //                type: generator.IdentifierName("ArgumentException"),
            //                arguments: new SyntaxNode[] { generator.LiteralExpression("Value must be a number!") }
            //            )
            //        )
            //    }
            //);

            var whileLoop = generator.WhileStatement(
                condition: generator.LogicalNotExpression(tryParse),
                statements: new SyntaxNode[] {
                    CreateConsole(generator, generator.LiteralExpression("Value must be a number!")),
                    CreateConsole(generator, generator.IdentifierName("message"))
                }
            );

            SyntaxNode method = generator.MethodDeclaration(
                name: "ReadInput",
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.Static,
                parameters: new SyntaxNode[] {
                    generator.ParameterDeclaration(
                        name: "message",
                        type: generator.TypeExpression(SpecialType.System_String)
                    ) 
                },
                returnType: generator.TypeExpression(SpecialType.System_Double),
                statements: new SyntaxNode[] { 
                    CreateConsole(generator, generator.IdentifierName("message")), 
                    val,
                    whileLoop,
                    generator.ReturnStatement(generator.IdentifierName("val"))
                }
            );

            return method;
        }
        public static SyntaxNode CreateReadInput(SyntaxGenerator generator, string message)
        {
            var identifier = generator.IdentifierName("ReadInput");
            var argument = generator.Argument(
                generator.LiteralExpression(message)
            );
            return generator.InvocationExpression(identifier, argument);
        }
        public static SyntaxNode CreateNthRootMethod(SyntaxGenerator generator)
        {
            //static double NthRoot(double x, double n)
            //{
            //    return Math.Pow(x, 1.0 / n);
            //}
            SyntaxNode method = generator.MethodDeclaration(
                name: "NthRoot",
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.Static,
                parameters: new SyntaxNode[] {
                    generator.ParameterDeclaration(
                        name: "x",
                        type: generator.TypeExpression(SpecialType.System_Double)
                    ),
                    generator.ParameterDeclaration(
                        name: "n",
                        type: generator.TypeExpression(SpecialType.System_Double)
                    )
                },
                returnType: generator.TypeExpression(SpecialType.System_Double),
                statements: new SyntaxNode[] {
                    generator.ReturnStatement(
                        generator.InvocationExpression(
                            expression: generator.MemberAccessExpression(
                                expression: generator.IdentifierName("Math"),
                                memberName: "Pow"
                            ),
                            generator.Argument(generator.IdentifierName("x")),
                            generator.Argument(generator.DivideExpression(
                                left: generator.LiteralExpression(1.0),
                                right: generator.IdentifierName("n")
                            ))
                        )
                    )
                }
            );
            return method;
        }

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
        public static SyntaxNode CreateConsole(SyntaxGenerator generator, string format, SyntaxNode[] identifiers)
        {
            var identifier = generator.IdentifierName("Console");
            var expression = generator.MemberAccessExpression(identifier, "WriteLine");

            SyntaxList<SyntaxNode> arguments = new SyntaxList<SyntaxNode>();

            // Create the string argument 
            var stringExpression = generator.LiteralExpression(format);
            var stringArg = generator.Argument(stringExpression);
            arguments = arguments.Add(stringArg);

            // Create the template arguments
            foreach (var arg in identifiers)
            {
                arguments = arguments.Add(arg);
            }

            return generator.InvocationExpression(expression, arguments);
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
