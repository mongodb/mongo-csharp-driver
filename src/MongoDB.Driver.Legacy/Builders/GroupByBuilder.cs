/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
    public static class GroupBy
    {
        // public static methods
        /// <summary>
        /// Sets a key function.
        /// </summary>
        /// <param name="keyFunction">The key function.</param>
        /// <returns>A BsonJavaScript.</returns>
        public static BsonJavaScript Function(BsonJavaScript keyFunction)
        {
            return keyFunction;
        }

        /// <summary>
        /// Sets one or more key names.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GroupByBuilder Keys(params string[] names)
        {
            return new GroupByBuilder(names);
        }
    }

    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(GroupByBuilder.Serializer))]
    public class GroupByBuilder : BuilderBase, IMongoGroupBy
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupByBuilder"/> class.
        /// </summary>
        public GroupByBuilder()
        {
            _document = new BsonDocument();
        }

        /// <summary>
        /// Initializes a new instance of the GroupByBuilder class.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        public GroupByBuilder(string[] names)
            : this()
        {
            Keys(names);
        }

        // public methods
        /// <summary>
        /// Sets one or more key names.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GroupByBuilder Keys(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, 1);
            }

            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // nested classes
        new internal class Serializer : SerializerBase<GroupByBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GroupByBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
        }
    }

    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class GroupBy<TDocument>
    {
        // public static methods
        /// <summary>
        /// Sets one or more key names.
        /// </summary>
        /// <param name="memberExpressions">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static GroupByBuilder<TDocument> Keys(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return new GroupByBuilder<TDocument>().Keys(memberExpressions);
        }
    }

    /// <summary>
    /// A builder for specifying what the GroupBy command should group by.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [BsonSerializer(typeof(GroupByBuilder<>.Serializer))]
    public class GroupByBuilder<TDocument> : BuilderBase, IMongoGroupBy
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private GroupByBuilder _groupByBuilder;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupByBuilder{TDocument}"/> class.
        /// </summary>
        public GroupByBuilder()
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
            _groupByBuilder = new GroupByBuilder();
        }

        // public methods
        /// <summary>
        /// Sets one or more key names.
        /// </summary>
        /// <param name="memberExpressions">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public GroupByBuilder<TDocument> Keys(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            var names = memberExpressions
                .Select(x => _serializationInfoHelper.GetSerializationInfo(x))
                .Select(x => x.ElementName);

            _groupByBuilder = _groupByBuilder.Keys(names.ToArray());
            return this;
        }

        /// <summary>
        /// Converts this object to a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _groupByBuilder.ToBsonDocument();
        }

        // nested classes
        new internal class Serializer : SerializerBase<GroupByBuilder<TDocument>>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GroupByBuilder<TDocument> value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._groupByBuilder.ToBsonDocument());
            }
        }
    }
}
