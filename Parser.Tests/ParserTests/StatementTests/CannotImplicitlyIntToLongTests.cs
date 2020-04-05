using Parser.Parser.Exceptions;
using Parser.Tests.ILGeneratorTests.MethodTests;
using Xunit;

namespace Parser.Tests.ParserTests.StatementTests
{
    public class CannotImplicitlyIntToLongTests
    {
        [Theory]
        [InlineData("long q=12; int w = q;")]
        [InlineData("int q = long.MaxValue;")]
        public void Parse__ImplicitIntToLong__ThrowError(string expr)
        {
            var exception = Assert.Throws<CompileException>(() => Compiler.CompileStatement(expr, out _));
            Assert.Equal("Cannot implicitly convert type 'long ' to int", exception.Message);
        }

        [Theory]
        [InlineData("int q = MethodWithoutParameters();return 1;")]
        public void Parse__ImplicitIntToLongAssignmentMethod__ThrowError(string expr)
        {
            var exception = Assert.Throws<CompileException>(() =>
                Compiler.CompileStatement(expr, out _, typeof(MethodsFieldsForTests)));
            Assert.Equal("Cannot implicitly convert type 'long ' to int", exception.Message);
        }
    }
}