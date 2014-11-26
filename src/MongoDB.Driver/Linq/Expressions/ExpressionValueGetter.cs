using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq.Expressions
{
    /// <summary>
    /// Visit an expression tree to extract a value
    /// </summary>
    public class ExpressionValueGetter : ExpressionVisitor
    {

        /// <summary>
        /// Gets the constant expression which hold the value
        /// </summary>
        public ConstantExpression Result
        { get; private set; }

        /// <summary>
        /// Visit the <paramref name="node"/> to find the value
        /// </summary>
        /// <param name="node"></param>
        public void VisitNode(Expression node)
        {
            this.Visit(node);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            this.Result = node;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            this.Result = Expression.Constant(Expression.Lambda(node).Compile().DynamicInvoke());
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            this.Result = Expression.Constant(Expression.Lambda(node).Compile().DynamicInvoke());
            return null;
        }
    }
}
