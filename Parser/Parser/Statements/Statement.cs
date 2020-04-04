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

        public bool IsReturnStatement
        {
            get
            {
                if (Statements.Length == 0) return false;
                if (Statements[^1].ExpressionType == ExpressionType.Return) return true;
                var ifElseStatements = Statements.OfType<IfElseStatement>().ToArray();
                return ifElseStatements.Any(x => x.Else?.IsReturnStatement == true && x.IfTrue.IsReturnStatement);
            }
        }
    }
}