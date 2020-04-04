using JetBrains.Annotations;
using Parser.Exceptions;
using Xunit;

namespace Parser.Tests.ILGeneratorTests
{
    public class NegativeCallMethodsTests
    {
        [UsedImplicitly]
        public static int IntMethod(int a)
        {
            return 1;
        }

        [UsedImplicitly]
        public static int LongMethod(long a)
        {
            return 1;
        }

        [UsedImplicitly]
        public static int BoolMethod(bool a)
        {
            return 1;
        }

        [Theory]
        [InlineData("return IntMethod(true);")]
        [InlineData("long l = 2147483648;return IntMethod(l);")]
        [InlineData("return IntMethod(2147483648);")]
        [InlineData("return LongMethod(false);")]
        [InlineData("return BoolMethod(1);")]
        public void Parse__CallMethodWithInvalidParams(string exr)
        {
            var exception = Assert.Throws<CompileException>
                (() => TestHelper.GeneratedStatementsMySelf(exr, out var func, @this: GetType()));
            
        }

        [Theory]
        [InlineData("return MethodWith2Parameters(1);")]
        [InlineData("return MethodWith2Parameters(1,2,3);")]
        public void Parse__CallMethodWithInvalidParamsAmount(string expr)
        {
            var exception = Assert.Throws<CompileException>
                (() => TestHelper.GeneratedStatementsMySelf(expr, out var func, @this: GetType()));
            Assert.Equal(exception.Message,"MethodWith2Parameters method passed an incorrect number of parameters");
        }
    }
}