namespace Parser
{
    public class PrimaryExpression : IExpression
    {
        public static PrimaryExpression FoldedAfterMul0 = new PrimaryExpression("0") {IsFoldedAfterMul0 = true};

        public static CompilerType GetPrimaryType(string number)
        {
            var l = long.Parse(number);
            return GetPrimaryType(l);
        }

        public static CompilerType GetPrimaryType(long number)
        {
            return number >= int.MinValue && number <= int.MaxValue ? CompilerType.Int : CompilerType.Long;
        }

        public PrimaryExpression(string value, CompilerType compilerType)
        {
            Value = value;
            CompilerType = compilerType;
        }

        public PrimaryExpression(string value)
        {
            Value = value;
            CompilerType = GetPrimaryType(value);
        }

        public long LongValue => long.Parse(Value);

        public readonly string Value;
        public readonly CompilerType CompilerType;
        public ExpressionType ExpressionType { get; } = ExpressionType.Primary;

        // (x*12*14)*0 = 0; needs for example : (1/0*x) - no divide by null compile time exception, (1/0) - divide by null compile time exception
        public bool IsFoldedAfterMul0;
    }
}