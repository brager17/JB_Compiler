using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Xunit;

namespace Parser.Tests.ILGeneratorTests
{
    public class PositiveCallMethodsTests
    {
        [Theory]
        [InlineData("1+MethodWithoutParameters()")]
        [InlineData("1+MethodWith1Parameter(3)")]
        [InlineData("1+MethodWith2Parameters(1,3)")]
        public void ExpressionWithCallsStaticMethods(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x)")]
        [InlineData("1+MethodWith3Parameters(x,y,z)")]
        [InlineData("1+MethodWith3Parameters(a,1324,c)")]
        public void Parse__StaticMethodWithLocalVariableParameters__Correct(string expression)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expression, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynGeneratedFunc);

            Assert.Equal(expected, actual);
            Assert.Equal(roslynGeneratedFunc(1, 2, 3), func(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x+12+y)")]
        [InlineData("1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14)")]
        [InlineData(
            "1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14+MethodWithoutParameters()*MethodWith3Parameters(x,y,z))")]
        public void Parse__StaticMethodWithExpressionParameter(string expr)
        {
            var actual = TestHelper.GeneratedExpressionMySelf(expr, out var func);

            var expected = TestHelper.GeneratedRoslynExpression(expr, out var roslynGeneratedFunc);

            Assert.Equal(expected, actual);
            Assert.Equal(roslynGeneratedFunc(1, 2, 3), func(1, 2, 3));
        }

        [UsedImplicitly]
        public static void AddByRef(ref long x)
        {
            x++;
        }

        [UsedImplicitly]
        public static int s = 1;

        [Fact]
        public void Parse__VoidRefMethodCall__MethodWasCalled()
        {
            var exprWithArgX =
                $@"
                AddByRef(ref x);
                return x;";

            TestHelper.GeneratedStatementsMySelf(exprWithArgX, out var func, @this: GetType());
            var r = func(1, 1, 1);
            Assert.Equal(2, r);
        }
    }
}