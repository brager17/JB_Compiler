using Parser.Tests.ILGeneratorTests.MethodTests;
using Xunit;
using Xunit.Abstractions;

namespace Parser.Tests.ILGeneratorTests.BinaryOperations
{
    public class BinaryOperationsTests
    {
        [Theory]
        [InlineData("x")]
        public void ReturnX(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynFunc);

            Assert.Equal(roslynFunc(11, 17, 43), myFunc(11, 17, 43));
            Assert.Equal(expected, actual);
        }


        [Theory]
        [InlineData("x+y")]
        [InlineData("x-y")]
        [InlineData("x*y")]
        [InlineData("x/y")]
        [InlineData("-x+y")]
        [InlineData("-x-y")]
        [InlineData("-x*y")]
        [InlineData("-x/y")]
        [InlineData("x+(-y)")]
        [InlineData("x-(-y)")]
        [InlineData("x*(-y)")]
        [InlineData("x/(-y)")]
        public void Parse__VariableOperationsTest__ResultAsRoslyn(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynFunc);

            Assert.Equal(roslynFunc(11, 17, 43), myFunc(11, 17, 43));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("12*x")]
        [InlineData("12/x")]
        [InlineData("12-x")]
        [InlineData("12+x")]
        public void Parse__ConstantWithVariableOperations__ResultAsRoslyn(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynFunc);

            Assert.Equal(roslynFunc(11, 17, 43), myFunc(11, 17, 43));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("x*long.MaxValue", 2)]
        [InlineData("x+long.MaxValue", 1)]
        [InlineData("x+long.MinValue", -1)]
        [InlineData("x+(-9223372036854775808)", -1)]
        [InlineData("x+int.MinValue", -1)]
        [InlineData("x+(-2147483648)", -1)]
        public void Parse__OperationsWithOverflow__ResultAsRoslyn(string expression, long x)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc);

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynFunc);

            Assert.Equal(roslynFunc(11, 17, 43), myFunc(11, 17, 43));
            Assert.Equal(expected, actual);
        }


        [InlineData("x*y*z")]
        [InlineData("x/y/z")]
        [InlineData("x+y+z")]
        [InlineData("x-y-z")]
        [InlineData("(x/(y+z)+(x*y))")]
        [Theory]
        public void Expression(string expression)
        {
            var actual = Compiler.CompileExpression(expression, out var myFunc,typeof(MethodsFieldsForTests));

            var expected = TestHelper.GeneratedRoslynExpression(expression, out var roslynFunc);

            Assert.Equal(roslynFunc(11, 17, 43), myFunc(11, 17, 43));
            Assert.Equal(expected, actual);
        }


        [Fact]
        public void Parse__MultiplyMethodArguments__ResultAsRoslyn()
        {
            var method = Compiler.CompileExpression("x*y*z*x");
            for (var x = 0; x < 10; x++)
            for (var y = 0; y < 10; y++)
            for (var z = 0; z < 10; z++)
                Assert.Equal(x * y * z * x, method.Invoke(x, y, z));
        }
    }
}