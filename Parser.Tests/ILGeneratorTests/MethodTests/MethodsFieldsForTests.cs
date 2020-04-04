using JetBrains.Annotations;

namespace Parser.Tests.ILGeneratorTests
{
    public static class MethodsFieldsForTests
    {
        [UsedImplicitly]
        public static long MethodWithoutParameters() => 1;

        [UsedImplicitly]
        public static long MethodWith1Parameter(long x) => x;

        [UsedImplicitly]
        public static long MethodWith2Parameters(long x, long y) => (x + y);

        [UsedImplicitly]
        public static long MethodWith3Parameters(long x, long y, long z) => (x + y + z);

        [UsedImplicitly]
        public static long a = 1;

        [UsedImplicitly]
        public static long b = 2;

        [UsedImplicitly]
        public static long c = 3;
    }
}