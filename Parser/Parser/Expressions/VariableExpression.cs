namespace Parser
{
    // todo разделить для Expression'ов и для Statement'ов
    public enum ExpressionType
    {
        LocalVariable = 1,
        FieldVariable,
        MethodArgVariable,
        Primary,
        Binary,
        Unary,
        MethodCallExpression,
        VoidMethodCallStatement,
        Assignment,
        Logical,
        Return,
        IfElse,
        Statement
    }

    public interface IExpression
    {
        public ExpressionType ExpressionType { get; }
        public CompilerType ReturnType { get; }
    }

    public enum CompilerType
    {
        Int = 1,

        // UInt,
        Long,
        Bool,
        Void
    }

    public class VariableExpression : IExpression
    {
        public string Name;

        public VariableExpression(string name, CompilerType compilerType, bool byReference = false)
        {
            Name = name;
            ReturnType = compilerType;
            ByReference = byReference;
        }

        public VariableExpression(string name, CompilerType compilerType, bool byReference,
            ExpressionType expressionType)
        {
            Name = name;
            ReturnType = compilerType;
            ExpressionType = expressionType;
            ByReference = byReference;
        }


        public readonly bool ByReference;
        public ExpressionType ExpressionType { get; }
        public CompilerType ReturnType { get; }
    }
}