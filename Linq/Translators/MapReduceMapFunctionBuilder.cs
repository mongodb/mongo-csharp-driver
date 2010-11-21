using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MongoDB.Linq.Expressions;
using System.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class MapReduceMapFunctionBuilder : MongoExpressionVisitor
    {
        private readonly JavascriptFormatter _formatter;
        private Dictionary<string, string> _initMap;
        private string _currentAggregateName;

        public MapReduceMapFunctionBuilder()
        {
            _formatter = new JavascriptFormatter();
        }

        public string Build(ReadOnlyCollection<FieldDeclaration> fields, Expression groupBy)
        {
            var sb = new StringBuilder();
            sb.Append("function() { emit(");

            sb.Append(groupBy == null ? "1" : _formatter.FormatJavascript(groupBy));

            sb.Append(", ");

            _initMap = new Dictionary<string, string>();
            VisitFieldDeclarationList(fields);
            FormatInit(sb);

            sb.Append("); }");

            return sb.ToString();
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            switch (aggregate.AggregateType)
            {
                case AggregateType.Average:
                    _initMap[_currentAggregateName + "Sum"] = _formatter.FormatJavascript(aggregate.Argument);
                    _initMap[_currentAggregateName + "Cnt"] = "1";
                    break;
                case AggregateType.Count:
                    _initMap[_currentAggregateName] = "1";
                    break;
                case AggregateType.Max:
                case AggregateType.Min:
                case AggregateType.Sum:
                    _initMap[_currentAggregateName] = _formatter.FormatJavascript(aggregate.Argument);
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

        private void FormatInit(StringBuilder sb)
        {
            sb.Append("{");
            var isFirst = true;
            foreach (var field in _initMap)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(", ");

                sb.AppendFormat("\"{0}\": {1}", field.Key, field.Value);
            }
            sb.Append("}");
        }

    }
}