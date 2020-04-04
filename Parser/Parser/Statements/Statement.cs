using System.Linq;

namespace Parser
{
    public class Statement : IStatement
    {
        public ExpressionType ExpressionType { get; } = ExpressionType.Statement;

        public readonly IStatement[] Statements;

        public Statement(IStatement[] statements)
        {
            Statements = statements;
        }

        public bool IsReturnStatement => Statements.Last().ExpressionType == ExpressionType.Return;
    }
}