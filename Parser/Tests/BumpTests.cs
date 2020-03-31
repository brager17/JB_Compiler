using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Compiler;
using Mono.Cecil;
using Xunit;
using Xunit.Abstractions;

namespace Parser
{
    public class BumpTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private TestCasesGenerator testCasesGenerator;

        public BumpTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            testCasesGenerator = new TestCasesGenerator();
        }

        [InlineData("x*y*z")]
        [InlineData("x/y/z")]
        [InlineData("x+y+z")]
        [InlineData("x-y-z")]
        [InlineData("(x/(y+z)+(x*y))")]
        [Theory]
        public void Expression(string expression)
        {
            var actual = GeneratedMySelf(expression, out var myFunc);

            var expected = GeneratedRoslyn(expression, out var monoFunc);

            Assert.Equal(expected, actual);
        }

        public static long a = 1;
        public static long b = 1;
        public static long c = 1;

        [InlineData("x*c*y*z+a-b", new[] {"a", "b", "c"})]
        [InlineData("x/y/z/a", new[] {"a"})]
        [InlineData("x+y+z-a", new[] {"a"})]
        [InlineData("x-y-z-a", new[] {"a"})]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b-2*c))", new[] {"a", "b", "c"})]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b*-c))", new[] {"a", "b", "c"})]
        [InlineData("(x/-(a+b*c))", new[] {"a", "b", "c"})]
        [Theory]
        public void ExpressionWithClosedVariable(string expression, string[] closed)
        {
            var actual = GeneratedMySelf(expression, out var func, closed);

            var expected =
                GeneratedRoslyn(expression, out var monoFunc, GetType().Name, GetType().Namespace, closed);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IlCodeIsIdentical()
        {
            var randomExpr = testCasesGenerator.GenerateRandomExpression(10);

            foreach (var item in randomExpr)
            {
                var actual = GeneratedMySelf(item, out var func);

                var expected = GeneratedRoslyn(item, out var monoFunc, GetType().Name, GetType().Namespace);

                Assert.Equal(expected, actual);
            }
        }

        // problems :Expression 3147*(7/y)/z/84-(13+x)-106984*y/183*x-y/x*(8*(5)+x*0*(3/((2/(8/x/9)*0*(2-(9-z/6/x+7/y))/2+x))*3+y-1))
//        x = 1245627257,y = 2124552673,z = 601530941 
        //      MyResult 2408827548221383992
        //System.DivideByZeroException : Attempted to divide by zero.


        //  at Parser.BumpTests.Run(Int64 x, Int64 y, Int64 z)
        //at Parser.BumpTests.ExecutionIsIdentical() in C:\Users\evgeniy\RiderProjects\Parser\Parser\BumpTests.cs:line 125

        [Theory]
        [InlineData(1245627257, 2124552673, 601530941)]
        public static long Fact(long x, long y, long z)
        {
            return 3147 * (7 / y) / z / 84 - (13 + x) - 106984 * y / 183 * x - y / x *
                (8 * (5) + x * 0 *
                    (3 / ((2 / (8 / x / 9) * 0 * (2 - (9 - z / 6 / x + 7 / y)) / 2 + x)) * 3 + y - 1));
        }

        [Fact]
        public void ExecutionIsIdentical()
        {
            string expression = null;

            expression = testCasesGenerator.GenerateRandomExpression(1).Single();
            var rnd = new Random();

            (long x, long y, long z) Generate() => (rnd.Next(1, int.MaxValue), rnd.Next(1, int.MaxValue),
                rnd.Next(1, int.MaxValue));

            Func<long, long, long, long> func;
            Func<long, long, long, long> rosynFunc;
            // _testOutputHelper.WriteLine("Expression " + randomExpr);

            string[] myself;
            try
            {
                myself = GeneratedMySelf(expression, out func);
            }
            catch (DivideByZeroException)
            {
                // _testOutputHelper.WriteLine("MyResult DivideByZeroException in compile time");
                Assert.Throws<DivideByZeroException>(() =>
                    GeneratedRoslyn(expression, out rosynFunc, GetType().Name, GetType().Namespace));
                // _testOutputHelper.WriteLine("Roslyn DivideByZeroException in compile time");
                return;
            }

            string[] roslyn = GeneratedRoslyn(expression, out rosynFunc, GetType().Name, GetType().Namespace);

            // var (x, y, z) = Generate();
            var (x, y, z) = (1245627257, 2124552673, 601530941);

            // _testOutputHelper.WriteLine($"x = {x},y = {y},z = {z} ");

            long myResult = 0;
            try
            {
                myResult = func(x, y, z);
                // _testOutputHelper.WriteLine("MyResult " + myResult);
            }
            catch (DivideByZeroException)
            {
                // _testOutputHelper.WriteLine("MyResult DivideByZeroException");
                Assert.Throws<DivideByZeroException>(() => rosynFunc(x, y, z));
                // _testOutputHelper.WriteLine("Roslyn DivideByZeroException");
                // _testOutputHelper.WriteLine("");
                return;
            }

            var rolsynResult = rosynFunc(x, y, z);
            // _testOutputHelper.WriteLine("Roslyn " + rolsynResult);
            Assert.Equal(myResult, rolsynResult);
            // _testOutputHelper.WriteLine("");
        }

        [Fact]
        public void Parse__SumTwoInts__Correct()
        {
            var expr = "1111*y+12323*x";
            var roslyn = GeneratedMySelf(expr, out var func);
            func(1, 1, 1);
        }


        private string[] GeneratedMySelf(string expression, out Func<long, long, long, long> func,
            string[] closed = null)
        {
            var lexer = new Lexer(expression);

            var tokens = lexer.ReadAll();
            var parser = new Parser(tokens);
            var r = parser.Parse().Single();
            var dynamicMethod = new DynamicMethod(
                "method",
                typeof(long),
                new[] {typeof(long), typeof(long), typeof(long)});

            var staticFields = closed == null
                ? null
                : GetType()
                    .GetFields().Where(x => closed.Contains(x.Name))
                    .ToDictionary(x => x.Name, x => x);

            var visitor = new CompileExpressionVisitor(dynamicMethod, new[] {"x", "y", "z"}, staticFields, null);
            visitor.Start((BinaryExpression) r);

            func = (Func<long, long, long, long>) dynamicMethod.CreateDelegate(typeof(Func<long, long, long, long>));
            return visitor.logger.GetLogs;
        }

        private string[] GeneratedRoslyn(string expression, out Func<long, long, long, long> func,
            string @class = "Runner",
            string @namespace = "RunnerNamespace", string[] fields = null)
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