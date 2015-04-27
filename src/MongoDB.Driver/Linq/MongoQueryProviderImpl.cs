/* Copyright 2010-2014 MongoDB Inc.
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Processors;

namespace MongoDB.Driver.Linq
{
    internal class MongoQueryProviderImpl<TDocument> : IMongoQueryProvider
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;

        public MongoQueryProviderImpl(IMongoCollection<TDocument> collection, AggregateOptions options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _options = Ensure.IsNotNull(options, "options");
        }

        public AggregateOptions Options
        {
            get { return _options; }
        }

        public QueryableExecutionModel BuildExecutionModel(Expression expression)
        {
            return QueryableExecutionModelBuilder.Build(
                Prepare(expression),
                _collection.Settings.SerializerRegistry);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MongoQueryableImpl<TDocument, TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Ensure.IsNotNull(expression, "expression");

            var elementType = GetElementType(expression.Type);

            try
            {
                return (IQueryable)Activator.CreateInstance(
                    typeof(MongoQueryableImpl<,>).MakeGenericType(typeof(TDocument), elementType),
                    new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var result = Execute(expression);
            return (TResult)result;
        }

        public object Execute(Expression expression)
        {
            var executionPlan = QueryableExecutionBuilder.Build(
                Prepare(expression),
                Expression.Constant(this),
                _collection.Settings.SerializerRegistry);

            var efn = Expression.Lambda(executionPlan);
            return efn.Compile().DynamicInvoke(null);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default(CancellationToken))
        {
            var executionPlan = QueryableExecutionBuilder.BuildAsync(
                Prepare(expression),
                Expression.Constant(this),
                Expression.Constant(cancellationToken),
                _collection.Settings.SerializerRegistry);

            var efn = Expression.Lambda(executionPlan);
            return (Task<TResult>)efn.Compile().DynamicInvoke(null);
        }

        private object Execute(QueryableExecutionModel model)
        {
            return model.Execute(_collection, _options);
        }

        private Task ExecuteAsync(QueryableExecutionModel model, CancellationToken cancellationToken)
        {
            return model.ExecuteAsync(_collection, _options, cancellationToken);
        }

        private Expression Prepare(Expression expression)
        {
            expression = PartialEvaluator.Evaluate(expression);
            expression = Transformer.Transform(expression);
            expression = ProjectionBinder.Bind(expression, _collection.DocumentSerializer, _collection.Settings.SerializerRegistry);

            return expression;
        }

        private static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) { return seqType; }
            return ienum.GetGenericArguments()[0];
        }

        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
            {
                return null;
            }

            if (seqType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            }

            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }

            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) { return ienum; }
                }
            }

            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }

            return null;
        }
    }
}