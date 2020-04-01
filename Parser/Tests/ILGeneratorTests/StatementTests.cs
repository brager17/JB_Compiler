using System;
using System.Linq;
using Xunit;

namespace Parser.ILGeneratorTests
{
    //todo добавить в шаблон класса для Рослина реализацию всех методов, чтобы можно было их использовать не передавая из тестов, какие именно методы нужно добавить в шаблон класса 
    public class StatementTests
    {
        [Fact]
        public void Compile__LongStatements__Correct()
        {
            string expr = "long q = 12;long w = -14;return q+w;";
            var result = TestHelper.GetParseResultStatements(expr);

            TestHelper.GeneratedStatementsMySelf(expr, out var func, GetType());
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

            TestHelper.GeneratedStatementsMySelf(expr, out var func, GetType());
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
            TestHelper.GeneratedStatementsMySelf(expr, out var func, GetType());
            TestHelper.GeneratedRoslyn(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }

        public static long d = 1;

        [Theory]
        [InlineData("int _d = d;int q=1;return _d+d-12;","_d+d-12")]
        [InlineData("int i=2;long j = Method(i,12);return j;","j")]
        public void Compile__StatementWithStaticMethodsAndFields__Correct(string expr, string returnExpr)
        {
            TestHelper.GeneratedStatementsMySelf(expr, out var func, GetType());
            TestHelper.GeneratedRoslyn(returnExpr,
                out var roslynFunc,
                statements: expr
                    .Split(';')
                    .SkipLast(2).ToArray());

            Assert.Equal(roslynFunc(1, 1, 1), func(1, 1, 1));
        }
    }
}