using System;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Parser
{
    public static class MethodsFieldsForTests
    {
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


        public static long a = 1;
        public static long b = 2;
        public static long c = 3;
    }

    public class BumpTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private TestCasesGenerator testCasesGenerator;

        public BumpTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            testCasesGenerator = new TestCasesGenerator();
        }

        [Theory]
        [InlineData("x")]
        public void ReturnX(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(myFunc(1, 1, 1), myFunc(1, 1, 1));
        }

        [Theory]
        [InlineData("x+y")]
        public void ReturnXPlusY(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(myFunc(1, 1, 1), myFunc(1, 1, 1));
        }

        [Theory]
        [InlineData("x+y+12")]
        public void ReturnXPlusYPlusNum(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(myFunc(1, 1, 1), myFunc(1, 1, 1));
        }


        [Theory]
        [InlineData("4111111111 + 1")]
        [InlineData("4111111111 + 41111111111")]
        [InlineData("4111111111 + 23")]
        public void SumNumbersOfOtherTypes(string expr)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expr, out var myFunc);
            var expected = TestHelper.GeneratedRoslynExpression(expr, out var expectedFunc);
            Assert.Equal(expectedFunc(1,1,1), myFunc(1, 1, 1));
        } 

        // public long Test11()
        // {
        //     uint q = 4111111111;
        // }

        public long Test() => uint.MaxValue;

        [InlineData("x*y*z")]
        [InlineData("x/y/z")]
        [InlineData("x+y+z")]
        [InlineData("x-y-z")]
        [InlineData("(x/(y+z)+(x*y))")]
        [Theory]
        public void Expression(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(expected, actual);
        }


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
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

        }


        [Theory]
        [InlineData("1+MethodWithoutParameters()",
            new[] {MethodsFieldsForTests.MethodWithoutParametersText},
            new[] {nameof(MethodsFieldsForTests.MethodWithoutParameters)})]
        [InlineData("1+MethodWith1Parameter(3)",
            new[] {MethodsFieldsForTests.MethodWith1ParameterText},
            new[] {nameof(MethodsFieldsForTests.MethodWith1Parameter)})]
        [InlineData("1+MethodWith2Parameters(1,3)",
            new[] {MethodsFieldsForTests.MethodWith2ParametersText},
            new[] {nameof(MethodsFieldsForTests.MethodWith2Parameters)})]
        public void ExpressionWithCallsStaticMethods(string expression, string[] methods, string[] methodNames)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x)",
            new[] {MethodsFieldsForTests.MethodWith1ParameterText},
            new[] {nameof(MethodsFieldsForTests.MethodWith1Parameter)})]
        [InlineData("1+MethodWith3Parameters(x,y,z)",
            new[] {MethodsFieldsForTests.MethodWith3ParametersText},
            new[] {nameof(MethodsFieldsForTests.MethodWith3Parameters)})]
        [InlineData("1+MethodWith3Parameters(a,1324,c)",
            new[] {MethodsFieldsForTests.MethodWith3ParametersText},
            new[] {nameof(MethodsFieldsForTests.MethodWith3Parameters)})]
        public void Parse__StaticMethodWithVariableParameters__Correct
            (string expression, string[] methods, string[] methodNames)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var func);

            var expected =
                TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x+12+y)",
            new[] {MethodsFieldsForTests.MethodWith1ParameterText},
            new[] {nameof(MethodsFieldsForTests.MethodWith1Parameter)})]
        [InlineData("1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14)",
            new[] {MethodsFieldsForTests.MethodWith1ParameterText},
            new[] {nameof(MethodsFieldsForTests.MethodWith1Parameter)})]
        [InlineData(
            "1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14+MethodWithoutParameters()*MethodWith3Parameters(x,y,z))",
            new[]
            {
                MethodsFieldsForTests.MethodWith1ParameterText, MethodsFieldsForTests.MethodWithoutParametersText,
                MethodsFieldsForTests.MethodWith3ParametersText
            },
            new[]
            {
                nameof(MethodsFieldsForTests.MethodWith1Parameter),
                nameof(MethodsFieldsForTests.MethodWithoutParameters),
                nameof(MethodsFieldsForTests.MethodWith3Parameters)
            })]
        public void Parse__StaticMethodWithExpressionParameter(string expr, string[] methodAsText, string[] methodNames)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expr, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expr, out var monoFunc);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
        }

        [Fact]
        public void IlCodeIsIdentical()
        {
            // var randomExpr = testCasesGenerator.GenerateRandomExpression(10);
            //
            // foreach (var item in randomExpr)
            // {
            //     var actual = TestHelper.GeneratedExpressionMySelf(item, out var func);
            //
            //     var expected = TestHelper.GeneratedRoslyn(item, out var monoFunc);
            //
            //     Assert.Equal(expected, actual);
            // }
        }

        // [Theory]
        // [InlineData(1245627257, 2124552673, 601530941)]
        // public static long Fact2(long x, long y, long z)
        // {
        //     return 3147 * (7 / y) / z / 84 - (13 + x) - 106984 * y / 183 * x - y / x *
        //         (8 * (5) + x * 0 *
        //             (3 / ((2 / (8 / x / 9) * 0 * (2 - (9 - z / 6 / x + 7 / y)) / 2 + x)) * 3 + y - 1));
        // }

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
                myself = TestHelper.GeneratedExpressionMySelf(expression, out func);
            }
            catch (DivideByZeroException)
            {
                // _testOutputHelper.WriteLine("MyResult DivideByZeroException in compile time");
                Assert.Throws<DivideByZeroException>(() => TestHelper.GeneratedRoslynExpression(expression, out rosynFunc));
                // _testOutputHelper.WriteLine("Roslyn DivideByZeroException in compile time");
                return;
            }

            string[] roslyn = TestHelper.GeneratedRoslynExpression(expression, out rosynFunc);

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
            var roslyn = TestHelper.GeneratedExpressionMySelf(expr, out var func);
            func(1, 1, 1);
        }
    }
}