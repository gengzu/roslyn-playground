using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace roslyn_playground
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var staticCalculator = CreateStaticCalculator();

            System.Console.WriteLine(staticCalculator.Add(5, 10));

            CreateDynamicCalculator();

        }

        static void CreateDynamicCalculator()
        {
            var addMethodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("int"), "Add")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return a + b;")))
            ;

            var classDeclaration = SyntaxFactory.ClassDeclaration("DynamicCalculator")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ICalculator")))
                .AddMembers(addMethodDeclaration)
                ;

            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedCalculator")).NormalizeWhitespace()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
                .AddMembers(classDeclaration)
                ;

            var unit = SyntaxFactory.CompilationUnit()
                .AddMembers(@namespace)
                ;

            System.Console.WriteLine(@namespace.NormalizeWhitespace().ToFullString());

            var compiler = CSharpCompilation.Create("testAssembly", syntaxTrees: new[] { unit.SyntaxTree });

            using (var ms = new MemoryStream())
            {
                var result = compiler.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                }
            }
        }

        static ICalculator CreateStaticCalculator()
        {
            return new StaticCalculator();
        }
    }

    public interface ICalculator
    {
        int Add(int a, int b);
    }

    public class StaticCalculator : ICalculator
    {
        public int Add(int a, int b)
        {
            return 666;
        }
    }
}
