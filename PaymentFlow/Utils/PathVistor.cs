using System.Linq.Expressions;
using System.Reflection;

namespace PaymentFlow.Utils
{
    public class PathVistor : ExpressionVisitor
    {
        internal readonly List<string> Path = new List<string>();

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo)
                Path.Add(node.Member.Name);

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "get_Item")
            {
                var arg = Expression.Lambda<Func<int>>(node.Arguments[0], null).Compile()();
                Path.Add(arg.ToString());
            }

            if (node.Method.Name == "ElementAt" && node.Arguments.Count == 2)
            {
                var arg = Expression.Lambda<Func<int>>(node.Arguments[1], null).Compile()();
                Path.Add(arg.ToString());
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex && Visit(node.Right) is ConstantExpression right)
                Path.Add(right.Value.ToString());

            return base.VisitBinary(node);
        }
    }
}
