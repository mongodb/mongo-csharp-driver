/* Copyright 2018-present MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Transaction options.
    /// </summary>
    public class TransactionOptions
    {
        // private fields
        private readonly ReadConcern _readConcern;
        private readonly WriteConcern _writeConcern;

        // public constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionOptions" /> class.
        /// </summary>
        /// <param name="readConcern">The read concern.</param>
        /// <param name="writeConcern">The write concern.</param>
        public TransactionOptions(
            Optional<ReadConcern> readConcern = default(Optional<ReadConcern>),
            Optional<WriteConcern> writeConcern = default(Optional<WriteConcern>))
        {
            _readConcern = readConcern.WithDefault(null);
            _writeConcern = writeConcern.WithDefault(null);
        }

        // public properties
        /// <summary>
        /// Gets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern => _readConcern;

        /// <summary>
        /// Gets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern => _writeConcern;

        // public methods
        /// <summary>
        /// Returns a new TransactionOptions with some values changed.
        /// </summary>
        /// <param name="readConcern">The new read concern.</param>
        /// <param name="writeConcern">The new write concern.</param>
        /// <returns>The new TransactionOptions.</returns>
        public TransactionOptions With(
            Optional<ReadConcern> readConcern = default(Optional<ReadConcern>),
            Optional<WriteConcern> writeConcern = default(Optional<WriteConcern>))
        {
            return new TransactionOptions(
                readConcern: readConcern.WithDefault(_readConcern),
                writeConcern: writeConcern.WithDefault(_writeConcern));
        }
    }
}
