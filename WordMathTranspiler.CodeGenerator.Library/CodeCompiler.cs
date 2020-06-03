using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WordMathTranspiler.CodeGenerator
{
    public class CodeCompiler
    {
        public static void GenerateExecutable(IEnumerable<SyntaxTree> trees, string outputDirectory, string compileLanguage, string assemblyName)
        {
            string resultDir = outputDirectory + "/CompilationResult";

            // Prep directory
            DirectoryInfo di = new DirectoryInfo(resultDir);
            if (di.Exists)
            {
                di.Delete(true);
            }
            di.Refresh();
            di.Create();

            using (FileStream fs = new FileStream(Path.Combine(resultDir, "program.dll"), FileMode.Create))
            {
                EmitResult result = null;
                switch (compileLanguage)
                {
                    case "VisualBasic":
                        result = VisualBasicCompilation.Create(
                            assemblyName: assemblyName,
                            syntaxTrees: trees,
                            references: GenerateReferences(),
                            options: new VisualBasicCompilationOptions(
                                outputKind: OutputKind.ConsoleApplication,
                                optimizationLevel: OptimizationLevel.Release,
                                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                            )
                        ).Emit(fs);
                        break;
                    default:
                        result = CSharpCompilation.Create(
                            assemblyName: assemblyName,
                            syntaxTrees: trees,
                            references: GenerateReferences(),
                            options: new CSharpCompilationOptions(
                                outputKind: OutputKind.ConsoleApplication,
                                optimizationLevel: OptimizationLevel.Release,
                                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                            )
                        ).Emit(fs);
                        break;
                }

                if (result.Success)
                {
                    using (FileStream configStream = new FileStream(Path.Combine(resultDir, "program.runtimeconfig.json"), FileMode.Create))
                    {
                        configStream.Write(Encoding.UTF8.GetBytes(GenerateRuntimeConfig()));
                    }

                    using (FileStream runFileStream = new FileStream(Path.Combine(resultDir, "run.bat"), FileMode.Create))
                    {
                        runFileStream.Write(Encoding.UTF8.GetBytes(GenerateWindowsRunScript()));
                    }
                }
                else
                {
                    var errorList = string.Join(
                        separator: Environment.NewLine,
                        values: result.Diagnostics.Select(diagnostic => diagnostic.ToString())
                    );

                    Console.WriteLine(errorList);
                }
            }
        }

        private static MetadataReference[] GenerateReferences()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            return new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.CSharp.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.VisualBasic.dll")), //required for vb
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Microsoft.VisualBasic.Core.dll")) //required for vb
            };
        }

        private static string GenerateRuntimeConfig()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream,new JsonWriterOptions() { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteStartObject("runtimeOptions");
                    writer.WriteStartObject("framework");
                    writer.WriteString("name", "Microsoft.NETCore.App");
                    writer.WriteString(
                        "version",
                        RuntimeInformation.FrameworkDescription.Replace(".NET Core ", "")
                    );
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static string GenerateWindowsRunScript()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("@ECHO OFF");
            builder.AppendLine("dotnet ./program.dll");
            builder.AppendLine("@PAUSE");

            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(builder.ToString()));
        }
    }
}
