using System.Linq;
using Parser.Tests.ILGeneratorTests.MethodTests;
using Xunit;
using Xunit.Abstractions;

namespace Parser.Tests.ILGeneratorTests
{
    public class StatementTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public StatementTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Compile__LongStatements__Correct()
        {
            string expr = "long q = 12;long w = -14;return q+w;";
            var result = TestHelper.GetParseResultStatements(expr);

            Compiler.CompileStatement(expr, out var func);
            TestHelper.GeneratedRoslynExpression("q+w",
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Fact]
        public void Compile__IntStatements__Correct()
        {
            string expr = "int q = 12;int w = -14;return q+w;";

            Compiler.CompileStatement(expr, out var func);
            TestHelper.GeneratedRoslynExpression("q+w",
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Theory]
        [InlineData("int q=1;long s = 12;long r = 14;return (1+12+s)-r;", "(1+12+s)-r")]
        [InlineData("int q=1;long s = q*13;long r = s+q-2;return (1+12+s)-r;", "(1+12+s)-r")]
        public void Compile__LongAndIntStatements__Correct(string expr, string returnExpr)
        {
            Compiler.CompileStatement(expr, out var func);
            TestHelper.GeneratedRoslynExpression(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        public static long MethodWith2Parameters(long o, long t) => t + o;
        
        [Theory]
        [InlineData("long _d = a+c;int q=1;return _d+a-12;", "_d+a-12")]
        [InlineData("int i=2;long j = MethodWith2Parameters(i,a);return j;", "j")]
        // problem
        // public class Runner
        // {
        // public static long a = 1;

        // [MethodImpl(MethodImplOptions.NoOptimization)]
        // public static long Run(long x, long y, long z)
        // {
        // int _d = a;
        // int q = 1;
        // return _d + d - 12;
        // }
        // }
        public void Compile__StatementWithStaticMethodsAndFields__Correct(string expr, string returnExpr)
        {
            Compiler.CompileStatement(expr, out var func,typeof(MethodsFieldsForTests));
            TestHelper.GeneratedRoslynExpression(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Theory]
        [InlineData("int t=1;long q=2;long r = t + q;return r;", "r")]
        [InlineData("long u = 1;long q = int.MaxValue+u;return q;", "q")]
        [InlineData("long u = 1;long q = int.MaxValue+u;int w = int.MaxValue-2;return q+w;", "q+w")]
        [InlineData("long u = 1;long q = int.MaxValue+u;int w = int.MaxValue-2;return q*w;", "q*w")]
        [InlineData("long u = 1;long q = int.MaxValue+u;int w = int.MaxValue-2;return q/w;", "q/w")]
        [InlineData("long u = 1;long q = int.MaxValue+u;int w = int.MaxValue-2;return q-w;", "q-w")]
        [InlineData("long u = 1;long q = int.MaxValue+u;long w = long.MaxValue;return q+w;", "q+w")]
        public void Test(string expr, string @return)
        {
            Compiler.CompileStatement(expr, out var func);
            var tt = TestHelper.GeneratedRoslynExpression(@return,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
            // Assert.Equal(t,tt);
        }


        [Fact]
        public void DefineBooleanVariables()
        {
            var expr =
                @"
            bool boolTrue = x > 1;
            bool boolFalse = y > 1;
            if(boolTrue && boolFalse) {return 1;} else {return 0;}
            ";

            Compiler.CompileStatement(expr, out var func);

            Assert.Equal(0, func(1, 1, 0));
            Assert.Equal(0, func(2, 1, 0));
            Assert.Equal(0, func(1, 2, 0));
            Assert.Equal(1, func(2, 2, 0));
        }
    }
}