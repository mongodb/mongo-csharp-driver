using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Linq.Expressions;
using MongoDB.Linq.Util;

namespace MongoDB.Linq.Translators
{
    internal class JavascriptFormatter : MongoExpressionVisitor
    {
        private StringBuilder _js;

        public string FormatJavascript(Expression expression)
        {
            _js = new StringBuilder();
            Visit(expression);
            return _js.ToString();
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            _js.Append("(");
            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    _js.Append(" === ");
                    break;
                case ExpressionType.GreaterThan:
                    _js.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _js.Append(" >= ");
                    break;
                case ExpressionType.LessThan:
                    _js.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _js.Append(" <= ");
                    break;
                case ExpressionType.NotEqual:
                    _js.Append(" != ");
                    break;
                case ExpressionType.Modulo:
                    _js.Append(" % ");
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    _js.Append(" && ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    _js.Append(" || ");
                    break;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    _js.Append(" + ");
                    break;
                case ExpressionType.Coalesce:
                    _js.Append(" || ");
                    break;
                case ExpressionType.Divide:
                    _js.Append(" / ");
                    break;
                case ExpressionType.ExclusiveOr:
                    _js.Append(" ^ ");
                    break;
                case ExpressionType.LeftShift:
                    _js.Append(" << ");
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    _js.Append(" * ");
                    break;
                case ExpressionType.RightShift:
                    _js.Append(" >> ");
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    _js.Append(" - ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The operation {0} is not supported.", b.NodeType));
            }

            Visit(b.Right);

            _js.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            _js.Append(GetJavascriptValueForConstant(c));
            return c;
        }

        protected override Expression VisitField(FieldExpression f)
        {
            //TODO: may need to handle a field that composes other fields.
             _js.AppendFormat("this.{0}", f.Name);
            return f;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Member.DeclaringType == typeof(Array))
            {
                if (m.Member.Name == "Length")
                {
                    Visit(m.Expression);
                    _js.Append(".length");
                    return m;
                }
            }
            else if (m.Member.DeclaringType == typeof(string))
            {
                if (m.Member.Name == "Length")
                {
                    Visit(m.Expression);
                    _js.Append(".length");
                    return m;
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(m.Member.DeclaringType))
            {
                if (m.Member.Name == "Count")
                {
                    Visit(m.Expression);
                    _js.Append(".length");
                    return m;
                }
            }
            else if (typeof(ICollection<>).IsOpenTypeAssignableFrom(m.Member.DeclaringType))
            {
                if (m.Member.Name == "Count")
                {
                    Visit(m.Expression);
                    _js.Append(".length");
                    return m;
                }
            }

            throw new NotSupportedException(string.Format("The member {0} is not supported.", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m == null)
                return m;

            FieldExpression field;
            if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Count":
                        if (m.Arguments.Count == 1)
                        {
                            Visit(m.Arguments[0]);
                            _js.Append(".length");
                            return m;
                        }
                        throw new NotSupportedException("The method Count with a predicate is not supported for field.");
                }
            }
            else if (m.Method.DeclaringType == typeof(string))
            {
                field = m.Object as FieldExpression;
                if (field == null)
                    throw new MongoQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));
                Visit(field);

                switch (m.Method.Name)
                {
                    case "StartsWith":
                        _js.AppendFormat("/^{0}/", EvaluateConstant<string>(m.Arguments[0]));
                        break;
                    case "EndsWith":
                        _js.AppendFormat("/{0}$/", EvaluateConstant<string>(m.Arguments[0]));
                        break;
                    case "Contains":
                        _js.AppendFormat("/{0}/", EvaluateConstant<string>(m.Arguments[0]));
                        break;
                    case "SubString":
                        switch(m.Arguments.Count)
                        {
                            case 1:
                                _js.AppendFormat(".substr({0})", EvaluateConstant<int>(m.Arguments[0]));
                                break;
                            case 2:
                                _js.AppendFormat(".substr({0})", EvaluateConstant<int>(m.Arguments[0]), EvaluateConstant<int>(m.Arguments[1]));
                                break;
                        }
                        break;
                    case "ToLower":
                        _js.Append(".toLowerCase()");
                        break;
                    case "ToUpper":
                        _js.Append(".toUpperCase()");
                        break;
                    case "get_Chars":
                        _js.AppendFormat("[{0}]", EvaluateConstant<int>(m.Arguments[0]));
                        break;
                    default:
                        throw new NotSupportedException(string.Format("The string method {0} is not supported.", m.Method.Name));
                }

                return m;
            }
            else if (m.Method.DeclaringType == typeof(Regex))
            {
                if (m.Method.Name == "IsMatch")
                {
                    field = m.Arguments[0] as FieldExpression;
                    if (field == null)
                        throw new MongoQueryException(string.Format("The mongo field must be the operator for a string operation of type {0}.", m.Method.Name));

                    Visit(field);
                    string value;
                    if (m.Object == null)
                        value = EvaluateConstant<string>(m.Arguments[1]);
                    else
                        throw new MongoQueryException(string.Format("Only the static Regex.IsMatch is supported.", m.Method.Name));

                    _js.AppendFormat("/{0}/", value);
                    return m;
                }
            }

            throw new NotSupportedException(string.Format("The method {0} is not supported.", m.Method.Name));
        }

        protected override NewExpression VisitNew(NewExpression nex)
        {
            _js.Append(new JavascriptObjectFormatter().FormatObject(nex));
            return nex;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    _js.Append("-");
                    Visit(u.Operand);
                    break;
                case ExpressionType.UnaryPlus:
                    _js.Append("+");
                    Visit(u.Operand);
                    break;
                case ExpressionType.Not:
                    _js.Append("!(");
                    Visit(u.Operand);
                    _js.Append(")");
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator {0} is not supported.", u.NodeType));
            }

            return u;
        }

        private static T EvaluateConstant<T>(Expression e)
        {
            if (e.NodeType != ExpressionType.Constant)
                throw new ArgumentException("Expression must be a constant.");

            return (T)((ConstantExpression)e).Value;
        }

        private static string GetJavascriptValueForConstant(ConstantExpression c)
        {
            if (c.Value == null)
                return "null";
            else if (c.Value.GetType().IsPrimitive)
                return c.Value.ToString();
            else
                return "\"" + c.Value + "\"";
        }


        private class JavascriptObjectFormatter : MongoExpressionVisitor
        {
            private StringBuilder _js;
            private readonly JavascriptFormatter _formatter;

            public JavascriptObjectFormatter()
            {
                _formatter = new JavascriptFormatter();
            }

            public string FormatObject(Expression nex)
            {
                _js = new StringBuilder("{");
                Visit(nex);
                return _js.Append("}").ToString();
            }

            protected override NewExpression VisitNew(NewExpression nex)
            {
                var parameters = nex.Constructor.GetParameters();
                for(int i = 0; i < nex.Arguments.Count; i++)
                {
                    if (i > 0)
                        _js.Append(", ");
                    _js.AppendFormat("{0}: {1}", parameters[i].Name, _formatter.FormatJavascript(nex.Arguments[i]));
                }
                return nex;
            }
        }
    }
}