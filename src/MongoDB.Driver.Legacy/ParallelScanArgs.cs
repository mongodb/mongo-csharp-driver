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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the ParallelScan command helper method.
    /// </summary>
    public class ParallelScanArgs
    {
        // private fields
        private int? _batchSize;
        private int _numberOfCursors = 1;
        private ReadPreference _readPreference;
        private IBsonSerializer _serializer;

        // public properties
        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        /// <summary>
        /// Gets or sets the desired number of cursors.
        /// </summary>
        public int NumberOfCursors
        {
            get { return _numberOfCursors; }
            set { _numberOfCursors = value; }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }
    }

    /// <summary>
    /// Represents arguments for the ParallelScan command helper method.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class ParallelScanArgs<TDocument> : ParallelScanArgs
    {
        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        public new IBsonSerializer<TDocument> Serializer
        {
            get { return (IBsonSerializer<TDocument>)base.Serializer; }
            set { base.Serializer = value; }
        }
    }
}
