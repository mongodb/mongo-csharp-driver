using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MongoDB.Linq.Expressions;
using System.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class MapReduceFinalizerFunctionBuilder : MongoExpressionVisitor
    {
        private StringBuilder _return;
        private List<KeyValuePair<string, string>> _returnValues;
        private string _currentAggregateName;

        public string Build(ReadOnlyCollection<FieldDeclaration> fields)
        {
            _return = new StringBuilder();
            _returnValues = new List<KeyValuePair<string, string>>();
            _return.Append("function(key, value) { return { ");

            VisitFieldDeclarationList(fields);

            for (int i = 0; i < _returnValues.Count; i++)
            {
                if (i > 0)
                    _return.Append(", ");
                _return.AppendFormat("\"{0}\": {1}", _returnValues[i].Key, _returnValues[i].Value);
            }

            _return.Append("};}");

            return _return.ToString();
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            switch (aggregate.AggregateType)
            {
                case AggregateType.Average:
                    _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, string.Format("value.{0}Sum/value.{0}Cnt", _currentAggregateName)));
                    break;
                case AggregateType.Count:
                case AggregateType.Max:
                case AggregateType.Min:
                case AggregateType.Sum:
                    _returnValues.Add(new KeyValuePair<string, string>(_currentAggregateName, "value." + _currentAggregateName));
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
    }
}