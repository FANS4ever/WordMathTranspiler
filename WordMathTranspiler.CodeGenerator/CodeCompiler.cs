using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    class CodeCompiler
    {
        public static void GenerateExecutable(IEnumerable<SyntaxTree> trees)
        {
            string currentDir = Directory.GetCurrentDirectory();
            string resultDir = currentDir + "/CompilationResult";

            DirectoryInfo di = new DirectoryInfo(resultDir);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }

            using (FileStream fs = new FileStream(Path.Combine(resultDir, "program.dll"), FileMode.Create))
            {
                EmitResult result = CSharpCompilation.Create(
                    assemblyName: "TestFile",
                    syntaxTrees: trees,
                    references: GenerateReferences(),
                    options: new CSharpCompilationOptions(
                        outputKind: OutputKind.ConsoleApplication,
                        optimizationLevel: OptimizationLevel.Debug,
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                    )
                ).Emit(fs);

                if (result.Success)
                {
                    using (FileStream configStream = new FileStream(Path.Combine(resultDir, "program.runtimeconfig.json"), FileMode.Create))
                    {
                        configStream.Write(Encoding.UTF8.GetBytes(GenerateRuntimeConfig()));
                    }

                    using (FileStream runFileStream = new FileStream(Path.Combine(resultDir, "run.bat"), FileMode.Create))
                    {
                        runFileStream.Write(Encoding.UTF8.GetBytes(GenerateRunFile()));
                    }
                }
                else
                {
                    var errorList = string.Join(
                        separator: Environment.NewLine,
                        values: result.Diagnostics.Select(diagnostic => diagnostic.ToString())
                    );

                    // Consider writing error to file?
                    Console.WriteLine(errorList);
                }
            }
        }

        /// <summary>
        /// Update to use that reference resolver thing mentioned in github
        /// </summary>
        /// <returns></returns>
        private static MetadataReference[] GenerateReferences()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            return new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"))
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

        private static string GenerateRunFile()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("@ECHO OFF");
            builder.AppendLine("dotnet ./program.dll");
            builder.AppendLine("@PAUSE");

            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(builder.ToString()));
        }
    }
}
