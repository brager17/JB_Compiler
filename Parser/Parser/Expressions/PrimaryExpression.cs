using System;

namespace Parser
{
    public class PrimaryExpression : IExpression
    {
        public static PrimaryExpression FoldedAfterMul0 = new PrimaryExpression("0") {IsFoldedAfterMul0 = true};

        public static bool GetPrimaryType(string constant, out CompilerType compilerType)
        {
            compilerType = default;

            if (int.TryParse(constant, out _))
            {
                compilerType = CompilerType.Int;
                return true;
            }

            if (long.TryParse(constant, out _))
            {
                compilerType = CompilerType.Long;
                return true;
            }

            if (bool.TryParse(constant, out _))
            {
                compilerType = CompilerType.Bool;
                return true;
            }

            return false;
        }

        public PrimaryExpression(string value, CompilerType compilerType)
        {
            Value = value;
            ReturnType = compilerType;
        }

        public PrimaryExpression(string value)
        {
            Value = value;
            if (!GetPrimaryType(value, out var type)) throw new Exception("Integral constant is too large");
            ReturnType = type;
        }


        public readonly string Value;
        public ExpressionType ExpressionType { get; } = ExpressionType.Primary;
        public CompilerType ReturnType { get; }

        // (x*12*14)*0 = 0; needs for example : (1/0*x) - no divide by null compile time exception, (1/0) - divide by null compile time exception
        public bool IsFoldedAfterMul0;
    }
}