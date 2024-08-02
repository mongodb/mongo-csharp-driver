/* Copyright 2020-present MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a create index commit quorum.
    /// </summary>
    public abstract class CreateIndexCommitQuorum
    {
        #region static
        // private static fields
        private static readonly CreateIndexCommitQuorum __majority = new CreateIndexCommitQuorumWithMode("majority");
        private static readonly CreateIndexCommitQuorum __votingMembers = new CreateIndexCommitQuorumWithMode("votingMembers");

        // public static properties
        /// <summary>
        /// Gets a create index commit quorum of majority.
        /// </summary>
        public static CreateIndexCommitQuorum Majority => __majority;

        /// <summary>
        /// Gets a create index commit quorum of voting members.
        /// </summary>
        public static CreateIndexCommitQuorum VotingMembers => __votingMembers;

        // public static methods
        /// <summary>
        /// Creates a create index commit quorum with a mode value.
        /// </summary>
        /// <param name="mode">The mode value.</param>
        /// <returns>A create index commit quorum.</returns>
        public static CreateIndexCommitQuorum Create(string mode) => new CreateIndexCommitQuorumWithMode(mode);

        /// <summary>
        /// Creates a create index commit quorum with a w value.
        /// </summary>
        /// <param name="w">The w value.</param>
        /// <returns>A create index commit quorum.</returns>
        public static CreateIndexCommitQuorum Create(int w) => new CreateIndexCommitQuorumWithW(w);
        #endregion

        // public methods
        /// <summary>
        /// Converts the create index commit quorum to a BsonValue.
        /// </summary>
        /// <returns>A BsonValue.</returns>
        public abstract BsonValue ToBsonValue();
    }

    /// <summary>
    /// Represents a CreateIndexCommitQuorum with a mode value.
    /// </summary>
    public sealed class CreateIndexCommitQuorumWithMode : CreateIndexCommitQuorum
    {
        // private fields
        private readonly string _mode;

        // constructors
        /// <summary>
        /// Initializes an instance of CreateIndexCommitQuorumWithMode.
        /// </summary>
        /// <param name="mode">The mode value.</param>
        public CreateIndexCommitQuorumWithMode(string mode)
        {
            _mode = Ensure.IsNotNullOrEmpty(mode, nameof(mode));
        }

        // public properties
        /// <summary>
        /// The mode value.
        /// </summary>
        public string Mode => _mode;

        // public methods
        /// <inheritdoc/>
        public override BsonValue ToBsonValue()
        {
            return new BsonString(_mode);
        }
    }

    /// <summary>
    /// Represents a CreateIndexCommitQuorum with a w value.
    /// </summary>
    public sealed class CreateIndexCommitQuorumWithW : CreateIndexCommitQuorum
    {
        // private fields
        private readonly int _w;

        // constructors
        /// <summary>
        /// Initializes an instance of CreateIndexCommitQuorumWithW.
        /// </summary>
        /// <param name="w">The w value.</param>
        public CreateIndexCommitQuorumWithW(int w)
        {
            _w = Ensure.IsGreaterThanOrEqualToZero(w, nameof(w));
        }

        // public properties
        /// <summary>
        /// The w value.
        /// </summary>
        public int W => _w;

        // public methods
        /// <inheritdoc/>
        public override BsonValue ToBsonValue()
        {
            return BsonInt32.Create(_w);
        }
    }
}
