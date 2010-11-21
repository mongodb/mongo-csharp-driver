using System;
using MongoDB.Driver;
using MongoDB.Linq.Expressions;
using System.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class MongoQueryObjectBuilder : MongoExpressionVisitor
    {
        private MongoQueryObject _queryObject;
        private QueryAttributes _queryAttributes;

        internal MongoQueryObject Build(Expression expression)
        {
            _queryObject = new MongoQueryObject();
            _queryAttributes = new QueryAttributesGatherer().Gather(expression);
            _queryObject.IsCount = _queryAttributes.IsCount;
            _queryObject.IsMapReduce = _queryAttributes.IsMapReduce;
            Visit(expression);
            return _queryObject;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = PreProcessSelect(select);

            if (select.From != null)
                VisitSource(select.From);
            if (select.Where != null)
            {
                try
                {
                    //try this first, and if it fails, resort to javascript generation, which is slower on the server side.
                    _queryObject.SetQueryDocument(new DocumentFormatter().FormatDocument(select.Where));
                }
                catch
                {
                    _queryObject.SetWhereClause(new JavascriptFormatter().FormatJavascript(select.Where));
                }
            }

            if (_queryAttributes.IsMapReduce)
            {
                _queryObject.IsMapReduce = true;
                _queryObject.MapFunction = new MapReduceMapFunctionBuilder().Build(select.Fields, select.GroupBy);
                _queryObject.ReduceFunction = new MapReduceReduceFunctionBuilder().Build(select.Fields);
                _queryObject.FinalizerFunction = new MapReduceFinalizerFunctionBuilder().Build(select.Fields);
            }
            else if(!_queryAttributes.IsCount && !select.Fields.HasSelectAllField())
            {
                var fieldGatherer = new FieldGatherer();
                foreach (var field in select.Fields)
                {
                    var expandedFields = fieldGatherer.Gather(field.Expression);
                    foreach (var expandedField in expandedFields)
                        _queryObject.Fields[expandedField.Name] = 1;
                }
            }

            if (select.OrderBy != null)
            {
                foreach (var order in select.OrderBy)
                {
                    var field = Visit(order.Expression) as FieldExpression;
                    if (field == null)
                        throw new MongoQueryException("Complex order by clauses are not supported.");
                    _queryObject.AddSort(field.Name, order.OrderType == OrderType.Ascending ? 1 : -1);
                }
            }

            if (select.Take != null)
                _queryObject.NumberToLimit = EvaluateConstant<int>(select.Take);

            if (select.Skip != null)
                _queryObject.NumberToSkip = EvaluateConstant<int>(select.Skip);

            return select;
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            Visit(projection.Source);
            return projection;
        }

        protected override Expression VisitSource(Expression source)
        {
            switch ((MongoExpressionType)source.NodeType)
            {
                case MongoExpressionType.Collection:
                    var collection = (CollectionExpression)source;
                    _queryObject.CollectionName = collection.CollectionName;
                    _queryObject.Database = collection.Database;
                    _queryObject.DocumentType = collection.DocumentType;
                    break;
                case MongoExpressionType.Select:
                    Visit(source);
                    break;
                default:
                    throw new InvalidOperationException("Select source is not valid type");
            }
            return source;
        }

        private SelectExpression PreProcessSelect(SelectExpression select)
        {
            if (select.Where != null && select.Where.NodeType == ExpressionType.Constant && select.Where.Type == typeof(bool))
            {
                var value = EvaluateConstant<bool>(select.Where);
                if (value)
                    select = select.SetWhere(null);
                else
                    throw new MongoQueryException("If you don't want to return any values, don't call the method.");
            }

            return select;
        }

        private static T EvaluateConstant<T>(Expression e)
        {
            if (e.NodeType != ExpressionType.Constant)
                throw new ArgumentException("Expression must be a constant.");

            return (T)((ConstantExpression)e).Value;
        }

        private class QueryAttributes
        {
            public bool IsCount { get; private set; }
            public bool IsMapReduce { get; private set; }

            public QueryAttributes(bool isCount, bool isMapReduce)
            {
                IsCount = isCount;
                IsMapReduce = isMapReduce;
            }
        }

        private class QueryAttributesGatherer : MongoExpressionVisitor
        {
            private bool _isCount { get; set; }
            private bool _isMapReduce { get; set; }

            public QueryAttributes Gather(Expression expression)
            {
                _isCount = false;
                _isMapReduce = false;
                Visit(expression);
                return new QueryAttributes(_isCount, _isMapReduce);
            }

            protected override Expression VisitSelect(SelectExpression select)
            {
                if (select.From.NodeType != (ExpressionType)MongoExpressionType.Collection)
                    throw new MongoQueryException("The query is too complex to be processed by MongoDB. Try building a map-reduce query by hand or simplifying the query and using Linq-to-Objects.");

                bool hasAggregates = new AggregateChecker().HasAggregates(select);

                if (select.GroupBy != null)
                    _isMapReduce = true;
                
                else if (hasAggregates)
                {
                    if (select.Fields.Count == 1 && select.Fields[0].Expression.NodeType == (ExpressionType)MongoExpressionType.Aggregate)
                    {
                        var aggregateExpression = (AggregateExpression)select.Fields[0].Expression;
                        if (aggregateExpression.AggregateType == AggregateType.Count)
                            _isCount = true;
                    }

                    if (!_isCount)
                        _isMapReduce = true;
                }

                Visit(select.Where);
                return select;
            }
        }
    }
}