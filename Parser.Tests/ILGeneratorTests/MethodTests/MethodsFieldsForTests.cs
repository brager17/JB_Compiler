using JetBrains.Annotations;

namespace Parser.Tests.ILGeneratorTests.MethodTests
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
        public static void AddByRef(ref long x)
        {
            x++;
        }

        [UsedImplicitly]
        public static long a = 1;

        [UsedImplicitly]
        public static long b = 2;

        [UsedImplicitly]
        public static long c = 3;


        internal const string RunnerClassTemplate = @"
using System.Runtime.CompilerServices;
namespace RunnerNamespace
{
     public class Runner
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWithoutParameters() => 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith1Parameter(long x) => x;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith2Parameters(long x, long y) => (x + y);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long MethodWith3Parameters(long x, long y, long z) => (x + y + z);
        
        public static long a = 1;
        public static long b = 2;
        public static long c = 3;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static long Run(long x, long y, long z)
        {
            {statements};
            return {expr};
        }
    }
}";
    }
    
  ;

}