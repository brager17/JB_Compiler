using System;
using System.IO;
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
        public static long b = 2;
        public static long c = 3;

        [InlineData("x*c*y*z+a-b")]
        [InlineData("x/y/z/a")]
        [InlineData("x+y+z-a")]
        [InlineData("x-y-z-a")]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b-2*c))")]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b*-c))")]
        [InlineData("(x/-(a+b*c))")]
        [Theory]
        public void ExpressionWithClosedVariable(string expression)
        {
            var actual = GeneratedMySelf(expression, out var func, new[] {"a", "b", "c"});

            var expected =
                GeneratedRoslyn(expression, out var monoFunc, GetType().Name, GetType().Namespace,
                    new[] {("a", a), ("b", b), ("c", c)});

            Assert.Equal(expected, actual);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWithoutParameters() => 1;

        public const string MethodWithoutParametersText = @"
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWithoutParameters() => 1;";


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith1Parameter(long x) => x;

        public const string MethodWith1ParameterText = @"
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith1Parameter(long x) => x;";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith2Parameters(long x, long y) => (x + y);

        public const string MethodWith2ParametersText = @"
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith2Parameters(long x, long y) => (x + y);";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith3Parameters(long x, long y, long z) => (x + y + z);

        public const string MethodWith3ParametersText = @"
         [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith3Parameters(long x, long y, long z) => (x + y + z);";

        [Theory]
        [InlineData("1+MethodWithoutParameters()",
            new[] {MethodWithoutParametersText},
            new[] {nameof(MethodWithoutParameters)})]
        [InlineData("1+MethodWith1Parameter(3)",
            new[] {MethodWith1ParameterText},
            new[] {nameof(MethodWith1Parameter)})]
        [InlineData("1+MethodWith2Parameters(1,3)",
            new[] {MethodWith2ParametersText},
            new[] {nameof(MethodWith2Parameters)})]
        public void ExpressionWithCallsStaticMethods(string expression, string[] methods, string[] methodNames)
        {
            var actual = GeneratedMySelf(expression, out var func, null, methodNames);

            var expected =
                GeneratedRoslyn(expression, out var monoFunc, GetType().Name, GetType().Namespace, null, methods);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x)",
            new[] {MethodWith1ParameterText},
            new[] {nameof(MethodWith1Parameter)})]
        [InlineData("1+MethodWith3Parameters(x,y,z)",
            new[] {MethodWith3ParametersText},
            new[] {nameof(MethodWith3Parameters)})]
        [InlineData("1+MethodWith3Parameters(a,1324,c)",
            new[] {MethodWith3ParametersText},
            new[] {nameof(MethodWith3Parameters)})]
        public void Parse__StaticMethodWithVariableParameters__Correct
            (string expression, string[] methods, string[] methodNames)
        {
            var actual = GeneratedMySelf(expression, out var func, new[] {"a", "b", "c"}, methodNames);

            var expected =
                GeneratedRoslyn(expression, out var monoFunc, GetType().Name, GetType().Namespace,
                    new[] {("a", a), ("b", b), ("c", c)}, methods);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x+12+y)",
            new[] {MethodWith1ParameterText},
            new[] {nameof(MethodWith1Parameter)})]
        [InlineData("1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14)",
            new[] {MethodWith1ParameterText},
            new[] {nameof(MethodWith1Parameter)})]
        [InlineData(
            "1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14+MethodWithoutParameters()*MethodWith3Parameters(x,y,z))",
            new[] {MethodWith1ParameterText, MethodWithoutParametersText, MethodWith3ParametersText},
            new[] {nameof(MethodWith1Parameter), nameof(MethodWithoutParameters), nameof(MethodWith3Parameters)})]
        public void Parse__StaticMethodWithExpressionParameter(string expr, string[] methodAsText, string[] methodNames)
        {
            var actual = GeneratedMySelf(expr, out var func, new[] {"a", "b", "c"}, methodNames);

            var expected =
                GeneratedRoslyn(expr, out var monoFunc, GetType().Name, GetType().Namespace,
                    new[] {("a", a), ("b", b), ("c", c)}, methodAsText);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
            Assert.Equal(expected, actual);
        }

        public long CallMethodWithoutParameters()
        {
            return MethodWithoutParameters();
        }

        public long CallMethodWithParameter()
        {
            return MethodWith1Parameter(1);
        }

        public long CallMethodWithParameters()
        {
            return MethodWith2Parameters(1, 2);
        }

        public long SumCallMethodsWithParameters()
        {
            return MethodWithoutParameters() + MethodWith1Parameter(1) + MethodWith2Parameters(1, 2);
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

        [Theory]
        [InlineData(1245627257, 2124552673, 601530941)]
        public static long Fact2(long x, long y, long z)
        {
            return 3147 * (7 / y) / z / 84 - (13 + x) - 106984 * y / 183 * x - y / x *
                (8 * (5) + x * 0 *
                    (3 / ((2 / (8 / x / 9) * 0 * (2 - (9 - z / 6 / x + 7 / y)) / 2 + x)) * 3 + y - 1));
        }

        [Fact]
        public void ExecutionIsIdentical()
        {
            string expression =
                "x+684451365090141806*x/y+3/y*z+z/6/(6)/4+x*6-((6/(6*(x+9/z)+y-3-z/5-z/7/x*5)-7)+0*(7/y-4/(0+(4/(3/x)))))";
            long x = default;
            long y = default;
            long z = default;
            using StreamWriter sw = new StreamWriter("out.txt");
            var testCasesGenerator = new TestCasesGenerator();

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

            (x, y, z) = Generate();

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
            string[] closed = null, string[] methodNames = null)
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

            var staticMethods = methodNames == null
                ? null
                : GetType()
                    .GetMethods()
                    .Where(x => methodNames.Contains(x.Name))
                    .ToDictionary(x => x.Name, x => x);

            var visitor =
                new CompileExpressionVisitor(dynamicMethod, new[] {"x", "y", "z"}, staticFields, staticMethods);
            visitor.Start((BinaryExpression) r);

            func = (Func<long, long, long, long>) dynamicMethod.CreateDelegate(typeof(Func<long, long, long, long>));
            return visitor.logger.GetLogs;
        }

        private string[] GeneratedRoslyn(string expression, out Func<long, long, long, long> func,
            string @class = "Runner",
            string @namespace = "RunnerNamespace", (string, long)[] fields = null, string[] methods = null)
        {
            var assembly = testCasesGenerator.GetAssemblyStream(expression, @class, @namespace, fields, methods);

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