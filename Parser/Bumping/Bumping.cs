using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Compiler;
using Mono.Cecil;
using Xunit;

namespace Parser.Bumping
{
    public static class Bumping
    {
        public static TestCasesGenerator testCasesGenerator = new TestCasesGenerator();

        public static void ExecutionIsIdentical()
        {
            string expression = null;
            long x = default;
            long y = default;
            long z = default;
            using StreamWriter sw = new StreamWriter("out.txt");
            using StreamWriter loggerSw = new StreamWriter("logs.txt");

            while (true)
            {
                try
                {
                    expression = testCasesGenerator.GenerateRandomExpression(1).Single();
                    var rnd = new Random();

                    (long x, long y, long z) Generate() => (rnd.Next(1, int.MaxValue), rnd.Next(1, int.MaxValue),
                        rnd.Next(1, int.MaxValue));


                    Func<long, long, long, long> func;
                    Func<long, long, long, long> rosynFunc;

                    string[] myself;
                    try
                    {
                        myself = GeneratedMySelf(expression, out func);
                    }
                    catch (DivideByZeroException)
                    {
                        Assert.Throws<DivideByZeroException>(() => GeneratedRoslyn(expression, out rosynFunc));
                        continue;
                    }

                    string[] roslyn = GeneratedRoslyn(expression, out rosynFunc);

                    (x, y, z) = Generate();
                    loggerSw.WriteLine($"{expression} {x} {y} {z}");

                    long myResult;
                    try
                    {
                        myResult = func(x, y, z);
                    }
                    catch (DivideByZeroException)
                    {
                        Assert.Throws<DivideByZeroException>(() => rosynFunc(x, y, z));
                        continue;
                    }

                    var rolsynResult = rosynFunc(x, y, z);
                    Assert.Equal(myResult, rolsynResult);
                }
                catch (Exception ex)
                {
                    sw.WriteLine("expression :" + expression);
                    sw.WriteLine("x :" + x);
                    sw.WriteLine("y :" + y);
                    sw.WriteLine("z :" + z);
                    sw.WriteLine("exception :" + ex);
                    sw.WriteLine("exception message:" + ex.Message);
                    sw.WriteLine("exception stack:" + ex.StackTrace);
                    sw.WriteLine();
                }
            }
        }

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
            var assembly = testCasesGenerator.GetAssemblyStream(expression, @class, @namespace, fields);

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