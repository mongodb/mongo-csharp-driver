﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// An implementation of IQueryable{{T}} for querying a MongoDB collection.
    /// This class has been named MongoQueryable instead of MongoQuery to avoid confusion with IMongoQuery.
    /// </summary>
    public class MongoQueryable<T> : IOrderedQueryable<T>
    {
        // private fields
        private MongoQueryProvider _provider;
        private Expression _expression;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoQueryable class.
        /// </summary>
        public MongoQueryable(MongoQueryProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            _provider = provider;
            _expression = Expression.Constant(this);
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryable class.
        /// </summary>
        public MongoQueryable(MongoQueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            _provider = provider;
            _expression = expression;
        }

        // public methods
        /// <summary>
        /// Gets an enumerator for the results of a MongoDB LINQ query.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_provider.Execute(_expression)).GetEnumerator();
        }

        // explicit implementation of IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_provider.Execute(_expression)).GetEnumerator();
        }

        // explicit implementation of IQueryable
        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        Expression IQueryable.Expression
        {
            get { return _expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return _provider; }
        }
    }
}
