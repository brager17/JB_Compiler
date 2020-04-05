using JetBrains.Annotations;
using Xunit;

namespace Parser.Tests.ILGeneratorTests.MethodTests
{
    public class PositiveCallMethodsTests
    {
        [Theory]
        [InlineData("1+MethodWithoutParameters()")]
        [InlineData("1+MethodWith1Parameter(3)")]
        [InlineData("1+MethodWith2Parameters(1,3)")]
        public void ExpressionWithCallsStaticMethods(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var func, typeof(MethodsFieldsForTests));

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var monoFunc);

            Assert.Equal(func(1, 2, 3), monoFunc(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x)")]
        [InlineData("1+MethodWith3Parameters(x,y,z)")]
        [InlineData("1+MethodWith3Parameters(a,1324,c)")]
        public void Parse__StaticMethodWithLocalVariableParameters__ResultAsRoslyn(string expression)
        {
            var ilLogs = Compiler.CompileExpression(expression, out var func, typeof(MethodsFieldsForTests));

            var roslynGeneratedIl = TestHelper.GeneratedRoslynExpression(expression, out var roslynGeneratedFunc);

            Assert.Equal(roslynGeneratedIl, ilLogs);
            Assert.Equal(roslynGeneratedFunc(1, 2, 3), func(1, 2, 3));
        }

        [Theory]
        [InlineData("1+MethodWith1Parameter(x+12+y)")]
        [InlineData("1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14)")]
        [InlineData(
            "1+MethodWith1Parameter(x+12+y)*MethodWith1Parameter(12*14+MethodWithoutParameters()*MethodWith3Parameters(x,y,z))")]
        public void Parse__StaticMethodWithExpressionParameter(string expr)
        {
            var actual = Compiler.CompileExpression(expr, out var func, typeof(MethodsFieldsForTests));

            var expected = TestHelper.GeneratedRoslynExpression(expr, out var roslynGeneratedFunc);

            Assert.Equal(expected, actual);
            Assert.Equal(roslynGeneratedFunc(1, 2, 3), func(1, 2, 3));
        }
        
        [UsedImplicitly]
        public static void AddByRef(ref long x)
        {
            x++;
        }


        [Fact]
        public void Parse__VoidRefMethodCall__MethodWasCalled()
        {
            var exprWithArgX =
                $@"
                AddByRef(ref x);
                return x;";

            Compiler.CompileStatement(exprWithArgX, out var func, typeof(MethodsFieldsForTests));
            var r = func(1, 1, 1);
            Assert.Equal(2, r);
        }
        
        
        public static long BooleanParameterTest(bool param)
        {
            return param ? 1 : 0;
        }

        [Fact]
        public void Compile__MethodWithBooleanParameter__CalledCorrect()
        {
            var expr = "bool q = true; if(x == 1) {q = true;} else {q = false;} return BooleanParameterTest(q);";

            var result = Compiler.CompileStatement(expr, GetType());

            Assert.Equal(1, result(1, 0, 0));
            Assert.Equal(0, result(0, 0, 0));
        }
    }
}