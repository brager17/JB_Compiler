using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Compiler;
using Mono.Cecil;

namespace Parser.Bumping
{
    public static class Bumping
    {
        public static TestCasesGenerator testCasesGenerator = new TestCasesGenerator();

        private static string[] GeneratedMySelf(string expression, out Func<long, long, long, long> func,
            string[] closed = null)
        {
            var lexer = new Lexer(expression);

            var r = TestHelper.GetParseResultExpression(expression);
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var visitor = new CompileExpressionVisitor(dynamicMethod, new[] {"x", "y", "z"}, null, null);
            visitor.Start((BinaryExpression) r);

            func = (Func<long, long, long, long>) dynamicMethod.CreateDelegate(typeof(Func<long, long, long, long>));
            return visitor.logger.GetLogs;
        }

        private static string[] GeneratedRoslyn(string expression, out Func<long, long, long, long> func,
            string @class = "Runner",
            string @namespace = "RunnerNamespace", (string, long)[] fields = null)
        {
            var assembly = testCasesGenerator.GetAssemblyStream(expression);

            var methodDefinition = AssemblyDefinition.ReadAssembly(assembly).MainModule
                .GetTypes()
                .Single(x => x.Name.Contains(@class))
                .Methods
                .Single(x => x.Name == "Run");
            var instructions = methodDefinition
                .Body.Instructions
                .ToArray();

            var loaded = Assembly.Load(assembly.ToArray());
            var method = loaded
                .ExportedTypes
                .Single(x => x.Name.Contains(@class))
                .GetMethods()
                .Single(x => x.Name == "Run");

            func = (Func<long, long, long, long>) method.CreateDelegate(typeof(Func<long, long, long, long>));

            return instructions.Select(x => x.ToString().Remove(0, 9)).ToArray();
        }
    }
}