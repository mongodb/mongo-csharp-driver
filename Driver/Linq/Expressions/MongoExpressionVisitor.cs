/* Copyright 2010-2012 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A base class for classes that visit MongoExpressions.
    /// </summary>
    public abstract class MongoExpressionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Initializes an instance of the MongoExpressionVisitor class.
        /// </summary>
        protected MongoExpressionVisitor()
        {
        }

        /// <summary>
        /// Visits an Expression (which might be a MongoExpression).
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }

            switch ((MongoExpressionType)node.NodeType)
            {
                case MongoExpressionType.Collection:
                    return VisitCollection((MongoCollectionExpression)node);
                case MongoExpressionType.Field:
                    return VisitField((MongoFieldExpression)node);
                case MongoExpressionType.Projection:
                    return VisitProjection((MongoProjectionExpression)node);
                case MongoExpressionType.Select:
                    return VisitSelect((MongoSelectExpression)node);
                default:
                    return base.Visit(node);
            }
        }

        /// <summary>
        /// Visits a MongoCollectionExpression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected virtual Expression VisitCollection(MongoCollectionExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MongoFieldExpression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected virtual Expression VisitField(MongoFieldExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MongoProjectionExpression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected virtual Expression VisitProjection(MongoProjectionExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MongoSelectExpression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected virtual Expression VisitSelect(MongoSelectExpression node)
        {
            throw new NotImplementedException();
        }
    }
}
