namespace Parser
{
    public class ReturnStatement:IStatement
    {
        public IExpression Returned { get; }

        public ReturnStatement(IExpression returned)
        {
            Returned = returned;
        }

        public ExpressionType ExpressionType { get; } = ExpressionType.Return;
    }
}