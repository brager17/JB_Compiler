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
            var readOnlyList = lexer.ReadAll();
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
            var readOnlyList = lexer.ReadAll();
            var context = new ParserContext(
                readOnlyList, new Dictionary<string, CompilerType>
                    {{"x", CompilerType.Long}, {"y", CompilerType.Long}, {"z", CompilerType.Long}}, null, methods
            );
            var parser = new Parser(context);
            var result = parser.Parse();
            return result.Statements;
        }

        public static string[] GeneratedExpressionMySelf(string expression, out Func<long, long, long, long> func)
        {
            var lexer = new Lexer(expression);

            var staticFields = typeof(MethodsFieldsForTests)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.FieldType == typeof(long) || x.FieldType == typeof(int))
                .ToDictionary(x => x.Name, x => x);

            var staticMethods = typeof(MethodsFieldsForTests)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .ToDictionary(x => x.Name, x => x);

            var methodParameters = new Dictionary<string, CompilerType>()
                {{"x", CompilerType.Long}, {"y", CompilerType.Long}, {"z", CompilerType.Long}};

            var tokens = lexer.ReadAll();


            var context = new ParserContext(
                tokens, methodParameters,
                staticFields.ToDictionary(x => x.Key, x => x.Value.FieldType.GetRoslynType()),
                staticMethods.ToDictionary(x => x.Key,
                    x => (x.Value.GetParameters().Select(xx => xx.ParameterType.GetRoslynType()).ToArray(),
                        x.Value.ReturnType.GetRoslynType()))
            );
            var parser = new Parser(context);
            var r = parser.ParseExpression();
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});


            var visitor =
                new CompileExpressionVisitor(dynamicMethod, new[] {"x", "y", "z"}, staticFields, staticMethods);
            visitor.Start(r);

            func = (Func<long, long, long, long>) dynamicMethod.CreateDelegate(typeof(Func<long, long, long, long>));
            return visitor.logger.GetLogs;
        }


        public static string[] GeneratedStatementsMySelf(
            string expression,
            out Func<long, long, long, long> func,
            Dictionary<string, CompilerType> parameters = null,
            Dictionary<string, CompilerType> closedFields = null,
            Type @this = null)
        {
            var lexer = new Lexer(expression);

            var staticFields = typeof(MethodsFieldsForTests)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Union(@this != null
                    ? @this.GetFields(BindingFlags.Public | BindingFlags.Static)
                    : Array.Empty<FieldInfo>())
                .Where(x => x.FieldType == typeof(long) || x.FieldType == typeof(int))
                .Where(x => closedFields?.ContainsKey(x.Name) ?? true)
                .ToDictionary(x => x.Name, x => x);

            var staticMethods = typeof(MethodsFieldsForTests)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Union(@this != null
                    ? @this.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    : Array.Empty<MethodInfo>())
                .ToDictionary(x => x.Name, x => x);

            parameters ??= new Dictionary<string, CompilerType>()
            {
                {"x", CompilerType.Long},
                {"y", CompilerType.Long},
                {"z", CompilerType.Long}
            };

            var tokens = lexer.ReadAll();
            var context = new ParserContext(tokens, parameters, staticFields.ToDictionary(
                    x => x.Key,
                    x => x.Value.FieldType.GetRoslynType()),
                staticMethods.ToDictionary(x => x.Key,
                    x => (x.Value.GetParameters().Select(xx => xx.ParameterType.GetRoslynType()).ToArray(),
                        x.Value.ReturnType.GetRoslynType())));

            var parser = new Parser(context);
            var r = parser.Parse();

            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});


            var visitor = new CompileExpressionVisitor(
                dynamicMethod,
                parameters.Select(x => x.Key).ToArray(),
                staticFields,
                staticMethods);
            visitor.Start(r);

            func = (Func<long, long, long, long>) dynamicMethod.CreateDelegate(typeof(Func<long, long, long, long>));
            return visitor.logger.GetLogs;
        }

        private static TestCasesGenerator testCasesGenerator = new TestCasesGenerator();


        public static string[] GeneratedRoslynExpression(string returnExpression, out Func<long, long, long, long> func,
            string[] statements = null)
        {
            var assembly =
                testCasesGenerator.GetAssemblyStream(returnExpression, statements: statements);

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