using System;
using Parser.Lexer;
using Parser.Parser.Expressions;

namespace Parser.Utils
{
    public static class ExpressionExtensions
    {
        public static bool TryCast<T>(this IExpression expression, out T value) where T : IExpression
        {
            switch (expression.ExpressionType)
            {
                case ExpressionType.LocalVariable when typeof(T) == typeof(LocalVariableExpression):
                case ExpressionType.FieldVariable when typeof(T) == typeof(FieldVariableExpression):
                case ExpressionType.MethodArgVariable when typeof(T) == typeof(MethodArgumentVariableExpression):
                case ExpressionType.Primary when typeof(T) == typeof(PrimaryExpression):
                case ExpressionType.Binary when typeof(T) == typeof(BinaryExpression):
                case ExpressionType.Unary when typeof(T) == typeof(UnaryExpression):
                case ExpressionType.Logical when typeof(T) == typeof(LogicalBinaryExpression):
                case ExpressionType.MethodCallExpression when typeof(T) == typeof(MethodCallExpression):
                    value = (T) expression;
                    return true;
                default:
                    value = default;
                    return false;
            }
        }

        public static long AsLong(this PrimaryExpression primaryExpression) => long.Parse(primaryExpression.Value);
        public static int AsInt(this PrimaryExpression primaryExpression) => int.Parse(primaryExpression.Value);
        public static bool AsBool(this PrimaryExpression primaryExpression) => bool.Parse(primaryExpression.Value);

        public static Type GetCSharpType(this CompilerType compilerType) => compilerType switch
        {
            CompilerType.Bool => typeof(bool),
            CompilerType.Int => typeof(int),
            CompilerType.Long => typeof(long),
            _ => throw new ArgumentOutOfRangeException()
        };

        public static CompilerType TokenToCompilerType(this TokenType keywordType) => keywordType switch
        {
            TokenType.IntWord => CompilerType.Int,
            TokenType.LongWord => CompilerType.Long,
            TokenType.BoolWord => CompilerType.Bool,
        };

       

        public static CompilerType GetRoslynType(this Type type)
        {
            return type.Name switch
            {
                "Int32" => CompilerType.Int,
                "Int32&" => CompilerType.Int,
                "Int64&" => CompilerType.Long,
                "Int64" => CompilerType.Long,
                "Boolean" => CompilerType.Bool,
                "Boolean&" => CompilerType.Bool,
                "Void" => CompilerType.Void,
                _ => 0
            };
        }
    }
}