using System.Collections.Generic;

namespace Parser
{
    public static class Constants
    {
        public static Dictionary<string, (string, CompilerType)> Dictionary =
            new Dictionary<string, (string, CompilerType)>()
            {
                {"int.MinValue", (int.MinValue.ToString(), CompilerType.Int)},
                {"int.MaxValue", (int.MaxValue.ToString(), CompilerType.Int)},
                // {"uint.MinValue", ( uint.MinValue.ToString(), CompilerType.UInt)},
                // {"uint.MaxValue", ( uint.MaxValue.ToString(), CompilerType.UInt)},
                {"long.MinValue", ( long.MinValue.ToString(), CompilerType.Long)},
                {"long.MaxValue", ( long.MaxValue.ToString(), CompilerType.Long)},
            };
    }
}