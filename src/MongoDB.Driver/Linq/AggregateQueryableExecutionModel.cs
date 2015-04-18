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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A model for a queryable to be executed using the aggregation framework.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public class AggregateQuerableExecutionModel<TOutput> : QueryableExecutionModel
    {
        private readonly ReadOnlyCollection<BsonDocument> _documents;
        private readonly IBsonSerializer<TOutput> _outputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateQuerableExecutionModel{TOutput}"/> class.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="outputSerializer">The output serializer.</param>
        public AggregateQuerableExecutionModel(IEnumerable<BsonDocument> documents, IBsonSerializer<TOutput> outputSerializer)
        {
            _documents = Ensure.IsNotNull(documents, "documents") as ReadOnlyCollection<BsonDocument>;
            if (_documents == null)
            {
                _documents = new List<BsonDocument>(documents).AsReadOnly();
            }
            _outputSerializer = Ensure.IsNotNull(outputSerializer, "outputSerializer");
        }

        /// <summary>
        /// Gets the documents.
        /// </summary>
        public ReadOnlyCollection<BsonDocument> Documents
        {
            get { return _documents; }
        }

        /// <summary>
        /// Gets the output serializer.
        /// </summary>
        public IBsonSerializer<TOutput> OutputSerializer
        {
            get { return _outputSerializer; }
        }

        /// <summary>
        /// Gets the type of the output.
        /// </summary>
        public override Type OutputType
        {
            get { return typeof(TOutput); }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder("aggregate([");
            if (_documents.Count > 0)
            {
                sb.Append(string.Join(", ", _documents.Select(x => x.ToString())));
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
