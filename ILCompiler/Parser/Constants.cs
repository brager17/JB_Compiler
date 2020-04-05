using System.Collections.Generic;
using Parser.Parser.Expressions;

namespace Parser.Parser
{
    public static class Constants
    {
        public static Dictionary<string, (string toString, CompilerType type)> Dictionary =
            new Dictionary<string, (string , CompilerType )>()
            {
                {"int.MinValue", (int.MinValue.ToString(), CompilerType.Int)},
                {"int.MaxValue", (int.MaxValue.ToString(), CompilerType.Int)},
                {"long.MinValue", ( long.MinValue.ToString(), CompilerType.Long)},
                {"long.MaxValue", ( long.MaxValue.ToString(), CompilerType.Long)},
                {"true", ("true", CompilerType.Bool)},
                {"false", ("false", CompilerType.Bool)},
            };
    }
}