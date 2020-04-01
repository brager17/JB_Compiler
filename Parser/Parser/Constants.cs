using System.Collections.Generic;

namespace Parser
{
    public static class Constants
    {
        public static Dictionary<string, long> Dictionary = new Dictionary<string, long>()
        {
            {"int.MaxValue", int.MaxValue},
            {"int.MinValue", int.MinValue},
            {"long.MaxValue", long.MaxValue},
            {"long.MinValue", long.MinValue},
        };
    }
}