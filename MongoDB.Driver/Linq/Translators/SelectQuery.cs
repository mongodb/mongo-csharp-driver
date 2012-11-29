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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a LINQ query that has been translated to an equivalent MongoDB Find query.
    /// </summary>
    public class SelectQuery : TranslatedQuery
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private LambdaExpression _where;
        private Type _ofType;
        private List<OrderByClause> _orderBy;
        private LambdaExpression _projection;
        private int? _skip;
        private int? _take;
        private Func<IEnumerable, object> _elementSelector; // used for First, Last, etc...
        private LambdaExpression _distinct;
        private Expression _lastExpression;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoLinqFindQuery class.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        /// <param name="documentType">The document type.</param>
        public SelectQuery(MongoCollection collection, Type documentType)
            : base(collection, documentType)
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
        }

        // public properties
        /// <summary>
        /// Gets the final result type if an OfType query operator was used (otherwise null).
        /// </summary>
        public Type OfType
        {
            get { return _ofType; }
        }

        /// <summary>
        /// Gets a list of Expressions that defines the sort order (or null if not specified).
        /// </summary>
        public ReadOnlyCollection<OrderByClause> OrderBy
        {
            get { return (_orderBy == null) ? null :_orderBy.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the Expression that defines the projection (or null if not specified).
        /// </summary>
        public LambdaExpression Projection
        {
            get { return _projection; }
        }

        /// <summary>
        /// Gets the Expression that defines how many documents to skip (or null if not specified).
        /// </summary>
        public int? Skip
        {
            get { return _skip; }
        }

        /// <summary>
        /// Gets the Expression that defines how many documents to take (or null if not specified);
        /// </summary>
        public int? Take
        {
            get { return _take; }
        }

        /// <summary>
        /// Gets the LambdaExpression that defines the where clause (or null if not specified).
        /// </summary>
        public LambdaExpression Where
        {
            get { return _where; }
        }

        // public methods
        /// <summary>
        /// Creates an IMongoQuery from the where clause (returns null if no where clause was specified).
        /// </summary>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BuildQuery()
        {
            if (_where == null)
            {
                return null;
            }

            // TODO: check lambda for proper type

            var predicateTranslator = new PredicateTranslator(_serializationInfoHelper);
            var body = _where.Body;
            return predicateTranslator.BuildQuery(body);
        }

        /// <summary>
        /// Executes the translated Find query.
        /// </summary>
        /// <returns>The result of executing the translated Find query.</returns>
        public override object Execute()
        {
            if (_take.HasValue && _take.Value == 0)
            {
                var type = _ofType ?? DocumentType;

                return typeof(Enumerable).GetMethod("Empty").MakeGenericMethod(type).Invoke(null, null);
            }

            var query = BuildQuery();

            if (_distinct != null)
            {
                return ExecuteDistinct(query);
            }

            var cursor = Collection.FindAs(DocumentType, query);

            if (_orderBy != null)
            {
                var sortBy = new SortByDocument();
                foreach (var clause in _orderBy)
                {
                    var keyExpression = clause.Key.Body;
                    var serializationInfo = _serializationInfoHelper.GetSerializationInfo(keyExpression);
                    var direction = (clause.Direction == OrderByDirection.Descending) ? -1 : 1;
                    sortBy.Add(serializationInfo.ElementName, direction);
                }
                cursor.SetSortOrder(sortBy);
            }

            if (_skip != null)
            {
                cursor.SetSkip(_skip.Value);
            }

            if (_take != null)
            {
                cursor.SetLimit(_take.Value);
            }

            var projection = _projection;
            if (_ofType != null)
            {
                if (projection == null)
                {
                    var paramExpression = Expression.Parameter(DocumentType, "x");
                    var convertExpression = Expression.Convert(paramExpression, _ofType);
                    projection = Expression.Lambda(convertExpression, paramExpression);
                }
                else
                {
                    var paramExpression = Expression.Parameter(DocumentType, "x");
                    var convertExpression = Expression.Convert(paramExpression, _ofType);
                    var body = ExpressionParameterReplacer.ReplaceParameter(projection.Body, projection.Parameters[0], convertExpression);
                    projection = Expression.Lambda(body, paramExpression);
                }
            }

            IProjector projector;
            if (projection == null)
            {
                var projectorType = typeof(IdentityProjector<>).MakeGenericType(DocumentType);
                projector = (IProjector)Activator.CreateInstance(projectorType, cursor);
            }
            else
            {
                var lambdaType = projection.GetType();
                var delegateType = lambdaType.GetGenericArguments()[0];
                var sourceType = delegateType.GetGenericArguments()[0];
                var resultType = delegateType.GetGenericArguments()[1];
                var projectorType = typeof(Projector<,>).MakeGenericType(sourceType, resultType);
                var compiledProjection = projection.Compile();
                projector = (IProjector)Activator.CreateInstance(projectorType, cursor, compiledProjection);
            }

            if (_elementSelector != null)
            {
                return _elementSelector(projector);
            }
            else
            {
                return projector;
            }
        }

        /// <summary>
        /// Translates a LINQ query expression tree.
        /// </summary>
        /// <param name="expression">The LINQ query expression tree.</param>
        public void Translate(Expression expression)
        {
            // when we reach the original MongoQueryable<TDocument> we're done
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                if (constantExpression.Type == typeof(MongoQueryable<>).MakeGenericType(DocumentType))
                {
                    return;
                }
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                TranslateMethodCall(methodCallExpression);
                return;
            }

            var message = string.Format("Don't know how to translate expression: {0}.", ExpressionFormatter.ToString(expression));
            throw new NotSupportedException(message);
        }

        // private methods
        private void CombinePredicateWithWhereClause(MethodCallExpression methodCallExpression, LambdaExpression predicate)
        {
            if (predicate != null)
            {
                if (_projection != null)
                {
                    var message = string.Format("{0} with predicate after a projection is not supported.", methodCallExpression.Method.Name);
                    throw new NotSupportedException(message);
                }

                if (_where == null)
                {
                    _where = predicate;
                    return;
                }

                if (_where.Parameters.Count != 1)
                {
                    throw new MongoInternalException("Where lambda expression should have one parameter.");
                }
                var whereBody = _where.Body;
                var whereParameter = _where.Parameters[0];

                if (predicate.Parameters.Count != 1)
                {
                    throw new MongoInternalException("Predicate lambda expression should have one parameter.");
                }
                var predicateBody = predicate.Body;
                var predicateParameter = predicate.Parameters[0];

                // when using OfType the parameter types might not match (but they do have to be compatible)
                ParameterExpression parameter;
                if (predicateParameter.Type.IsAssignableFrom(whereParameter.Type))
                {
                    predicateBody = ExpressionParameterReplacer.ReplaceParameter(predicateBody, predicateParameter, whereParameter);
                    parameter = whereParameter;
                }
                else if (whereParameter.Type.IsAssignableFrom(predicateParameter.Type))
                {
                    whereBody = ExpressionParameterReplacer.ReplaceParameter(whereBody, whereParameter, predicateParameter);
                    parameter = predicateParameter;
                }
                else
                {
                    throw new NotSupportedException("Can't combine existing where clause with new predicate because parameter types are incompatible.");
                }

                var combinedBody = Expression.AndAlso(whereBody, predicateBody);
                _where = Expression.Lambda(combinedBody, parameter);
            }
        }

        private void EnsurePreviousExpressionIsSkipOrTake()
        {
            var lastExpressionAsMethodCall = _lastExpression as MethodCallExpression;
            if (lastExpressionAsMethodCall == null || (lastExpressionAsMethodCall.Method.Name != "Skip" && lastExpressionAsMethodCall.Method.Name != "Take"))
            {
                throw new MongoQueryException("Skip and Take may only be used in conjunction with each other and cannot be separated by other operations.");
            }
        }

        private object ExecuteDistinct(IMongoQuery query)
        {
            if (_orderBy != null)
            {
                throw new NotSupportedException("Distinct cannot be used with OrderBy.");
            }
            if (_skip != null || _take != null)
            {
                throw new NotSupportedException("Distinct cannot be used with Skip or Take.");
            }

            var keyExpression = _distinct.Body;
            BsonSerializationInfo serializationInfo;
            try
            {
                serializationInfo = _serializationInfoHelper.GetSerializationInfo(keyExpression);
            }
            catch
            {
                throw new NotSupportedException("Distinct is only supported for a single field. Projections used with Distinct must resolve to a single field in the document.");
            }

            var dottedElementName = serializationInfo.ElementName;
            var source = Collection.Distinct(dottedElementName, query);

            var deserializationProjectorGenericDefinition = typeof(DeserializationProjector<>);
            var deserializationProjectorType = deserializationProjectorGenericDefinition.MakeGenericType(keyExpression.Type);
            return Activator.CreateInstance(deserializationProjectorType, source, serializationInfo);
        }

        private void SetElementSelector(MethodCallExpression methodCallExpression, Func<IEnumerable, object> elementSelector)
        {
            if (_elementSelector != null)
            {
                var message = string.Format("{0} cannot be combined with any other element selector.", methodCallExpression.Method.Name);
                throw new NotSupportedException(message);
            }
            _elementSelector = elementSelector;
        }

        private Expression StripQuote(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Quote)
            {
                return ((UnaryExpression)expression).Operand;
            }
            return expression;
        }

        private int ToInt32(Expression expression)
        {
            if (expression.Type != typeof(int))
            {
                throw new ArgumentOutOfRangeException("expression", "Expected an Expression of Type Int32.");
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression == null)
            {
                throw new ArgumentOutOfRangeException("expression", "Expected a ConstantExpression.");
            }

            return (int)constantExpression.Value;
        }

        private void TranslateAny(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // ignore any projection since we only are interested in the count
            _projection = null;

            // note: recall that cursor method Size respects Skip and Limit while Count does not
            SetElementSelector(methodCallExpression, source => ((int)((IProjector)source).Cursor.Size()) > 0);
        }

        private void TranslateCount(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // ignore any projection since we only are interested in the count
            _projection = null;

            // note: recall that cursor method Size respects Skip and Limit while Count does not
            switch (methodCallExpression.Method.Name)
            {
                case "Count":
                    SetElementSelector(methodCallExpression, source => (int)((IProjector)source).Cursor.Size());
                    break;
                case "LongCount":
                    SetElementSelector(methodCallExpression, source => ((IProjector)source).Cursor.Size());
                    break;
            }
        }

        private void TranslateDistinct(MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length != 1)
            {
                var message = "The version of the Distinct query operator with an equality comparer is not supported.";
                throw new NotSupportedException(message);
            }

            if (_projection == null)
            {
                var message = "Distinct must be used with Select to identify the field whose distinct values are to be found.";
                throw new NotSupportedException(message);
            }

            _distinct = _projection;
            _projection = null;
        }

        private void TranslateElementAt(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            // ElementAt can be implemented more efficiently in terms of Skip, Limit and First
            var index = ToInt32(methodCallExpression.Arguments[1]);
            _skip = index;
            _take = 1;

            switch (methodCallExpression.Method.Name)
            {
                case "ElementAt":
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                    break;
                case "ElementAtOrDefault":
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                    break;
            }
        }

        private void TranslateFirstOrSingle(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            switch (methodCallExpression.Method.Name)
            {
                case "First":
                    _take = 1;
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                    break;
                case "FirstOrDefault":
                    _take = 1;
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                    break;
                case "Single":
                    _take = 2;
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().Single());
                    break;
                case "SingleOrDefault":
                    _take = 2;
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().SingleOrDefault());
                    break;
            }
        }

        private void TranslateLast(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // when using OrderBy without Take Last can be much faster by reversing the sort order and using First instead of Last
            if (_orderBy != null && _take == null)
            {
                for (int i = 0; i < _orderBy.Count; i++)
                {
                    var clause = _orderBy[i];
                    var oppositeDirection = (clause.Direction == OrderByDirection.Descending) ? OrderByDirection.Ascending : OrderByDirection.Descending;
                    _orderBy[i] = new OrderByClause(clause.Key, oppositeDirection);
                }
                _take = 1;

                switch (methodCallExpression.Method.Name)
                {
                    case "Last":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                        break;
                    case "LastOrDefault":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                        break;
                }
            }
            else
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Last":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().Last());
                        break;
                    case "LastOrDefault":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().LastOrDefault());
                        break;
                }
            }
        }

        private void TranslateMaxMin(MethodCallExpression methodCallExpression)
        {
            var methodName = methodCallExpression.Method.Name;

            if (_orderBy != null)
            {
                var message = string.Format("{0} cannot be used with OrderBy.", methodName);
                throw new NotSupportedException(message);
            }
            if (_skip != null || _take != null)
            {
                var message = string.Format("{0} cannot be used with Skip or Take.", methodName);
                throw new NotSupportedException(message);
            }

            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    if (_projection != null)
                    {
                        var message = string.Format("{0} must be used with either Select or a selector argument, but not both.", methodName);
                        throw new NotSupportedException(message);
                    }
                    _projection = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            if (_projection == null)
            {
                var message = string.Format("{0} must be used with either Select or a selector argument.", methodName);
                throw new NotSupportedException(message);
            }

            // implement Max/Min by sorting on the relevant field(s) and taking the first result
            _orderBy = new List<OrderByClause>();
            if (_projection.Body.NodeType == ExpressionType.New)
            {
                // take the individual constructor arguments and make new lambdas out of them for the OrderByClauses
                var newExpression = (NewExpression)_projection.Body;
                foreach (var keyExpression in newExpression.Arguments)
                {
                    var delegateTypeGenericDefinition = typeof(Func<,>);
                    var delegateType = delegateTypeGenericDefinition.MakeGenericType(_projection.Parameters[0].Type, keyExpression.Type);
                    var keyLambda = Expression.Lambda(delegateType, keyExpression, _projection.Parameters);
                    var clause = new OrderByClause(keyLambda, (methodName == "Min") ? OrderByDirection.Ascending : OrderByDirection.Descending);
                    _orderBy.Add(clause);
                }
            }
            else
            {
                var clause = new OrderByClause(_projection, (methodName == "Min") ? OrderByDirection.Ascending : OrderByDirection.Descending);
                _orderBy.Add(clause);
            }

            _take = 1;
            SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
        }

        private void TranslateMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count == 0)
            {
                var message = string.Format("Method call expression has no arguments: {0}.", ExpressionFormatter.ToString(methodCallExpression));
                throw new ArgumentOutOfRangeException("methodCallExpression", message);
            }

            var source = methodCallExpression.Arguments[0];
            Translate(source);
            _lastExpression = source;

            if (_distinct != null)
            {
                var message = "No further operators may follow Distinct in a LINQ query.";
                throw new NotSupportedException(message);
            }

            var methodName = methodCallExpression.Method.Name;
            switch (methodName)
            {
                case "Any":
                    TranslateAny(methodCallExpression);
                    break;
                case "Count":
                case "LongCount":
                    TranslateCount(methodCallExpression);
                    break;
                case "Distinct":
                    TranslateDistinct(methodCallExpression);
                    break;
                case "ElementAt":
                case "ElementAtOrDefault":
                    TranslateElementAt(methodCallExpression);
                    break;
                case "First":
                case "FirstOrDefault":
                case "Single":
                case "SingleOrDefault":
                    TranslateFirstOrSingle(methodCallExpression);
                    break;
                case "Last":
                case "LastOrDefault":
                    TranslateLast(methodCallExpression);
                    break;
                case "Max":
                case "Min":
                    TranslateMaxMin(methodCallExpression);
                    break;
                case "OfType":
                    TranslateOfType(methodCallExpression);
                    break;
                case "OrderBy":
                case "OrderByDescending":
                    TranslateOrderBy(methodCallExpression);
                    break;
                case "Select":
                    TranslateSelect(methodCallExpression);
                    break;
                case "Skip":
                    TranslateSkip(methodCallExpression);
                    break;
                case "Take":
                    TranslateTake(methodCallExpression);
                    break;
                case "ThenBy":
                case "ThenByDescending":
                    TranslateThenBy(methodCallExpression);
                    break;
                case "Where":
                    TranslateWhere(methodCallExpression);
                    break;
                default:
                    var message = string.Format("The {0} query operator is not supported.", methodName);
                    throw new NotSupportedException(message);
            }
        }

        private void TranslateOfType(MethodCallExpression methodCallExpression)
        {
            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(Queryable))
            {
                var message = string.Format("OfType method of class {0} is not supported.", BsonUtils.GetFriendlyTypeName(method.DeclaringType));
                throw new NotSupportedException(message);
            }
            if (!method.IsStatic)
            {
                throw new NotSupportedException("Expected OfType to be a static method.");
            }
            if (!method.IsGenericMethod)
            {
                throw new NotSupportedException("Expected OfType to be a generic method.");
            }
            var actualType = method.GetGenericArguments()[0];

            var args = methodCallExpression.Arguments.ToArray();
            if (args.Length != 1)
            {
                throw new NotSupportedException("Expected OfType method to have a single argument.");
            }
            var sourceExpression = args[0];
            if (!sourceExpression.Type.IsGenericType)
            {
                throw new NotSupportedException("Expected source argument to OfType to be a generic type.");
            }
            var nominalType = sourceExpression.Type.GetGenericArguments()[0];

            if (_projection != null)
            {
                throw new NotSupportedException("OfType after a projection is not supported.");
            }

            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return; // nothing to do
            }

            if (discriminator.IsBsonArray)
            {
                discriminator = discriminator.AsBsonArray[discriminator.AsBsonArray.Count - 1];
            }
            var query = Query.EQ(discriminatorConvention.ElementName, discriminator);

            var injectMethodInfo = typeof(LinqToMongo).GetMethod("Inject");
            var body = Expression.Call(injectMethodInfo, Expression.Constant(query));
            var parameter = Expression.Parameter(nominalType, "x");
            var predicate = Expression.Lambda(body, parameter);
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            _ofType = actualType;
        }

        private void TranslateOrderBy(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_orderBy != null)
            {
                throw new NotSupportedException("Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).");
            }

            var key = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            var direction = (methodCallExpression.Method.Name == "OrderByDescending") ? OrderByDirection.Descending : OrderByDirection.Ascending;
            var clause = new OrderByClause(key, direction);

            _orderBy = new List<OrderByClause>();
            _orderBy.Add(clause);
        }

        private void TranslateSelect(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            var lambdaExpression = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            if (lambdaExpression.Parameters.Count == 2)
            {
                var message = "The indexed version of the Select query operator is not supported.";
                throw new NotSupportedException(message);
            }
            if (lambdaExpression.Parameters.Count != 1)
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            // ignore trivial projections of the form: d => d
            if (lambdaExpression.Body == lambdaExpression.Parameters[0])
            {
                return;
            }
            _projection = lambdaExpression;
        }

        private void TranslateSkip(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_skip.HasValue || _take.HasValue)
            {
                EnsurePreviousExpressionIsSkipOrTake();
            }

            var value = ToInt32(StripQuote(methodCallExpression.Arguments[1]));

            if (_take.HasValue)
            {
                if (value > _take.Value)
                {
                    _skip = null;
                    _take = 0;
                    return;
                }

                _take = Math.Max(0, _take.Value - value);
            }

            if (_skip.HasValue)
            {
                value += _skip.Value;
            }

            _skip = value;
        }

        private void TranslateTake(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_skip.HasValue || _take.HasValue)
            {
                EnsurePreviousExpressionIsSkipOrTake();
            }

            var value = ToInt32(StripQuote(methodCallExpression.Arguments[1]));

            if (_take.HasValue && value > _take.Value)
            {
                value = _take.Value;
            }

            _take = value;
        }

        private void TranslateThenBy(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_orderBy == null)
            {
                throw new NotSupportedException("ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.");
            }

            var key = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            var direction = (methodCallExpression.Method.Name == "ThenByDescending") ? OrderByDirection.Descending : OrderByDirection.Ascending;
            var clause = new OrderByClause(key, direction);

            _orderBy.Add(clause);
        }

        private void TranslateWhere(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            var predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            if (predicate.Parameters.Count == 2)
            {
                var message = "The indexed version of the Where query operator is not supported.";
                throw new NotSupportedException(message);
            }

            CombinePredicateWithWhereClause(methodCallExpression, predicate);
        }
    }
}
