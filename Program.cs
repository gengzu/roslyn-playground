using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace roslyn_playground
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var staticCalculator = CreateStaticCalculator();

            System.Console.WriteLine("result 1 - " + staticCalculator.Add(5, 10));

            var dynamicCalculator = CreateDynamicCalculator();

            System.Console.WriteLine("result 2 - " + dynamicCalculator.Add(2, 3));

        }

        static CompilationUnitSyntax CreateCompilationUnit()
        {
            var addMethodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("int"), "Add")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(new[] {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("a")).WithType(SyntaxFactory.ParseTypeName("int")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("b")).WithType(SyntaxFactory.ParseTypeName("int"))
                    })
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return a + b;")))
            ;

            var classDeclaration = SyntaxFactory.ClassDeclaration("DynamicCalculator")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ICalculator")))
                .AddMembers(addMethodDeclaration)
                ;

            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedCalculator")).NormalizeWhitespace()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("roslyn_playground")))
                .AddMembers(classDeclaration)
                ;

            return SyntaxFactory.CompilationUnit()
                .AddMembers(@namespace)
                ;
        }

        static ICalculator CreateDynamicCalculator()
        {
            var unit = CreateCompilationUnit();

            System.Console.WriteLine(unit.NormalizeWhitespace().ToFullString());

            var compiler = CSharpCompilation.Create("testAssembly", 
                syntaxTrees: new[] { unit.SyntaxTree }, 
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
                );

            using (var ms = new MemoryStream())
            {
                var result = compiler.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    var mf = assembly.CreateInstance("GeneratedCalculator.DynamicCalculator");

                    System.Console.WriteLine("DONE!!! " + mf.GetType());

                    return mf as ICalculator;
                }
                else
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    return null;
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
