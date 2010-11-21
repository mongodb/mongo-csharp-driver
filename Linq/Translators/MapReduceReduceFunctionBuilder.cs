using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MongoDB.Linq.Expressions;
using System.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class MapReduceReduceFunctionBuilder : MongoExpressionVisitor
    {
        private StringBuilder _declare;
        private StringBuilder _loop;
        private StringBuilder _return;
        private List<KeyValuePair<string, string>> _returnValues;
        private string _currentAggregateName;

        public string Build(ReadOnlyCollection<FieldDeclaration> fields)
        {
            _declare = new StringBuilder();
            _loop = new StringBuilder();
            _return = new StringBuilder();
            _returnValues = new List<KeyValuePair<string, string>>();
            _declare.Append("function(key, values) {");
            _loop.Append("values.forEach(function(doc) {");
            _return.Append("return { ");

            VisitFieldDeclarationList(fields);

            for (int i = 0; i < _returnValues.Count; i++)
            {
                if (i > 0)
                    _return.Append(", ");
                _return.AppendFormat("\"{0}\": {1}", _returnValues[i].Key, _returnValues[i].Value);
            }

            _loop.Append("});");
            _return.Append("};}");

            return _declare.ToString() + _loop + _return;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            switch (aggregate.AggregateType)
            {
                case AggregateType.Average:
                    AverageAggregate(aggregate);
                    break;
                case AggregateType.Count:
                    CountAggregate(aggregate);
                    break;
                case AggregateType.Max:
                    MaxAggregate(aggregate);
                    break;
                case AggregateType.Min:
                    MinAggregate(aggregate);
                    break;
                case AggregateType.Sum:
                    SumAggregate(aggregate);
                    break;
            }

            return aggregate;
        }

        protected override ReadOnlyCollection<FieldDeclaration> VisitFieldDeclarationList(ReadOnlyCollection<FieldDeclaration> fields)
        {
            for (int i = 0, n = fields.Count; i < n; i++)
            {
                _currentAggregateName = fields[i].Name;
                if (_currentAggregateName == "*")
                    continue;
                Visit(fields[i].Expression);
            }

            return fields;
        }

        private void AverageAggregate(AggregateExpression aggregate)
        {
            var old = _currentAggregateName;
            _currentAggregateName = old + "Cnt";
            CountAggregate(aggregate);
            _currentAggregateName = old + "Sum";
            SumAggregate(aggregate);
            _currentAggregateName = old;
        }

        private void CountAggregate(AggregateExpression aggregate)
        {
            _declare.AppendFormat("var {0} = 0;", _currentAggregateName);
            _loop.AppendFormat("{0} += doc.{0};", _currentAggregateName);
            _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, _currentAggregateName));
        }

        private void MaxAggregate(AggregateExpression aggregate)
        {
            _declare.AppendFormat("var {0} = Number.MIN_VALUE;", _currentAggregateName);
            _loop.AppendFormat("if(doc.{0} > {0}) {0} = doc.{0};", _currentAggregateName);
            _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, _currentAggregateName));
        }

        private void MinAggregate(AggregateExpression aggregate)
        {
            _declare.AppendFormat("var {0} = Number.MAX_VALUE;", _currentAggregateName);
            _loop.AppendFormat("if(doc.{0} < {0}) {0} = doc.{0};", _currentAggregateName);
            _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, _currentAggregateName));
        }

        private void SumAggregate(AggregateExpression aggregate)
        {
            _declare.AppendFormat("var {0} = 0;", _currentAggregateName);
            _loop.AppendFormat("{0} += doc.{0};", _currentAggregateName);
            _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, _currentAggregateName));
        }

    }
}