using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class QueryBinder : MongoExpressionVisitor
    {
        private int _aggregateCount;
        private Expression _currentGroupElement;
        private Dictionary<Expression, GroupByInfo> _groupByMap;
        private Dictionary<ParameterExpression, Expression> _map;
        private readonly FieldProjector _projector;
        private IQueryProvider _provider;
        private readonly Expression _root;
        private List<OrderExpression> _thenBy;
        private bool _inField;

        public QueryBinder(IQueryProvider provider, Expression root)
        {
            _projector = new FieldProjector(CanBeField);
            _provider = provider;
            _root = root;
        }

        public Expression Bind(Expression expression)
        {
            _inField = false;
            _map = new Dictionary<ParameterExpression, Expression>();
            _groupByMap = new Dictionary<Expression, GroupByInfo>();
            return Visit(expression);
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            ExpressionType nodeType = b.NodeType;
            bool shouldFlip = false;
            switch (nodeType)
            {
                case ExpressionType.LessThan:
                    nodeType = ExpressionType.GreaterThanOrEqual;
                    shouldFlip = true;
                    break;
                case ExpressionType.LessThanOrEqual:
                    nodeType = ExpressionType.GreaterThan;
                    shouldFlip = true;
                    break;
                case ExpressionType.GreaterThan:
                    nodeType = ExpressionType.LessThanOrEqual;
                    shouldFlip = true;
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    nodeType = ExpressionType.LessThan;
                    shouldFlip = true;
                    break;
                case ExpressionType.NotEqual:
                    shouldFlip = true;
                    break;
                case ExpressionType.Equal:
                    shouldFlip = true;
                    break;
            }

            //reverse the conditionals if the left one is a constant to make things easier in the formatter...
            if (shouldFlip && b.Left.NodeType == ExpressionType.Constant)
                b = Expression.MakeBinary(nodeType, b.Right, b.Left, b.IsLiftedToNull, b.Method, b.Conversion);

            return base.VisitBinary(b);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (IsCollection(c.Value))
                return GetCollectionProjection(c.Value);
            return base.VisitConstant(c);
        }

        protected override Expression VisitField(FieldExpression f)
        {
            _inField = true;
            var e = base.VisitField(f);
            _inField = false;
            return e;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            var source = Visit(m.Expression);
            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    var init = (MemberInitExpression)source;
                    for (int i = 0, n = init.Bindings.Count; i < n; i++)
                    {
                        var ma = init.Bindings[i] as MemberAssignment;
                        if (ma != null && MembersMatch(ma.Member, m.Member))
                            return ma.Expression;
                    }
                    break;
                case ExpressionType.New:
                    var nex = (NewExpression)source;
                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], m.Member))
                                return nex.Arguments[i];
                        }
                    }
                    break;
            }

            if (source == m.Expression)
                return m;

            return Expression.MakeMemberAccess(source, m.Member);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
            {
                //if we are running off a field expression, things get handled in the QueryFormatter
                if (!IsOperationOnAField(m))
                {
                    switch (m.Method.Name)
                    {
                        case "Any":
                            if (m.Arguments.Count == 1)
                                return BindAny(m.Arguments[0], null, m == _root);
                            else
                                return BindAny(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), m == _root);
                        case "Where":
                            return BindWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                        case "Select":
                            return BindSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                        case "OrderBy":
                            return BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                        case "OrderByDescending":
                            return BindOrderBy(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
                        case "ThenBy":
                            return BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Ascending);
                        case "ThenByDescending":
                            return BindThenBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), OrderType.Descending);
                        case "Take":
                            if (m.Arguments.Count == 2)
                                return this.BindTake(m.Arguments[0], m.Arguments[1]);
                            break;
                        case "Skip":
                            if (m.Arguments.Count == 2)
                                return this.BindSkip(m.Arguments[0], m.Arguments[1]);
                            break;
                        case "First":
                        case "FirstOrDefault":
                        case "Single":
                        case "SingleOrDefault":
                            if (m.Arguments.Count == 1)
                                return BindFirstOrSingle(m.Arguments[0], null, m.Method.Name, m == _root);
                            if (m.Arguments.Count == 2)
                            {
                                var predicate = (LambdaExpression)StripQuotes(m.Arguments[1]);
                                return BindFirstOrSingle(m.Arguments[0], predicate, m.Method.Name, m == _root);
                            }
                            break;
                        case "Count":
                        case "Sum":
                        case "Average":
                        case "Min":
                        case "Max":
                            switch(m.Arguments.Count)
                            {
                                case 1:
                                    return BindAggregate(m.Arguments[0], m.Method, null, m == _root);
                                case 2:
                                {
                                    var argument = (LambdaExpression)StripQuotes(m.Arguments[1]);
                                    return BindAggregate(m.Arguments[0], m.Method, argument, m == _root);
                                }
                            }
                            break;
                        case "GroupBy":
                            if (m.Arguments.Count == 2)
                                return BindGroupBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), null, null);
                            if (m.Arguments.Count == 3)
                                return BindGroupBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), (LambdaExpression)StripQuotes(m.Arguments[2]), null);
                            if (m.Arguments.Count == 4)
                                return BindGroupBy(m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]), (LambdaExpression)StripQuotes(m.Arguments[2]), (LambdaExpression)StripQuotes(m.Arguments[3]));
                            break;
                    }
                    throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
                }
            }
            return base.VisitMethodCall(m);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            Expression e;
            return _map.TryGetValue(p, out e) ? e : p;
        }

        private Expression BindAggregate(Expression source, MethodInfo method, LambdaExpression argument, bool isRoot)
        {
            var returnType = method.ReturnType;
            var aggregateType = GetAggregateType(method.Name);
            bool hasPredicateArgument = HasPredicateArgument(aggregateType);
            bool distinct = false;
            bool argumentWasPredicate = false;

            var methodCallExpression = source as MethodCallExpression;
            if (methodCallExpression != null && !hasPredicateArgument && argument == null)
            {
                if (methodCallExpression.Method.Name == "Distinct" && methodCallExpression.Arguments.Count == 1
                    && (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(Enumerable)))
                {
                    source = methodCallExpression.Arguments[0];
                    distinct = true;
                }
            }

            if (argument != null && hasPredicateArgument)
            {
                source = Expression.Call(typeof(Queryable), "Where", method.GetGenericArguments(), source, argument);
                argument = null;
                argumentWasPredicate = true;
            }

            var projection = VisitSequence(source);
            Expression argExpression = null;
            if (argument != null)
            {
                _map[argument.Parameters[0]] = projection.Projector;
                argExpression = Visit(argument.Body);
            }
            else if (!hasPredicateArgument)
                argExpression = projection.Projector;

            var alias = new Alias();
            Expression aggregateExpression = new AggregateExpression(returnType, aggregateType, argExpression, distinct);
            var selectType = typeof(IEnumerable<>).MakeGenericType(returnType);
            string fieldName = "_$agg" + (_aggregateCount++);
            var select = new SelectExpression(alias, new[] { new FieldDeclaration(fieldName, aggregateExpression) }, projection.Source, null);

            if (isRoot)
            {
                var parameter = Expression.Parameter(selectType, "p");
                var lambda = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new[] { returnType }, parameter), parameter);
                return new ProjectionExpression(
                    select,
                    new FieldExpression(aggregateExpression, alias, fieldName),
                    lambda);
            }

            var subquery = new ScalarExpression(returnType, select);

            GroupByInfo info;
            if (!argumentWasPredicate && _groupByMap.TryGetValue(projection, out info))
            {
                if (argument != null)
                {
                    _map[argument.Parameters[0]] = info.Element;
                    argExpression = Visit(argument.Body);
                }
                else if (!hasPredicateArgument)
                    argExpression = info.Element;

                aggregateExpression = new AggregateExpression(returnType, aggregateType, argExpression, distinct);

                if (projection == _currentGroupElement)
                    return aggregateExpression;

                return new AggregateSubqueryExpression(info.Alias, aggregateExpression, subquery);
            }

            return subquery;
        }

        private Expression BindAny(Expression source, LambdaExpression predicate, bool isRoot)
        {
            var projection = VisitSequence(source);
            var sourceType = projection.Projector.Type;

            MethodInfo method = typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Count")
                .Single(m => m.GetParameters().Length == (predicate == null ? 1 : 2))
                .GetGenericMethodDefinition().MakeGenericMethod(sourceType);

            var expression = BindAggregate(source, method, predicate, isRoot);

            return Expression.GreaterThan(
                expression, Expression.Constant(0));
        }

        private Expression BindDistinct(Expression source)
        {
            var projection = VisitSequence(source);
            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, null, null, null, true, null, null),
                fieldProjection.Projector);
        }

        private Expression BindFirstOrSingle(Expression source, LambdaExpression predicate, string kind, bool isRoot)
        {
            var projection = VisitSequence(source);
            Expression where = null;
            if (predicate != null)
            {
                _map[predicate.Parameters[0]] = projection.Projector;
                where = Visit(predicate.Body);
            }

            Expression take = kind.StartsWith("First") ? Expression.Constant(1) : null;
            if (take == null & kind.StartsWith("Single"))
                take = Expression.Constant(2);

            if (take != null || where != null)
            {
                var alias = new Alias();
                var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
                projection = new ProjectionExpression(
                    new SelectExpression(alias, fieldProjection.Fields, projection.Source, where, null, null, false, null, take),
                    fieldProjection.Projector);
            }
            if (isRoot)
            {
                var elementType = projection.Projector.Type;
                var p = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "p");
                var lambda = Expression.Lambda(Expression.Call(typeof(Enumerable), kind, new[] { elementType }, p), p);
                return new ProjectionExpression(projection.Source, projection.Projector, lambda);
            }
            return projection;
        }

        protected virtual Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector)
        {
            var projection = VisitSequence(source);

            _map[keySelector.Parameters[0]] = projection.Projector;
            var keyExpression = Visit(keySelector.Body);

            var elementExpression = projection.Projector;
            if (elementSelector != null)
            {
                _map[elementSelector.Parameters[0]] = projection.Projector;
                elementExpression = Visit(elementSelector.Body);
            }

            var subqueryBasis = VisitSequence(source);
            _map[keySelector.Parameters[0]] = subqueryBasis.Projector;
            var subqueryKeyExpression = Visit(keySelector.Body);

            var subqueryCorrelation = Expression.Equal(keyExpression, subqueryKeyExpression);

            var subqueryElementExpression = subqueryBasis.Projector;
            if (elementSelector != null)
            {
                _map[elementSelector.Parameters[0]] = subqueryBasis.Projector;
                subqueryElementExpression = Visit(elementSelector.Body);
            }

            var elementAlias = new Alias();
            var elementProjection = _projector.ProjectFields(subqueryElementExpression, elementAlias, subqueryBasis.Source.Alias);
            var elementSubquery =
                new ProjectionExpression(
                    new SelectExpression(elementAlias, elementProjection.Fields, subqueryBasis.Source, subqueryCorrelation),
                    elementProjection.Projector);

            var alias = new Alias();

            var info = new GroupByInfo(alias, elementExpression);
            _groupByMap[elementSubquery] = info;

            Expression resultExpression;
            if (resultSelector != null)
            {
                var saveGroupElement = _currentGroupElement;
                _currentGroupElement = elementSubquery;

                _map[resultSelector.Parameters[0]] = keyExpression;
                _map[resultSelector.Parameters[1]] = elementSubquery;
                resultExpression = Visit(resultSelector.Body);
                _currentGroupElement = saveGroupElement;
            }
            else
            {
                resultExpression = Expression.New(
                    typeof(Grouping<,>).MakeGenericType(keyExpression.Type, subqueryElementExpression.Type).GetConstructors()[0],
                    new[] { keyExpression, elementSubquery });
            }

            var fieldProjection = _projector.ProjectFields(resultExpression, alias, projection.Source.Alias);

            var projectedElementSubquery = ((NewExpression)fieldProjection.Projector).Arguments[1];
            _groupByMap[projectedElementSubquery] = info;

            return new ProjectionExpression(
                new SelectExpression(alias, new FieldDeclaration[0], projection.Source, null, null, keyExpression, false, null, null),
                fieldProjection.Projector);
        }

        private Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            List<OrderExpression> thenBye = _thenBy;
            _thenBy = null;
            var projection = VisitSequence(source);

            _map[orderSelector.Parameters[0]] = projection.Projector;
            var orderings = new List<OrderExpression> {new OrderExpression(orderType, Visit(orderSelector.Body))};
            if (thenBye != null)
            {
                for (int i = thenBye.Count - 1; i >= 0; i--)
                {
                    var oe = thenBye[i];
                    var lambda = (LambdaExpression)oe.Expression;
                    _map[lambda.Parameters[0]] = projection.Projector;
                    orderings.Add(new OrderExpression(oe.OrderType, Visit(lambda.Body)));
                }
            }

            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, null, orderings.AsReadOnly(), null, false, null, null),
                fieldProjection.Projector);
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            var projection = VisitSequence(source);
            _map[selector.Parameters[0]] = projection.Projector;
            var expression = Visit(selector.Body);
            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(expression, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, null),
                fieldProjection.Projector);
        }

        private Expression BindSkip(Expression source, Expression skip)
        {
            var projection = VisitSequence(source);
            skip = Visit(skip);
            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, null, null, null, false, skip, null),
                fieldProjection.Projector);
        }

        private Expression BindTake(Expression source, Expression take)
        {
            var projection = VisitSequence(source);
            take = Visit(take);
            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, null, null, null, false, null, take),
                fieldProjection.Projector);
        }

        private Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            if (_thenBy == null)
                _thenBy = new List<OrderExpression>();

            _thenBy.Add(new OrderExpression(orderType, orderSelector));
            return Visit(source);
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            var projection = VisitSequence(source);
            _map[predicate.Parameters[0]] = projection.Projector;
            var where = Visit(predicate.Body);
            var alias = new Alias();
            var fieldProjection = _projector.ProjectFields(projection.Projector, alias, projection.Source.Alias);
            return new ProjectionExpression(
                new SelectExpression(alias, fieldProjection.Fields, projection.Source, where),
                fieldProjection.Projector);
        }

        private ProjectionExpression GetCollectionProjection(object value)
        {
            var collectionAlias = new Alias();
            var selectAlias = new Alias();
            var collection = (IMongoQueryable)value;
            var fields = new List<FieldDeclaration>();
            return new ProjectionExpression(
                new SelectExpression(selectAlias, fields, new CollectionExpression(collectionAlias, collection.Database, collection.CollectionName, collection.ElementType), null),
                Expression.Parameter(collection.ElementType, "document"));
        }

        private Expression BuildPredicateEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
        {
            var en1 = source1.GetEnumerator();
            var en2 = source2.GetEnumerator();
            Expression result = null;
            while (en1.MoveNext() && en2.MoveNext())
            {
                Expression compare = Expression.Equal(en1.Current, en2.Current);
                result = (result == null) ? compare : Expression.And(result, compare);
            }
            return result;
        }

        private ProjectionExpression ConvertToSequence(Expression expression)
        {
            switch (expression.NodeType)
            {
                case (ExpressionType)MongoExpressionType.Projection:
                    return (ProjectionExpression)expression;
                case ExpressionType.New:
                    var newExpression = (NewExpression)expression;
                    if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                        return (ProjectionExpression)newExpression.Arguments[1];
                    break;
            }

            throw new NotSupportedException(string.Format("The expression of type '{0}' is not a sequence", expression.Type));
        }

        private bool IsOperationOnAField(MethodCallExpression m)
        {
            return _inField
                || m.Arguments[0].NodeType == (ExpressionType)MongoExpressionType.Field
                || (m.Arguments.Count == 2 && m.Arguments[1].NodeType == (ExpressionType)MongoExpressionType.Field);
        }

        private ProjectionExpression VisitSequence(Expression source)
        {
            return ConvertToSequence(Visit(source));
        }

        internal static bool CanBeField(Expression expression)
        {
            switch (expression.NodeType)
            {
                case (ExpressionType)MongoExpressionType.Aggregate:
                case (ExpressionType)MongoExpressionType.AggregateSubquery:
                case (ExpressionType)MongoExpressionType.Field:
                case (ExpressionType)MongoExpressionType.Scalar:
                    return true;
                default:
                    return false;
            }
        }

        private static AggregateType GetAggregateType(string methodName)
        {
            switch (methodName)
            {
                case "Count":
                    return AggregateType.Count;
                case "Sum":
                    return AggregateType.Sum;
                case "Average":
                    return AggregateType.Average;
                case "Min":
                    return AggregateType.Min;
                case "Max":
                    return AggregateType.Max;
            }

            throw new NotSupportedException(string.Format("Aggregate of type '{0}' is not supported.", methodName));
        }

        private static bool HasPredicateArgument(AggregateType aggregateType)
        {
            return aggregateType == AggregateType.Count;
        }

        private static bool IsCollection(object value)
        {
            var q = value as IMongoQueryable;
            return q != null && q.Expression.NodeType == ExpressionType.Constant;
        }

        private static bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a == b)
                return true;
            if (a is MethodInfo && b is PropertyInfo)
                return a == ((PropertyInfo)b).GetGetMethod();
            if (a is PropertyInfo && b is MethodInfo)
                return ((PropertyInfo)a).GetGetMethod() == b;
            return false;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }

        private class GroupByInfo
        {
            public Alias Alias { get; private set; }
            public Expression Element { get; private set; }

            public GroupByInfo(Alias alias, Expression element)
            {
                Alias = alias;
                Element = element;
            }
        }
    }
}
