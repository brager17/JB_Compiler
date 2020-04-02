using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Parser.ILGeneratorTests
{
    //todo добавить в шаблон класса для Рослина реализацию всех методов, чтобы можно было их использовать не передавая из тестов, какие именно методы нужно добавить в шаблон класса 
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

            TestHelper.GeneratedStatementsMySelf(expr, out var func);
            TestHelper.GeneratedRoslyn("q+w",
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
            var result = TestHelper.GetParseResultStatements(expr);

            TestHelper.GeneratedStatementsMySelf(expr, out var func);
            TestHelper.GeneratedRoslyn("q+w",
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
            TestHelper.GeneratedStatementsMySelf(expr, out var func);
            TestHelper.GeneratedRoslyn(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        public static long d = 1;

        [Theory]
        [InlineData("long _d = a;int q=1;return _d+a-12;", "_d+a-12")]
        [InlineData("int i=2;long j = MethodWith2Parameters(i,12);return j;", "j")]
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
            TestHelper.GeneratedStatementsMySelf(expr, out var func);
            TestHelper.GeneratedRoslyn(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        [Theory]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;return q;", "q")]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;int w = int.MaxValue-2;return q+w;", "q+w")]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;int w = int.MaxValue-2;return q*w;", "q*w")]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;int w = int.MaxValue-2;return q/w;", "q/w")]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;int w = int.MaxValue-2;return q-w;", "q-w")]
        [InlineData("uint u = 1;uint q = int.MaxValue+u;long w = long.MaxValue;return q+w;", "q+w")]
        public void Test(string expr, string @return)
        {
            var t=TestHelper.GeneratedStatementsMySelf(expr, out var func);
            var tt=TestHelper.GeneratedRoslyn(@return,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        public long Method()
        {
            uint u = 1;
            uint q = int.MaxValue + u;
            int w = int.MaxValue - 2;
            return q + w;
        }

        [Fact]
        public void BumpTest()
        {
            // for (int i = 0; i < 10; i++)
            // {
            //     var (statements, ret) = new TestCasesGenerator().GenerateRandomStatements(20);
            //     var expr = string.Join(";", statements) + $";\nreturn {ret};";
            //     try
            //     {
            //         Func<long, long, long, long> roslyn = null;
            //         Func<long, long, long, long> func = null;
            //         try
            //         {
            //             TestHelper.GeneratedRoslyn(ret, out roslyn, statements);
            //         }
            //         catch (Exception ex)
            //         {
            //             Assert.Throws<Exception>(() => TestHelper.GeneratedStatementsMySelf(expr, out func));
            //             continue;
            //         }
            //
            //         TestHelper.GeneratedStatementsMySelf(expr, out func);
            //         long my = default;
            //         try
            //         {
            //             my = func(1, 1, 1);
            //         }
            //         catch (OverflowException)
            //         {
            //             Assert.Throws<OverflowException>(() => roslyn(1, 1, 1));
            //             continue;
            //         }
            //         catch (DivideByZeroException)
            //         {
            //             Assert.Throws<DivideByZeroException>(() => roslyn(1, 1, 1));
            //             continue;
            //         }
            //
            //         Assert.Equal(roslyn(1, 1, 1), my);
            //     }
            //     catch (Exception ex)
            //     {
            //         _testOutputHelper.WriteLine(ex.Message);
            //         _testOutputHelper.WriteLine("statements");
            //         foreach (var statement in statements)
            //         {
            //             _testOutputHelper.WriteLine(statement);
            //         }
            //
            //         throw;
            //     }
            // }
        }
    }
}