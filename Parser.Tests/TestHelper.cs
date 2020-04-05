using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Compiler;
using Mono.Cecil;
using Parser.Tests.ILGeneratorTests;


namespace Parser
{
    public static class TestHelper
    {
        public static IExpression GetParseResultExpression(string expression, bool constantFolding = true,
            Dictionary<string, (CompilerType[] parameters, CompilerType @return)> methods = null)
        {
            var lexer = new Lexer(expression);
            var readOnlyList = lexer.Tokenize();
            var context = new ParserContext(
                readOnlyList,
                new Dictionary<string, CompilerType>
                    {{"x", CompilerType.Long}, {"y", CompilerType.Long}, {"z", CompilerType.Long}},
                null,
                methods,
                constantFolding
            );
            var parser = new Parser(context);
            var result = parser.ParseExpression();
            return result;
        }

        public static IStatement[] GetParseResultStatements(string expression,
            Dictionary<string, (CompilerType[] parameters, CompilerType @return)> methods = null)
        {
            var lexer = new Lexer(expression);
            var readOnlyList = lexer.Tokenize();
            var context = new ParserContext(
                readOnlyList,
                new Dictionary<string, CompilerType>
                    {{"x", CompilerType.Long}, {"y", CompilerType.Long}, {"z", CompilerType.Long}},
                new Dictionary<string, FieldInfo>(), 
                methods ?? new Dictionary<string, (CompilerType[] parameters, CompilerType @return)>(),
                true);
            var parser = new Parser(context);
            var result = parser.Parse();
            return result.Statements;
        }

       private static TestCasesGenerator testCasesGenerator = new TestCasesGenerator();

        public static string[] GeneratedRoslynExpression(string returnExpression, out Func<long, long, long, long> func,
            string[] statements = null)
        {
            var assembly = testCasesGenerator.GetAssemblyStream(returnExpression, statements: statements);

            var methodDefinition = AssemblyDefinition.ReadAssembly(assembly).MainModule
                .GetTypes()
                .Single(x => x.Name.Contains("Runner"))
                .Methods
                .Single(x => x.Name == "Run");

            var instructions = methodDefinition
                .Body.Instructions
                .ToArray();

            var loaded = Assembly.Load(assembly.ToArray());
            var method = loaded
                .ExportedTypes
                .Single(x => x.Name.Contains("Runner"))
                .GetMethods()
                .Single(x => x.Name == "Run");

            func = (Func<long, long, long, long>) method.CreateDelegate(typeof(Func<long, long, long, long>));

            return instructions.Select(x => x.ToString().Remove(0, 9)).ToArray();
        }

        public static string[] GeneratedRoslynMethod(string methodBody, out Func<long, long, long, long> func)
        {
            var assembly = testCasesGenerator.GetAssemblyStream(methodBody: methodBody);

            var methodDefinition = AssemblyDefinition.ReadAssembly(assembly).MainModule
                .GetTypes()
                .Single(x => x.Name.Contains("Runner"))
                .Methods
                .Single(x => x.Name == "Run");

            var instructions = methodDefinition
                .Body.Instructions
                .ToArray();

            var loaded = Assembly.Load(assembly.ToArray());
            var method = loaded
                .ExportedTypes
                .Single(x => x.Name.Contains("Runner"))
                .GetMethods()
                .Single(x => x.Name == "Run");

            func = (Func<long, long, long, long>) method.CreateDelegate(typeof(Func<long, long, long, long>));

            return instructions.Select(x => x.ToString().Remove(0, 9)).ToArray();
        }
    }
}