/* Copyright 2010-2013 10gen Inc.
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
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Performs bottom-up analysis to find maximal subtrees that satisfy a predicate.
    /// </summary>
    class Nominator : ExpressionVisitor
    {
        Func<Expression, bool> _predicate;
        HashSet<Expression> _candidates;
        bool _isBlocked;

        internal Nominator(Func<Expression, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            _predicate = predicate;
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            _candidates = new HashSet<Expression>();
            this.Visit(expression);
            return _candidates;
        }

        protected override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool wasBlocked = _isBlocked;
                _isBlocked = false;
                base.Visit(expression);
                if (!_isBlocked)
                {
                    if (_predicate(expression))
                    {
                        _candidates.Add(expression);
                    }
                    else
                    {
                        _isBlocked = true;
                    }
                }
                _isBlocked |= wasBlocked;
            }
            return expression;
        }
    }
}
