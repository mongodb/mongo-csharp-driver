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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Translators
{
    /// <summary>
    /// Translates an expression tree into a <see cref="FilterDefinition{TDocument}"/>.
    /// </summary>
    internal class PredicateTranslator
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> __builder = new FilterDefinitionBuilder<BsonDocument>();

        public static BsonDocument Translate<TDocument>(Expression<Func<TDocument, bool>> predicate, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            // TODO: revisit this...
            var parameterSerializationInfo = new BsonSerializationInfo(null, parameterSerializer, parameterSerializer.ValueType);
            var parameterExpression = new SerializationExpression(predicate.Parameters[0], parameterSerializationInfo);
            var binder = new SerializationInfoBinder(BsonSerializer.SerializerRegistry);
            binder.RegisterParameterReplacement(predicate.Parameters[0], parameterExpression);
            var node = Transformer.Transform(predicate.Body);
            node = PartialEvaluator.Evaluate(node);
            node = binder.Bind(node);

            return Translate(node, serializerRegistry);
        }

        public static BsonDocument Translate(Expression node, IBsonSerializerRegistry serializerRegistry)
        {
            var translator = new PredicateTranslator();
            return translator.BuildFilter(node)
                .Render(serializerRegistry.GetSerializer<BsonDocument>(), serializerRegistry);
        }

        private PredicateTranslator()
        {
        }

        private FilterDefinition<BsonDocument> BuildFilter(Expression expression)
        {
            FilterDefinition<BsonDocument> filter = null;

            switch (expression.NodeType)
            {
                case ExpressionType.And:
                    filter = BuildAndQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.AndAlso:
                    filter = BuildAndAlsoQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.ArrayIndex:
                    filter = BuildBooleanQuery(expression);
                    break;
                case ExpressionType.Call:
                    filter = BuildMethodCallQuery((MethodCallExpression)expression);
                    break;
                case ExpressionType.Constant:
                    filter = BuildConstantQuery((ConstantExpression)expression);
                    break;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    filter = BuildComparisonQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.MemberAccess:
                    filter = BuildBooleanQuery(expression);
                    break;
                case ExpressionType.Not:
                    filter = BuildNotQuery((UnaryExpression)expression);
                    break;
                case ExpressionType.Or:
                    filter = BuildOrQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.OrElse:
                    filter = BuildOrElseQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.TypeIs:
                    filter = BuildTypeIsQuery((TypeBinaryExpression)expression);
                    break;
                case ExpressionType.Extension:
                    var mongoExpression = expression as ExtensionExpression;
                    if (mongoExpression != null)
                    {
                        switch (mongoExpression.ExtensionType)
                        {
                            case ExtensionExpressionType.Serialization:
                                if (mongoExpression.Type == typeof(bool))
                                {
                                    filter = BuildBooleanQuery(mongoExpression);
                                }
                                break;
                        }
                    }
                    break;
            }

            if (filter == null)
            {
                var message = string.Format("Unsupported filter: {0}.", expression);
                throw new ArgumentException(message);
            }

            return filter;
        }

        // private methods
        private FilterDefinition<BsonDocument> BuildAndAlsoQuery(BinaryExpression binaryExpression)
        {
            return __builder.And(BuildFilter(binaryExpression.Left), BuildFilter(binaryExpression.Right));
        }

        private FilterDefinition<BsonDocument> BuildAndQuery(BinaryExpression binaryExpression)
        {
            if (binaryExpression.Left.Type == typeof(bool) && binaryExpression.Right.Type == typeof(bool))
            {
                return BuildAndAlsoQuery(binaryExpression);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildAnyQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                var serializationInfo = GetSerializationInfo(arguments[0]);
                if (arguments.Length == 1)
                {
                    return __builder.And(
                        __builder.Ne(serializationInfo.ElementName, BsonNull.Value),
                        __builder.Not(__builder.Size(serializationInfo.ElementName, 0)));
                }
                else if (arguments.Length == 2)
                {
                    FilterDefinition<BsonDocument> filter;

                    var lambda = (LambdaExpression)arguments[1];
                    bool renderWithoutElemMatch = CanAnyBeRenderedWithoutElemMatch(lambda.Body);

                    if (renderWithoutElemMatch)
                    {
                        filter = BuildFilter(lambda.Body);
                    }
                    else
                    {
                        var itemSerializationInfo = GetItemSerializationInfo("Any", serializationInfo);
                        var body = PrefixedFieldRenamer.Rename(lambda.Body, serializationInfo.ElementName);
                        filter = __builder.ElemMatch(serializationInfo.ElementName, BuildFilter(body));
                        if (!(itemSerializationInfo.Serializer is IBsonDocumentSerializer))
                        {
                            filter = new ScalarElementMatchFilterDefinition<BsonDocument>(filter);
                        }
                    }

                    return filter;
                }
            }
            return null;
        }

        private bool CanAnyBeRenderedWithoutElemMatch(Expression expression)
        {
            switch (expression.NodeType)
            {
                // this doesn't cover all cases, but absolutely covers
                // the most common ones. This is opt-in behavior, so
                // when someone else discovers an Any query that shouldn't
                // be rendered with $elemMatch, we'll have to add it in.
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return true;
                case ExpressionType.Call:
                    var callNode = (MethodCallExpression)expression;
                    switch (callNode.Method.Name)
                    {
                        case "Any":
                            return callNode.Arguments.Count == 2;
                        case "Contains":
                        case "StartsWith":
                        case "EndsWith":
                            return true;
                        default:
                            return false;
                    }
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Not:
                    var unaryExpression = (UnaryExpression)expression;
                    return CanAnyBeRenderedWithoutElemMatch(unaryExpression.Operand);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Builds the array length query.
        /// </summary>
        /// <param name="variableExpression">The variable expression.</param>
        /// <param name="operatorType">Type of the operator.</param>
        /// <param name="constantExpression">The constant expression.</param>
        /// <returns></returns>
        private FilterDefinition<BsonDocument> BuildArrayLengthQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            var allowedOperators = new[]
            {
                ExpressionType.Equal,
                ExpressionType.NotEqual,
                ExpressionType.GreaterThan,
                ExpressionType.GreaterThanOrEqual,
                ExpressionType.LessThan,
                ExpressionType.LessThanOrEqual
            };

            if (!allowedOperators.Contains(operatorType))
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            BsonSerializationInfo serializationInfo = null;

            var unaryExpression = variableExpression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.ArrayLength)
            {
                TryGetSerializationInfo(unaryExpression.Operand, out serializationInfo);
            }

            var memberExpression = variableExpression as MemberExpression;
            if (memberExpression != null && memberExpression.Member.Name == "Count")
            {
                TryGetSerializationInfo(memberExpression.Expression, out serializationInfo);
            }

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "Count" && methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 1 && methodCallExpression.Arguments[0].Type != typeof(string))
                {
                    TryGetSerializationInfo(methodCallExpression.Arguments[0], out serializationInfo);
                }
            }

            if (serializationInfo != null)
            {
                switch (operatorType)
                {
                    case ExpressionType.Equal:
                        return __builder.Size(serializationInfo.ElementName, value);
                    case ExpressionType.NotEqual:
                        return __builder.Not(__builder.Size(serializationInfo.ElementName, value));
                    case ExpressionType.GreaterThan:
                        return __builder.SizeGt(serializationInfo.ElementName, value);
                    case ExpressionType.GreaterThanOrEqual:
                        return __builder.SizeGte(serializationInfo.ElementName, value);
                    case ExpressionType.LessThan:
                        return __builder.SizeLt(serializationInfo.ElementName, value);
                    case ExpressionType.LessThanOrEqual:
                        return __builder.SizeLte(serializationInfo.ElementName, value);
                }
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildBooleanQuery(bool value)
        {
            if (value)
            {
                return new BsonDocument(); // empty query matches all documents
            }
            else
            {
                return __builder.Type("_id", (BsonType)(-1)); // matches no documents (and uses _id index when used at top level)
            }
        }

        private FilterDefinition<BsonDocument> BuildBooleanQuery(Expression expression)
        {
            if (expression.Type == typeof(bool))
            {
                var constantExpression = expression as ConstantExpression;
                if (constantExpression != null)
                {
                    return BuildBooleanQuery((bool)constantExpression.Value);
                }

                var serializationInfo = GetSerializationInfo(expression);
                return new BsonDocument(serializationInfo.ElementName, true);
            }
            return null;
        }

        private FilterDefinition<BsonDocument> BuildComparisonQuery(BinaryExpression binaryExpression)
        {
            // the constant could be on either side
            var variableExpression = binaryExpression.Left;
            var constantExpression = binaryExpression.Right as ConstantExpression;
            var operatorType = binaryExpression.NodeType;

            if (constantExpression == null)
            {
                return null;
            }

            var query = BuildArrayLengthQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildModQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildCompareToQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringIndexOfQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringIndexQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringLengthQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringCaseInsensitiveComparisonQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildTypeComparisonQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            return BuildComparisonQuery(variableExpression, operatorType, constantExpression);
        }

        private FilterDefinition<BsonDocument> BuildCompareToQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (constantExpression.Type != typeof(int) || ((int)constantExpression.Value) != 0)
            {
                return null;
            }

            var call = variableExpression as MethodCallExpression;
            if (call == null || call.Object == null || call.Method.Name != "CompareTo" || call.Arguments.Count != 1)
            {
                return null;
            }

            constantExpression = call.Arguments[0] as ConstantExpression;
            if (constantExpression == null)
            {
                return null;
            }

            return BuildComparisonQuery(call.Object, operatorType, constantExpression);
        }

        private FilterDefinition<BsonDocument> BuildComparisonQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            var value = constantExpression.Value;

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && value is bool)
            {
                var boolValue = (bool)value;
                var query = this.BuildMethodCallQuery(methodCallExpression);

                var isTrueComparison = (boolValue && operatorType == ExpressionType.Equal)
                                        || (!boolValue && operatorType == ExpressionType.NotEqual);

                return isTrueComparison ? query : __builder.Not(query);
            }

            var serializationInfo = GetSerializationInfo(variableExpression);
            var valueType = serializationInfo.Serializer.ValueType;
            if (valueType.IsEnum || valueType.IsNullableEnum())
            {
                if (!valueType.IsEnum && value != null)
                {
                    valueType = valueType.GetNullableUnderlyingType();
                }

                if (value != null)
                {
                    value = Enum.ToObject(valueType, value);
                }
            }

            var serializedValue = serializationInfo.SerializeValue(value);
            switch (operatorType)
            {
                case ExpressionType.Equal: return __builder.Eq(serializationInfo.ElementName, serializedValue);
                case ExpressionType.GreaterThan: return __builder.Gt(serializationInfo.ElementName, serializedValue);
                case ExpressionType.GreaterThanOrEqual: return __builder.Gte(serializationInfo.ElementName, serializedValue);
                case ExpressionType.LessThan: return __builder.Lt(serializationInfo.ElementName, serializedValue);
                case ExpressionType.LessThanOrEqual: return __builder.Lte(serializationInfo.ElementName, serializedValue);
                case ExpressionType.NotEqual: return __builder.Ne(serializationInfo.ElementName, serializedValue);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildConstantQuery(ConstantExpression constantExpression)
        {
            var value = constantExpression.Value;
            if (value != null && value.GetType() == typeof(bool))
            {
                return BuildBooleanQuery((bool)value);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildContainsKeyQuery(MethodCallExpression methodCallExpression)
        {
            var dictionaryType = methodCallExpression.Object.Type;
            var implementedInterfaces = new List<Type>(dictionaryType.GetInterfaces());
            if (dictionaryType.IsInterface)
            {
                implementedInterfaces.Add(dictionaryType);
            }

            Type dictionaryGenericInterface = null;
            Type dictionaryInterface = null;
            foreach (var implementedInterface in implementedInterfaces)
            {
                if (implementedInterface.IsGenericType)
                {
                    if (implementedInterface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        dictionaryGenericInterface = implementedInterface;
                    }
                }
                else if (implementedInterface == typeof(IDictionary))
                {
                    dictionaryInterface = implementedInterface;
                }
            }

            if (dictionaryGenericInterface == null && dictionaryInterface == null)
            {
                return null;
            }

            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length != 1)
            {
                return null;
            }

            var constantExpression = arguments[0] as ConstantExpression;
            if (constantExpression == null)
            {
                return null;
            }
            var key = constantExpression.Value;

            var serializationInfo = GetSerializationInfo(methodCallExpression.Object);
            var serializer = serializationInfo.Serializer;

            var dictionarySerializer = serializer as IBsonDictionarySerializer;
            if (dictionarySerializer == null)
            {
                var message = string.Format(
                    "{0} in a LINQ query is only supported for members that are serialized using a serializer that implements IBsonDictionarySerializer.",
                    methodCallExpression.Method.Name); // could be Contains (for IDictionary) or ContainsKey (for IDictionary<TKey, TValue>)
                throw new NotSupportedException(message);
            }

            var keySerializer = dictionarySerializer.KeySerializer;
            var keySerializationInfo = new BsonSerializationInfo(
                null, // elementName
                keySerializer,
                keySerializer.ValueType);
            var serializedKey = keySerializationInfo.SerializeValue(key);

            var dictionaryRepresentation = dictionarySerializer.DictionaryRepresentation;
            switch (dictionaryRepresentation)
            {
                case DictionaryRepresentation.ArrayOfDocuments:
                    return __builder.Eq(serializationInfo.ElementName + ".k", serializedKey);
                case DictionaryRepresentation.Document:
                    return __builder.Exists(serializationInfo.ElementName + "." + serializedKey.AsString);
                default:
                    var message = string.Format(
                        "{0} in a LINQ query is only supported for DictionaryRepresentation ArrayOfDocuments or Document, not {1}.",
                        methodCallExpression.Method.Name, // could be Contains (for IDictionary) or ContainsKey (for IDictionary<TKey, TValue>)
                        dictionaryRepresentation);
                    throw new NotSupportedException(message);
            }
        }

        private FilterDefinition<BsonDocument> BuildContainsQuery(MethodCallExpression methodCallExpression)
        {
            // handle IDictionary Contains the same way as IDictionary<TKey, TValue> ContainsKey
            if (methodCallExpression.Object != null && typeof(IDictionary).IsAssignableFrom(methodCallExpression.Object.Type))
            {
                return BuildContainsKeyQuery(methodCallExpression);
            }

            if (methodCallExpression.Method.DeclaringType == typeof(string))
            {
                return BuildStringQuery(methodCallExpression);
            }

            if (methodCallExpression.Object != null && methodCallExpression.Object.NodeType == ExpressionType.Constant)
            {
                return BuildInQuery(methodCallExpression);
            }

            BsonSerializationInfo serializationInfo = null;
            ConstantExpression valueExpression = null;
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length == 1)
            {
                if (typeof(IEnumerable).IsAssignableFrom(methodCallExpression.Method.DeclaringType))
                {
                    serializationInfo = GetSerializationInfo(methodCallExpression.Object);
                    valueExpression = arguments[0] as ConstantExpression;
                }
            }
            else if (arguments.Length == 2)
            {
                if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
                {
                    if (arguments[0].NodeType == ExpressionType.Constant)
                    {
                        return BuildInQuery(methodCallExpression);
                    }
                    serializationInfo = GetSerializationInfo(arguments[0]);
                    valueExpression = arguments[1] as ConstantExpression;
                }
            }

            if (serializationInfo != null && valueExpression != null)
            {
                var itemSerializationInfo = GetItemSerializationInfo("Contains", serializationInfo);
                var serializedValue = itemSerializationInfo.SerializeValue(valueExpression.Value);
                return __builder.Eq(serializationInfo.ElementName, serializedValue);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildEqualsQuery(MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();

            // assume that static and instance Equals mean the same thing for all classes (i.e. an equality test)
            Expression firstExpression = null;
            Expression secondExpression = null;
            if (methodCallExpression.Object == null)
            {
                // static Equals method
                if (arguments.Length == 2)
                {
                    firstExpression = arguments[0];
                    secondExpression = arguments[1];
                }
            }
            else
            {
                // instance Equals method
                if (arguments.Length == 1)
                {
                    firstExpression = methodCallExpression.Object;
                    secondExpression = arguments[0];
                }
            }

            if (firstExpression != null && secondExpression != null)
            {
                // the constant could be either expression
                var variableExpression = firstExpression;
                var constantExpression = secondExpression as ConstantExpression;
                if (constantExpression == null)
                {
                    constantExpression = firstExpression as ConstantExpression;
                    variableExpression = secondExpression;
                }

                if (constantExpression == null)
                {
                    return null;
                }

                if (variableExpression.Type == typeof(Type) && constantExpression.Type == typeof(Type))
                {
                    return BuildTypeComparisonQuery(variableExpression, ExpressionType.Equal, constantExpression);
                }

                return BuildComparisonQuery(variableExpression, ExpressionType.Equal, constantExpression);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildInQuery(MethodCallExpression methodCallExpression)
        {
            var methodDeclaringType = methodCallExpression.Method.DeclaringType;
            var arguments = methodCallExpression.Arguments.ToArray();
            BsonSerializationInfo serializationInfo = null;
            ConstantExpression valuesExpression = null;
            if (methodDeclaringType == typeof(Enumerable) || methodDeclaringType == typeof(Queryable))
            {
                if (arguments.Length == 2)
                {
                    serializationInfo = GetSerializationInfo(arguments[1]);
                    valuesExpression = arguments[0] as ConstantExpression;
                }
            }
            else
            {
                if (methodDeclaringType.IsGenericType)
                {
                    methodDeclaringType = methodDeclaringType.GetGenericTypeDefinition();
                }

                bool contains = methodDeclaringType == typeof(ICollection<>) || methodDeclaringType.GetInterface("ICollection`1") != null;
                if (contains && arguments.Length == 1)
                {
                    serializationInfo = GetSerializationInfo(arguments[0]);
                    valuesExpression = methodCallExpression.Object as ConstantExpression;
                }
            }

            if (serializationInfo != null && valuesExpression != null)
            {
                var serializedValues = serializationInfo.SerializeValues((IEnumerable)valuesExpression.Value);
                return __builder.In(serializationInfo.ElementName, serializedValues);
            }
            return null;
        }

        private FilterDefinition<BsonDocument> BuildIsMatchQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Regex))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                var obj = methodCallExpression.Object;
                if (obj == null)
                {
                    if (arguments.Length == 2 || arguments.Length == 3)
                    {
                        var serializationInfo = GetSerializationInfo(arguments[0]);
                        var patternExpression = arguments[1] as ConstantExpression;
                        if (patternExpression != null)
                        {
                            var pattern = patternExpression.Value as string;
                            if (pattern != null)
                            {
                                var options = RegexOptions.None;
                                if (arguments.Length == 3)
                                {
                                    var optionsExpression = arguments[2] as ConstantExpression;
                                    if (optionsExpression == null || optionsExpression.Type != typeof(RegexOptions))
                                    {
                                        return null;
                                    }
                                    options = (RegexOptions)optionsExpression.Value;
                                }
                                var regex = new Regex(pattern, options);
                                return __builder.Regex(serializationInfo.ElementName, regex);
                            }
                        }
                    }
                }
                else
                {
                    var regexExpression = obj as ConstantExpression;
                    if (regexExpression != null && arguments.Length == 1)
                    {
                        var serializationInfo = GetSerializationInfo(arguments[0]);
                        var regex = regexExpression.Value as Regex;
                        if (regex != null)
                        {
                            return __builder.Regex(serializationInfo.ElementName, regex);
                        }
                    }
                }
            }
            return null;
        }

        private FilterDefinition<BsonDocument> BuildIsNullOrEmptyQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string) && methodCallExpression.Object == null)
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                var serializationInfo = GetSerializationInfo(arguments[0]);
                return __builder.In<string>(serializationInfo.ElementName, new string[] { null, "" });
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildMethodCallQuery(MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression.Method.Name)
            {
                case "Any": return BuildAnyQuery(methodCallExpression);
                case "Contains": return BuildContainsQuery(methodCallExpression);
                case "ContainsKey": return BuildContainsKeyQuery(methodCallExpression);
                case "EndsWith": return BuildStringQuery(methodCallExpression);
                case "Equals": return BuildEqualsQuery(methodCallExpression);
                case "In": return BuildInQuery(methodCallExpression);
                case "IsMatch": return BuildIsMatchQuery(methodCallExpression);
                case "IsNullOrEmpty": return BuildIsNullOrEmptyQuery(methodCallExpression);
                case "StartsWith": return BuildStringQuery(methodCallExpression);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildModQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (operatorType != ExpressionType.Equal && operatorType != ExpressionType.NotEqual)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int) && constantExpression.Type != typeof(long))
            {
                return null;
            }
            var value = ToInt64(constantExpression);

            var modBinaryExpression = variableExpression as BinaryExpression;
            if (modBinaryExpression != null && modBinaryExpression.NodeType == ExpressionType.Modulo)
            {
                var serializationInfo = GetSerializationInfo(modBinaryExpression.Left);
                var modulusExpression = modBinaryExpression.Right as ConstantExpression;
                if (modulusExpression != null)
                {
                    var modulus = ToInt64(modulusExpression);
                    if (operatorType == ExpressionType.Equal)
                    {
                        return __builder.Mod(serializationInfo.ElementName, modulus, value);
                    }
                    else
                    {
                        return __builder.Not(__builder.Mod(serializationInfo.ElementName, modulus, value));
                    }
                }
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildNotQuery(UnaryExpression unaryExpression)
        {
            var filter = BuildFilter(unaryExpression.Operand);
            return __builder.Not(filter);
        }

        private FilterDefinition<BsonDocument> BuildOrElseQuery(BinaryExpression binaryExpression)
        {
            return __builder.Or(BuildFilter(binaryExpression.Left), BuildFilter(binaryExpression.Right));
        }

        private FilterDefinition<BsonDocument> BuildOrQuery(BinaryExpression binaryExpression)
        {
            if (binaryExpression.Left.Type == typeof(bool) && binaryExpression.Right.Type == typeof(bool))
            {
                return BuildOrElseQuery(binaryExpression);
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildStringIndexOfQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            // TODO: support other comparison operators
            if (operatorType != ExpressionType.Equal)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var index = ToInt32(constantExpression);

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null &&
                (methodCallExpression.Method.Name == "IndexOf" || methodCallExpression.Method.Name == "IndexOfAny") &&
                methodCallExpression.Method.DeclaringType == typeof(string))
            {
                var serializationInfo = GetSerializationInfo(methodCallExpression.Object);

                object value;
                var startIndex = -1;
                var count = -1;

                var args = methodCallExpression.Arguments.ToArray();
                switch (args.Length)
                {
                    case 3:
                        var countExpression = args[2] as ConstantExpression;
                        if (countExpression == null)
                        {
                            return null;
                        }
                        count = ToInt32(countExpression);
                        goto case 2;
                    case 2:
                        var startIndexExpression = args[1] as ConstantExpression;
                        if (startIndexExpression == null)
                        {
                            return null;
                        }
                        startIndex = ToInt32(startIndexExpression);
                        goto case 1;
                    case 1:
                        var valueExpression = args[0] as ConstantExpression;
                        if (valueExpression == null)
                        {
                            return null;
                        }
                        value = valueExpression.Value;
                        break;
                    default:
                        return null;
                }

                string pattern = null;
                if (value.GetType() == typeof(char) || value.GetType() == typeof(char[]))
                {
                    char[] chars;
                    if (value.GetType() == typeof(char))
                    {
                        chars = new char[] { (char)value };
                    }
                    else
                    {
                        chars = (char[])value;
                    }
                    var positiveClass = string.Join("", chars.Select(c => (c == '-') ? "\\-" : (c == ']') ? "\\]" : Regex.Escape(c.ToString())).ToArray());
                    var negativeClass = "[^" + positiveClass + "]";
                    if (chars.Length > 1)
                    {
                        positiveClass = "[" + positiveClass + "]";
                    }

                    if (startIndex == -1)
                    {
                        // the regex for: IndexOf(c) == index 
                        // is: /^[^c]{index}c/
                        pattern = string.Format("^{0}{{{1}}}{2}", negativeClass, index, positiveClass);
                    }
                    else
                    {
                        if (count == -1)
                        {
                            // the regex for: IndexOf(c, startIndex) == index
                            // is: /^.{startIndex}[^c]{index - startIndex}c/
                            pattern = string.Format("^.{{{0}}}{1}{{{2}}}{3}", startIndex, negativeClass, index - startIndex, positiveClass);
                        }
                        else
                        {
                            if (index >= startIndex + count)
                            {
                                // index is outside of the substring so no match is possible
                                return BuildBooleanQuery(false);
                            }
                            else
                            {
                                // the regex for: IndexOf(c, startIndex, count) == index
                                // is: /^.{startIndex}(?=.{count})[^c]{index - startIndex}c/
                                pattern = string.Format("^.{{{0}}}(?=.{{{1}}}){2}{{{3}}}{4}", startIndex, count, negativeClass, index - startIndex, positiveClass);
                            }
                        }
                    }
                }
                else if (value.GetType() == typeof(string))
                {
                    var escapedString = Regex.Escape((string)value);
                    if (startIndex == -1)
                    {
                        // the regex for: IndexOf(s) == index 
                        // is: /^(?!.{0,index - 1}s).{index}s/
                        pattern = string.Format("^(?!.{{0,{2}}}{0}).{{{1}}}{0}", escapedString, index, index - 1);
                    }
                    else
                    {
                        if (count == -1)
                        {
                            // the regex for: IndexOf(s, startIndex) == index
                            // is: /^.{startIndex}(?!.{0, index - startIndex - 1}s).{index - startIndex}s/
                            pattern = string.Format("^.{{{1}}}(?!.{{0,{2}}}{0}).{{{3}}}{0}", escapedString, startIndex, index - startIndex - 1, index - startIndex);
                        }
                        else
                        {
                            var unescapedLength = ((string)value).Length;
                            if (unescapedLength > startIndex + count - index)
                            {
                                // substring isn't long enough to match
                                return BuildBooleanQuery(false);
                            }
                            else
                            {
                                // the regex for: IndexOf(s, startIndex, count) == index
                                // is: /^.{startIndex}(?=.{count})(?!.{0,index - startIndex - 1}s).{index - startIndex)s/
                                pattern = string.Format("^.{{{1}}}(?=.{{{2}}})(?!.{{0,{3}}}{0}).{{{4}}}{0}", escapedString, startIndex, count, index - startIndex - 1, index - startIndex);
                            }
                        }
                    }
                }

                if (pattern != null)
                {
                    return __builder.Regex(serializationInfo.ElementName, new BsonRegularExpression(pattern, "s"));
                }
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildStringIndexQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            var unaryExpression = variableExpression as UnaryExpression;
            if (unaryExpression == null)
            {
                return null;
            }

            if (unaryExpression.NodeType != ExpressionType.Convert || unaryExpression.Type != typeof(int))
            {
                return null;
            }

            var methodCallExpression = unaryExpression.Operand as MethodCallExpression;
            if (methodCallExpression == null)
            {
                return null;
            }

            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(string) || method.Name != "get_Chars")
            {
                return null;
            }

            var stringExpression = methodCallExpression.Object;
            if (stringExpression == null)
            {
                return null;
            }

            var args = methodCallExpression.Arguments.ToArray();
            if (args.Length != 1)
            {
                return null;
            }

            var indexExpression = args[0] as ConstantExpression;
            if (indexExpression == null)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }

            var index = ToInt32(indexExpression);
            var value = ToInt32(constantExpression);

            var c = new string((char)value, 1);
            var positiveClass = (c == "-") ? "\\-" : (c == "]") ? "\\]" : Regex.Escape(c);
            var negativeClass = "[^" + positiveClass + "]";

            string characterClass;
            switch (operatorType)
            {
                case ExpressionType.Equal:
                    characterClass = positiveClass;
                    break;
                case ExpressionType.NotEqual:
                    characterClass = negativeClass;
                    break;
                default:
                    return null; // TODO: suport other comparison operators?
            }
            var pattern = string.Format("^.{{{0}}}{1}", index, characterClass);

            var serializationInfo = GetSerializationInfo(stringExpression);
            return __builder.Regex(serializationInfo.ElementName, new BsonRegularExpression(pattern, "s"));
        }

        private FilterDefinition<BsonDocument> BuildStringLengthQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            BsonSerializationInfo serializationInfo = null;

            var memberExpression = variableExpression as MemberExpression;
            if (memberExpression != null && memberExpression.Member.Name == "Length")
            {
                TryGetSerializationInfo(memberExpression.Expression, out serializationInfo);
            }

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "Count" && methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var args = methodCallExpression.Arguments.ToArray();
                if (args.Length == 1 && args[0].Type == typeof(string))
                {
                    TryGetSerializationInfo(args[0], out serializationInfo);
                }
            }

            if (serializationInfo != null)
            {
                string regex = null;
                switch (operatorType)
                {
                    case ExpressionType.NotEqual:
                    case ExpressionType.Equal: regex = @"/^.{" + value.ToString() + "}$/s"; break;
                    case ExpressionType.GreaterThan: regex = @"/^.{" + (value + 1).ToString() + ",}$/s"; break;
                    case ExpressionType.GreaterThanOrEqual: regex = @"/^.{" + value.ToString() + ",}$/s"; break;
                    case ExpressionType.LessThan: regex = @"/^.{0," + (value - 1).ToString() + "}$/s"; break;
                    case ExpressionType.LessThanOrEqual: regex = @"/^.{0," + value.ToString() + "}$/s"; break;
                }
                if (regex != null)
                {
                    if (operatorType == ExpressionType.NotEqual)
                    {
                        return __builder.Not(__builder.Regex(serializationInfo.ElementName, regex));
                    }
                    else
                    {
                        return __builder.Regex(serializationInfo.ElementName, regex);
                    }
                }
            }

            return null;
        }

        private FilterDefinition<BsonDocument> BuildStringCaseInsensitiveComparisonQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            var methodExpression = variableExpression as MethodCallExpression;
            if (methodExpression == null)
            {
                return null;
            }

            var methodName = methodExpression.Method.Name;
            if ((methodName != "ToLower" && methodName != "ToUpper" && methodName != "ToLowerInvariant" && methodName != "ToUpperInvariant") ||
                methodExpression.Object == null ||
                methodExpression.Type != typeof(string) ||
                methodExpression.Arguments.Count != 0)
            {
                return null;
            }

            if (operatorType != ExpressionType.Equal && operatorType != ExpressionType.NotEqual)
            {
                return null;
            }

            var serializationInfo = GetSerializationInfo(methodExpression.Object);
            var serializedValue = serializationInfo.SerializeValue(constantExpression.Value);

            if (serializedValue.IsString)
            {
                var stringValue = serializedValue.AsString;
                var stringValueCaseMatches =
                    methodName == "ToLower" && stringValue == stringValue.ToLower(CultureInfo.InvariantCulture) ||
                    methodName == "ToLowerInvariant" && stringValue == stringValue.ToLower(CultureInfo.InvariantCulture) ||
                    methodName == "ToUpper" && stringValue == stringValue.ToUpper(CultureInfo.InvariantCulture) ||
                    methodName == "ToUpperInvariant" && stringValue == stringValue.ToUpper(CultureInfo.InvariantCulture);

                if (stringValueCaseMatches)
                {
                    string pattern = "/^" + Regex.Escape(stringValue) + "$/i";
                    var regex = new BsonRegularExpression(pattern);

                    if (operatorType == ExpressionType.Equal)
                    {
                        return __builder.Regex(serializationInfo.ElementName, regex);
                    }
                    else
                    {
                        return __builder.Not(__builder.Regex(serializationInfo.ElementName, regex));
                    }
                }
                else
                {
                    if (operatorType == ExpressionType.Equal)
                    {
                        // == "mismatched case" matches no documents
                        return BuildBooleanQuery(false);
                    }
                    else
                    {
                        // != "mismatched case" matches all documents
                        return BuildBooleanQuery(true);
                    }
                }
            }
            else if (serializedValue.IsBsonNull)
            {
                if (operatorType == ExpressionType.Equal)
                {
                    return __builder.Eq(serializationInfo.ElementName, BsonNull.Value);
                }
                else
                {
                    return __builder.Ne(serializationInfo.ElementName, BsonNull.Value);
                }
            }
            else
            {
                var message = string.Format("When using {0} in a LINQ string comparison the value being compared to must serialize as a string.", methodName);
                throw new ArgumentException(message);
            }
        }

        private FilterDefinition<BsonDocument> BuildStringQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(string))
            {
                return null;
            }

            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length != 1)
            {
                return null;
            }

            var stringExpression = methodCallExpression.Object;
            var constantExpression = arguments[0] as ConstantExpression;
            if (constantExpression == null)
            {
                return null;
            }

            var pattern = Regex.Escape((string)constantExpression.Value);
            switch (methodCallExpression.Method.Name)
            {
                case "Contains": pattern = ".*" + pattern + ".*"; break;
                case "EndsWith": pattern = ".*" + pattern; break;
                case "StartsWith": pattern = pattern + ".*"; break; // query optimizer will use index for rooted regular expressions
                default: return null;
            }

            var caseInsensitive = false;
            MethodCallExpression stringMethodCallExpression;
            while ((stringMethodCallExpression = stringExpression as MethodCallExpression) != null)
            {
                var trimStart = false;
                var trimEnd = false;
                Expression trimCharsExpression = null;
                switch (stringMethodCallExpression.Method.Name)
                {
                    case "ToLower":
                    case "ToLowerInvariant":
                    case "ToUpper":
                    case "ToUpperInvariant":
                        caseInsensitive = true;
                        break;
                    case "Trim":
                        trimStart = true;
                        trimEnd = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.FirstOrDefault();
                        break;
                    case "TrimEnd":
                        trimEnd = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.First();
                        break;
                    case "TrimStart":
                        trimStart = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.First();
                        break;
                    default:
                        return null;
                }

                if (trimStart || trimEnd)
                {
                    var trimCharsPattern = GetTrimCharsPattern(trimCharsExpression);
                    if (trimCharsPattern == null)
                    {
                        return null;
                    }

                    if (trimStart)
                    {
                        pattern = trimCharsPattern + pattern;
                    }
                    if (trimEnd)
                    {
                        pattern = pattern + trimCharsPattern;
                    }
                }

                stringExpression = stringMethodCallExpression.Object;
            }

            pattern = "^" + pattern + "$";
            if (pattern.StartsWith("^.*"))
            {
                pattern = pattern.Substring(3);
            }
            if (pattern.EndsWith(".*$"))
            {
                pattern = pattern.Substring(0, pattern.Length - 3);
            }

            var serializationInfo = GetSerializationInfo(stringExpression);
            var options = caseInsensitive ? "is" : "s";
            return __builder.Regex(serializationInfo.ElementName, new BsonRegularExpression(pattern, options));
        }

        private FilterDefinition<BsonDocument> BuildTypeComparisonQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (operatorType != ExpressionType.Equal)
            {
                // TODO: support NotEqual?
                return null;
            }

            if (constantExpression.Type != typeof(Type))
            {
                return null;
            }
            var actualType = (Type)constantExpression.Value;

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression == null)
            {
                return null;
            }
            if (methodCallExpression.Method.Name != "GetType" || methodCallExpression.Object == null)
            {
                return null;
            }
            if (methodCallExpression.Arguments.Count != 0)
            {
                return null;
            }

            var serializationInfo = GetSerializationInfo(methodCallExpression.Object);
            var nominalType = serializationInfo.NominalType;

            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return BuildBooleanQuery(true);
            }

            var elementName = discriminatorConvention.ElementName;
            if (serializationInfo.ElementName != null)
            {
                elementName = string.Format("{0}.{1}", serializationInfo.ElementName, elementName);
            }

            if (discriminator.IsBsonArray)
            {
                var discriminatorArray = discriminator.AsBsonArray;
                var queries = new FilterDefinition<BsonDocument>[discriminatorArray.Count + 1];
                queries[0] = __builder.Size(elementName, discriminatorArray.Count);
                for (var i = 0; i < discriminatorArray.Count; i++)
                {
                    queries[i + 1] = __builder.Eq(string.Format("{0}.{1}", elementName, i), discriminatorArray[i]);
                }
                return __builder.And(queries);
            }
            else
            {
                return __builder.And(
                    __builder.Exists(elementName + ".0", false), // trick to check that element is not an array
                    __builder.Eq(elementName, discriminator));
            }
        }

        private FilterDefinition<BsonDocument> BuildTypeIsQuery(TypeBinaryExpression typeBinaryExpression)
        {
            var nominalType = typeBinaryExpression.Expression.Type;
            var actualType = typeBinaryExpression.TypeOperand;

            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return BuildBooleanQuery(true);
            }

            if (discriminator.IsBsonArray)
            {
                discriminator = discriminator[discriminator.AsBsonArray.Count - 1];
            }

            var elementName = discriminatorConvention.ElementName;
            var serializationInfo = GetSerializationInfo(typeBinaryExpression.Expression);
            if (serializationInfo.ElementName != null)
            {
                elementName = string.Format("{0}.{1}", serializationInfo.ElementName, elementName);
            }
            return __builder.Eq(elementName, discriminator);
        }

        private string GetTrimCharsPattern(Expression trimCharsExpression)
        {
            if (trimCharsExpression == null)
            {
                return "\\s*";
            }

            var constantExpression = trimCharsExpression as ConstantExpression;
            if (constantExpression == null || !constantExpression.Type.IsArray || constantExpression.Type.GetElementType() != typeof(char))
            {
                return null;
            }

            var trimChars = (char[])constantExpression.Value;
            if (trimChars.Length == 0)
            {
                return "\\s*";
            }

            // build a pattern that matches the characters to be trimmed
            var characterClass = string.Join("", trimChars.Select(c => (c == '-') ? "\\-" : (c == ']') ? "\\]" : Regex.Escape(c.ToString())).ToArray());
            if (trimChars.Length > 1)
            {
                characterClass = "[" + characterClass + "]";
            }
            return characterClass + "*";
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

        private long ToInt64(Expression expression)
        {
            if (expression.Type != typeof(int) && expression.Type != typeof(long))
            {
                throw new ArgumentOutOfRangeException("expression", "Expected an Expression of Type Int32 or Int64.");
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression == null)
            {
                throw new ArgumentOutOfRangeException("expression", "Expected a ConstantExpression.");
            }

            if (expression.Type == typeof(int))
            {
                return (long)(int)constantExpression.Value;
            }
            else
            {
                return (long)constantExpression.Value;
            }
        }

        private bool TryGetSerializationInfo(Expression expression, out BsonSerializationInfo serializationInfo)
        {
            var serializationExpression = expression as ISerializationExpression;
            if (serializationExpression != null)
            {
                serializationInfo = serializationExpression.SerializationInfo;
                return true;
            }

            serializationInfo = null;
            return false;
        }

        private BsonSerializationInfo GetSerializationInfo(Expression expression)
        {
            BsonSerializationInfo serializationInfo;
            if (!TryGetSerializationInfo(expression, out serializationInfo))
            {
                var message = string.Format("{0} is not supported.",
                    expression.ToString());
                throw new InvalidOperationException(message);
            }

            return serializationInfo;
        }

        private BsonSerializationInfo GetItemSerializationInfo(string methodName, BsonSerializationInfo info)
        {
            var arraySerializer = info.Serializer as IBsonArraySerializer;
            BsonSerializationInfo itemSerializationInfo;
            if (arraySerializer == null || !arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
            {
                throw new InvalidOperationException(string.Format("{0} must have a serializer that supports retrieving item serialization info.", methodName));
            }

            return itemSerializationInfo;
        }

        /// <summary>
        /// This guy is going to replace expressions like Serialization("G.D") with Serialization("D").
        /// </summary>
        private class PrefixedFieldRenamer : ExtensionExpressionVisitor
        {
            public static Expression Rename(Expression node, string prefix)
            {
                var renamer = new PrefixedFieldRenamer(prefix);
                return renamer.Visit(node);
            }

            private string _prefix;

            private PrefixedFieldRenamer(string prefix)
            {
                _prefix = prefix;
            }

            protected internal override Expression VisitSerialization(SerializationExpression node)
            {
                if (node.SerializationInfo.ElementName.StartsWith(_prefix))
                {
                    var name = node.SerializationInfo.ElementName;
                    if (name == _prefix)
                    {
                        name = "";
                    }
                    else
                    {
                        name = name.Remove(0, _prefix.Length + 1);
                    }
                    return new SerializationExpression(
                        node.Expression,
                        node.SerializationInfo.WithNewName(name));
                }

                return base.VisitSerialization(node);
            }
        }
    }
}
