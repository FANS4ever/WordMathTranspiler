using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using WordMathTranspiler.MathMLParser;
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
            List<SyntaxNode> declared = new List<SyntaxNode>();
            var test = RecursiveVisit(root, ref declared);
            declared.Add(test);
            declared.Add(CreateConsole(this.generator, RecursivePrintStatement(root)));
            declared.Add(CreateConsole(this.generator, "A = {0}", new string[] { "A" }));

            var mainMethodNode = CreateMain(this.generator, declared);
            var classNode = CreateClass(this.generator, "Program", new SyntaxNode[] { mainMethodNode });
            var namespaceNode = this.generator.NamespaceDeclaration("ThisIsTest", classNode);
            var usingDirectives = generator.NamespaceImportDeclaration("System");
            var compilationUnit = this.generator.CompilationUnit(usingDirectives, namespaceNode).NormalizeWhitespace();

            Console.WriteLine(compilationUnit);
            return compilationUnit.SyntaxTree;
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
                    throw new NotImplementedException();

                case NumNode numNode:
                    return generator.LiteralExpression(numNode.Value);

                case FloatNode floatNode:
                    return generator.LiteralExpression(floatNode.Value);

                case VarNode varNode:
                    return this.generator.LocalDeclarationStatement(
                        generator.TypeExpression(SpecialType.System_Object),
                        varNode.Name
                    );

                case AssignNode assignNode:
                    if (assignNode.variable is VarNode)
                    {
                        var variableDeclaration = RecursiveVisit(assignNode.variable, ref declared);
                        declared.Add(variableDeclaration);

                        var identifier = this.generator.IdentifierName((assignNode.variable as VarNode).Name);
                        return generator.AssignmentStatement(identifier, RecursiveVisit(assignNode.expression, ref declared));
                    }
                    else
                    {
                        throw new Exception("Can't assign value to non variable.");
                    }
                case InvocationNode mathFNode:
                    //RecursiveVisit(mathFNode.arg);
                    throw new NotImplementedException();
                case BinOpNode operatorNode:
                    return generator.AddExpression(
                        RecursiveVisit(operatorNode.left, ref declared),
                        RecursiveVisit(operatorNode.right, ref declared)
                    );
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
                case FloatNode floatNode:
                    return floatNode.Value.ToString();
                case NumNode numNode:
                    return numNode.Value.ToString();
                case VarNode varNode:
                    return varNode.Name.ToString();
                case AssignNode assignNode:
                    return RecursivePrintStatement(assignNode.variable) + " = " + RecursivePrintStatement(assignNode.expression);
                case InvocationNode mathFNode:
                    return "(" + RecursivePrintStatement(mathFNode.arg) + ")";
                case BinOpNode operatorNode:
                    return RecursivePrintStatement(operatorNode.left) + " " + operatorNode.op.ToString() + " " + RecursivePrintStatement(operatorNode.right);
                case SupNode supNode:
                    return "(" + RecursivePrintStatement(supNode.baseEl) + ")^(" + RecursivePrintStatement(supNode.supEl) + ")"; 
                default:
                    throw new NotImplementedException("Node type not implemented yet.");
            }

        }

        #region SyntaxNode creatio helpers
        public static SyntaxNode CreateMain(SyntaxGenerator generator, IEnumerable<SyntaxNode> statements)
        {
            var mainMethod = generator.MethodDeclaration(
                name: "Main",
                parameters: null, // Dont set parameters here because it causes error. Use generator.AddParameters instead
                accessibility: Accessibility.NotApplicable,
                modifiers: DeclarationModifiers.Static,
                statements: statements
            );

            generator.AddParameters(
                mainMethod, 
                new SyntaxNode[] { 
                    generator.ParameterDeclaration("args",  generator.ArrayTypeExpression(generator.TypeExpression(SpecialType.System_String))) 
                }
            );

            return mainMethod;
        }
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
