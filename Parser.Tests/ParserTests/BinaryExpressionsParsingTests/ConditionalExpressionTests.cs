using Parser.Parser.Expressions;
using Xunit;

namespace Parser.Tests.ParserTests.BinaryExpressionsParsingTests
{
    public class ConditionalExpressionTests
    {
        [Fact]
        public void SimpleGreaterConditionalExpression()
        {
            var expr = "13 > 12";
            var result = TestHelper.GetParseResultExpression(expr);

            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            
            Assert.Equal(LogicalOperator.Greater, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }

        [Fact]
        public void SimpleLessConditionalExpression()
        {
            var expr = "13 < 12";
            var result = TestHelper.GetParseResultExpression(expr);
           
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.Less, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }

        [Fact]
        public void SimpleLessOrEqualConditionalExpression()
        {
            var expr = "13 <= 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.LessOrEq, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }

        [Fact]
        public void SimpleGreaterOrEqualConditionalExpression()
        {
            var expr = "13 >= 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.GreaterOrEq, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }

        [Fact]
        public void SimpleEqualConditionalExpression()
        {
            var expr = "13 == 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.Eq, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }


        [Fact]
        public void SimpleNoEqualConditionalExpression()
        {
            var expr = "13 != 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.NoEq, logical.Operator);
            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("13", ((PrimaryExpression) logical.Left).Value);

            Assert.Equal(logical.Left.ExpressionType, ExpressionType.Primary);
            Assert.Equal("12", ((PrimaryExpression) logical.Right).Value);
        }

        [Fact]
        public void CombinedConditionAndExpressions()
        {
            var expr = "13 != 12 && 13 > 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.And, logical.Operator);
            Assert.Equal(ExpressionType.Logical,logical.Left.ExpressionType);
            Assert.Equal(ExpressionType.Logical,logical.Right.ExpressionType);
        }

        [Fact]
        public void CombinedConditionOrExpressions()
        {
            var expr = "13 != 12 || 13 > 12";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.Or, logical.Operator);
            Assert.Equal(ExpressionType.Logical,logical.Left.ExpressionType);
            Assert.Equal(ExpressionType.Logical,logical.Right.ExpressionType);
        }
        
        [Fact]
        public void CombinedConditionOrAndExpressions()
        {
            var expr = "12 != 13 || 14 > 15 && 16 < 17";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.Or, logical.Operator);
            Assert.Equal(ExpressionType.Logical,logical.Left.ExpressionType);
            Assert.Equal(ExpressionType.Logical,logical.Right.ExpressionType);
        }
        
        
        [Fact]
        // == has less priority than != or >
        public void PriorityTest()
        {
            var expr = "12 != 13 == 14 > 15";
            var result = TestHelper.GetParseResultExpression(expr);
            
            Assert.Equal(ExpressionType.Logical, result.ExpressionType);
            var logical = (LogicalBinaryExpression) result;
            Assert.Equal(LogicalOperator.Eq, logical.Operator);
            Assert.Equal(ExpressionType.Logical,logical.Left.ExpressionType);
            Assert.Equal(ExpressionType.Logical,logical.Right.ExpressionType);
        }
    }
}