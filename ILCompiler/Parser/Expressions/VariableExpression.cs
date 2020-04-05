using System;

namespace Parser.Parser.Expressions
{
    [Flags]
    public enum ExpressionType
    {
        LocalVariable = 1,
        FieldVariable = 1 << 1,
        MethodArgVariable = 1 << 2,
        Primary = 1 << 3,
        Binary = 1 << 4,
        Unary = 1 << 5,
        MethodCallExpression = 1 << 6,
        VoidMethodCallStatement = 1 << 7,
        Assignment = 1 << 8,
        Logical = 1 << 9,
        Return = 1 << 10,
        IfElse = 1 << 11,
        Statement = 1 << 12,
        MethodCallParameter = 1 << 12,
        
        Variables = LocalVariable | FieldVariable | MethodArgVariable
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