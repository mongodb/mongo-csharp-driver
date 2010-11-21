using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Linq.Translators;
using MongoDB.Linq.Util;

namespace MongoDB.Linq.Expressions
{
    internal class MongoExpressionComparer : ExpressionComparer
    {
        private ScopedDictionary<Alias, Alias> _aliasScope;

        protected MongoExpressionComparer(ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope, ScopedDictionary<Alias, Alias> aliasScope)
            : base(parameterScope)
        {
            _aliasScope = aliasScope;
        }

        public new static bool AreEqual(Expression a, Expression b)
        {
            return AreEqual(null, null, a, b);
        }

        public static bool AreEqual(ScopedDictionary<ParameterExpression, ParameterExpression> parameterScope, ScopedDictionary<Alias, Alias> aliasScope, Expression a, Expression b)
        {
            return new MongoExpressionComparer(parameterScope, aliasScope).Compare(a, b);
        }

        protected override bool Compare(Expression a, Expression b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.NodeType != b.NodeType)
                return false;
            if (a.Type != b.Type)
                return false;
            switch ((MongoExpressionType)a.NodeType)
            {
                case MongoExpressionType.Collection:
                    return CompareCollection((CollectionExpression)a, (CollectionExpression)b);
                case MongoExpressionType.Field:
                    return CompareField((FieldExpression)a, (FieldExpression)b);
                case MongoExpressionType.Select:
                    return CompareSelect((SelectExpression)a, (SelectExpression)b);
                case MongoExpressionType.Aggregate:
                    return CompareAggregate((AggregateExpression)a, (AggregateExpression)b);
                case MongoExpressionType.Scalar:
                    return CompareSubquery((SubqueryExpression)a, (SubqueryExpression)b);
                case MongoExpressionType.AggregateSubquery:
                    return CompareAggregateSubquery((AggregateSubqueryExpression)a, (AggregateSubqueryExpression)b);
                case MongoExpressionType.Projection:
                    return CompareProjection((ProjectionExpression)a, (ProjectionExpression)b);
                default:
                    return base.Compare(a, b);
            }
        }

        protected virtual bool CompareCollection(CollectionExpression a, CollectionExpression b)
        {
            return a.CollectionName == b.CollectionName;
        }

        protected virtual bool CompareField(FieldExpression a, FieldExpression b)
        {
            return CompareAlias(a.Alias, b.Alias) && a.Name == b.Name && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareAlias(Alias a, Alias b)        
        {
            if (_aliasScope != null)
            {
                Alias mapped;
                if (_aliasScope.TryGetValue(a, out mapped))
                    return mapped == b;
            }
            return a == b;
        }

        protected virtual bool CompareSelect(SelectExpression a, SelectExpression b)
        {
            var save = _aliasScope;
            try
            {
                if (!Compare(a.From, b.From))
                    return false;

                _aliasScope = new ScopedDictionary<Alias, Alias>(save);
                MapAliases(a.From, b.From);

                return Compare(a.Where, b.Where)
                    && CompareOrderList(a.OrderBy, b.OrderBy)
                    && Compare(a.GroupBy, b.GroupBy)
                    && Compare(a.Skip, b.Skip)
                    && Compare(a.Take, b.Take)
                    && a.IsDistinct == b.IsDistinct
                    && CompareFieldDeclarations(a.Fields, b.Fields);
            }
            finally
            {
                _aliasScope = save;
            }
        }

        protected virtual bool CompareOrderList(ReadOnlyCollection<OrderExpression> a, ReadOnlyCollection<OrderExpression> b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (a[i].OrderType != b[i].OrderType ||
                    !Compare(a[i].Expression, b[i].Expression))
                    return false;
            }
            return true;
        }

        protected virtual bool CompareFieldDeclarations(ReadOnlyCollection<FieldDeclaration> a, ReadOnlyCollection<FieldDeclaration> b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareFieldDeclaration(a[i], b[i]))
                    return false;
            }
            return true;
        }

        protected virtual bool CompareFieldDeclaration(FieldDeclaration a, FieldDeclaration b)
        {
            return a.Name == b.Name && Compare(a.Expression, b.Expression);
        }

        protected virtual bool CompareAggregate(AggregateExpression a, AggregateExpression b)
        {
            return a.AggregateType == b.AggregateType && Compare(a.Argument, b.Argument);
        }

        protected virtual bool CompareSubquery(SubqueryExpression a, SubqueryExpression b)
        {
            if (a.NodeType != b.NodeType)
                return false;
            switch ((MongoExpressionType)a.NodeType)
            {
                case MongoExpressionType.Scalar:
                    return CompareScalar((ScalarExpression)a, (ScalarExpression)b);
            }
            return false;
        }

        protected virtual bool CompareScalar(ScalarExpression a, ScalarExpression b)
        {
            return Compare(a.Select, b.Select);
        }

        protected virtual bool CompareAggregateSubquery(AggregateSubqueryExpression a, AggregateSubqueryExpression b)
        {
            return Compare(a.AggregateAsSubquery, b.AggregateAsSubquery)
                && Compare(a.AggregateInGroupSelect, b.AggregateInGroupSelect)
                && a.GroupByAlias == b.GroupByAlias;
        }

        protected virtual bool CompareProjection(ProjectionExpression a, ProjectionExpression b)
        {
            if (!Compare(a.Source, b.Source))
                return false;

            var save = _aliasScope;
            try
            {
                _aliasScope = new ScopedDictionary<Alias, Alias>(_aliasScope);
                _aliasScope.Add(a.Source.Alias, b.Source.Alias);

                return Compare(a.Projector, b.Projector)
                    && Compare(a.Aggregator, b.Aggregator)
                    && a.IsSingleton == b.IsSingleton;
            }
            finally
            {
                _aliasScope = save;
            }
        }

        private void MapAliases(Expression a, Expression b)
        {
            var gatherer = new DeclaredAliasGatherer();
            Alias[] prodA = gatherer.Gather(a).ToArray();
            Alias[] prodB = gatherer.Gather(b).ToArray();
            for (int i = 0, n = prodA.Length; i < n; i++)
            {
                _aliasScope.Add(prodA[i], prodB[i]);
            }
        }
    }
}
