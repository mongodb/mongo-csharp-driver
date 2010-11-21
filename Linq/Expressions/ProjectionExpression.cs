using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class ProjectionExpression : MongoExpression
    {
        public LambdaExpression Aggregator { get; private set; }

        public bool IsSingleton
        {
            get { return Aggregator != null && Aggregator.Body.Type == Projector.Type; }
        }

        public Expression Projector { get; private set; }

        public SelectExpression Source { get; private set; }

        public ProjectionExpression(SelectExpression source, Expression projector)
            : this(source, projector, null)
        { }

        public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression aggregator)
            : base(MongoExpressionType.Projection, aggregator != null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
        {
            Source = source;
            Projector = projector;
            Aggregator = aggregator;
        }
    }
}
