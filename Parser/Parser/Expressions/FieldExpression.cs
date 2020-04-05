using System.Reflection;
using Compiler;

namespace Parser
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