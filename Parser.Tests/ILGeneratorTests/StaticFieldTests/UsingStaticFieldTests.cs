using JetBrains.Annotations;
using Xunit;

namespace Parser.Tests.ILGeneratorTests.StaticFieldTests
{
    public class UsingStaticFieldTests
    {
        [InlineData("x*c*y*z+a-b")]
        [InlineData("x/y/z/a")]
        [InlineData("x+y+z-a")]
        [InlineData("x-y-z-a")]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b-2*c))")]
        [InlineData("(x/(y+z)+(x*y)-a*(a+b*-c))")]
        [InlineData("(x/-(a+b*c))")]
        [Theory]
        public void Parse__ExpressionWithClosedVariable__ResultAsRoslyn(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc, typeof(MethodsFieldsForTests));

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var expectedFunc);
            Assert.Equal(expected, actual);
            Assert.Equal(expectedFunc(11, 17, 43), myFunc(11, 17, 43));
        }

        [UsedImplicitly]
        public static void AddByRef(ref long x)
        {
            x++;
        }

        public static long s = 12;

        [InlineData(
            @"AddByRef(ref s);
              return s;"
        )]
        [Theory]
        public void Parse__StatementWithClosedVariable__ResultAsRoslyn(string expression)
        {
            var actual = Compiler.CompileStatement(expression, out var myFunc, GetType());

            myFunc(1, 1, 1);

            Assert.Equal(13, s);
        }

        [Fact]
        public void Parse__ChangeLocalVariable__Changed()
        {
            Compiler.CompileStatement("int i = 1; i = 2;return i;", out var func);

            Assert.Equal(2, func(0, 0, 0));
        }

        public static long FieldVariable = 1;

        [Fact]
        public void Parse__ChangeFieldVariable__Changed()
        {
            var logs = Compiler.CompileStatement("FieldVariable = 2; return FieldVariable;", out var func, GetType());

            Assert.Equal(2, func(0, 0, 0));
        }

        [Fact]
        public void Parse__ChangeMethodArgument__Changed()
        {
            Compiler.CompileStatement("x=2;return x;", out var func, GetType());

            Assert.Equal(2, func(0, 0, 0));
        }
    }
}