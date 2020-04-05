using System.Collections.Generic;
using System.Reflection;
using Parser.Lexer;
using Parser.Parser.Expressions;

namespace Parser.Parser
{
    public class ParserContext
    {
        public ParserContext(IReadOnlyList<Token> tokens, Dictionary<string, CompilerType> methodParameters,
            Dictionary<string, FieldInfo> closureFields,
            Dictionary<string,MethodInfo> closureMethods,
            bool constantFolding)
        {
            Tokens = tokens;
            MethodParameters = methodParameters;
            ClosureFields = closureFields ?? new Dictionary<string, FieldInfo>();
            ClosureMethods = closureMethods;
            ConstantFolding = constantFolding;
        }

        public readonly IReadOnlyList<Token> Tokens;
        public readonly Dictionary<string, CompilerType> MethodParameters;
        public readonly Dictionary<string, FieldInfo> ClosureFields;
        public readonly Dictionary<string, MethodInfo> ClosureMethods;
        public readonly bool ConstantFolding;
    }
}