using System.Reflection;
using Parser.Utils;

namespace Parser.Parser.Expressions
{
    public class FieldVariableExpression : VariableExpression
    {
        public FieldVariableExpression(string name, FieldInfo fieldInfo,bool byReference=false) :
            base(name, fieldInfo.FieldType.GetRoslynType(), byReference,ExpressionType.FieldVariable)
        {
            FieldInfo = fieldInfo;
        }

        public readonly FieldInfo FieldInfo;
    }
}