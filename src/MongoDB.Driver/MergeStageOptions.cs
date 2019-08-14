/* Copyright 2019-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// The behavior of $merge is a result document and an existing document in the collection
    /// have the same value for the specified on field(s).
    /// </summary>
    public enum MergeStageWhenMatched
    {
        /// <summary>
        /// Replace the existing document in the output collection with the matching results document.
        /// </summary>
        Replace,

        /// <summary>
        /// Keep the existing document in the output collection.
        /// </summary>
        KeepExisting,

        /// <summary>
        /// Merge the matching documents (similar to the $mergeObjects operator).
        /// </summary>
        Merge,

        /// <summary>
        /// Stop and fail the aggregation. Any changes to the output collection from previous documents are not reverted.
        /// </summary>
        Fail,

        /// <summary>
        /// Use an aggregation pipeline to update the document in the collection.
        /// </summary>
        Pipeline
    }

    /// <summary>
    /// The behavior of $merge if a result document does not match an existing document in the output collection.
    /// </summary>
    public enum MergeStageWhenNotMatched
    {
        /// <summary>
        /// Insert the document into the output collection.
        /// </summary>
        Insert,

        /// <summary>
        /// Discard the document; i.e. $merge does not insert the document into the output collection.
        /// </summary>
        Discard,

        /// <summary>
        /// Stop and fail the aggregation operation. Any changes to the output collection from previous documents are not reverted.
        /// </summary>
        Fail
    }

    /// <summary>
    /// Options for the $merge aggregation pipeline stage.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output documents.</typeparam>
    public class MergeStageOptions<TOutput>
    {
        // private fields
        private BsonDocument _letVariables;
        private IReadOnlyList<string> _onFieldNames;
        private IBsonSerializer<TOutput> _outputSerializer;
        private MergeStageWhenMatched? _whenMatched;
        private PipelineDefinition<TOutput, TOutput> _whenMatchedPipeline;
        private MergeStageWhenNotMatched? _whenNotMatched;

        // public properties
        /// <summary>
        /// Specifies variables accessible for use in the WhenMatchedPipeline.
        /// </summary>
        public BsonDocument LetVariables
        {
            get => _letVariables;
            set => _letVariables = value;
        }

        /// <summary>
        /// Field or fields that act as a unique identifier for a document. The identifier determines if a results
        /// document matches an already existing document in the output collection.
        /// </summary>
        public IReadOnlyList<string> OnFieldNames
        {
            get => _onFieldNames;
            set => _onFieldNames = value;
        }

        /// <summary>
        /// The output serializer.
        /// </summary>
        public IBsonSerializer<TOutput> OutputSerializer
        {
            get => _outputSerializer;
            set => _outputSerializer = value;
        }

        /// <summary>
        /// The behavior of $merge if a result document and an existing document in the collectoin have the
        /// same value for the specified on field(s).
        /// </summary>
        public MergeStageWhenMatched? WhenMatched
        {
            get => _whenMatched;
            set => _whenMatched = value;
        }

        /// <summary>
        /// An aggregation pipeline to update the document in the collection.
        /// Used when WhenMatched is Pipeline.
        /// </summary>
        public PipelineDefinition<TOutput, TOutput> WhenMatchedPipeline
        {
            get => _whenMatchedPipeline;
            set => _whenMatchedPipeline = value;
        }

        /// <summary>
        /// The behavior of $merge if a result document does not match an existing document in the output collection.
        /// </summary>
        public MergeStageWhenNotMatched? WhenNotMatched
        {
            get => _whenNotMatched;
            set => _whenNotMatched = value;
        }
    }
}
