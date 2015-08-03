/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a FindAndModify command.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(CommandResultSerializer<FindAndModifyResult>))]
    public class FindAndModifyResult : CommandResult
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindAndModifyResult"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public FindAndModifyResult(BsonDocument response)
            : base(response)
        {
        }

        // public properties
        /// <summary>
        /// Gets the modified document.
        /// </summary>
        public BsonDocument ModifiedDocument
        {
            get
            {
                var value = Response["value"];
                return (value.IsBsonNull) ? null : value.AsBsonDocument;
            }
        }

        // public methods
        /// <summary>
        /// Gets the modified document as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The nominal type of the modified document.</typeparam>
        /// <returns>The modified document.</returns>
        public TDocument GetModifiedDocumentAs<TDocument>()
        {
            return (TDocument)GetModifiedDocumentAs(typeof(TDocument));
        }

        /// <summary>
        /// Gets the modified document as a TDocument.
        /// </summary>
        /// <param name="documentType">The nominal type of the modified document.</param>
        /// <returns>The modified document.</returns>
        public object GetModifiedDocumentAs(Type documentType)
        {
            var document = ModifiedDocument;
            return (document == null) ? null : BsonSerializer.Deserialize(document, documentType);
        }
    }
}
