/* Copyright 2020–present MongoDB Inc.
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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a topology description.
    /// </summary>
    /// <remarks>
    /// Comparing topology descriptions freshness does not exhibit the reversal property of
    /// inequalities e.g. IsStalerThan(a, b) (a "&lt;" b) does not imply !IsStalerThan(b, a) (b "&gt;" a)
    /// See <seealso cref="CompareTopologyVersion(TopologyVersion, TopologyVersion)"/> for more information.
    /// </remarks>
    public sealed class TopologyVersion : IEquatable<TopologyVersion>, IConvertibleToBsonDocument
    {
        #region static
        // public static methods
        /// <summary>
        /// Compares a local TopologyVersion with a server's TopologyVersion and indicates whether the local
        /// TopologyVersion is staler, fresher, or equal to the server's TopologyVersion.
        /// Per the SDAM specification, if the ProcessIds are not equal, this method assumes that
        /// <paramref name="y"/> is more recent. This means that this method does not exhibit
        /// the reversal properties of inequalities i.e. a "&lt;" b does not imply b "&gt;" a.
        /// </summary>
        /// <param name="x">The first TopologyVersion.</param>
        /// <param name="y">The other TopologyVersion.</param>
        /// <returns>
        /// Less than zero indicates that the <paramref name="x"/> is staler than the <paramref name="y"/>.
        /// Zero indicates that the <paramref name="x"/> description is equal to the <paramref name="y"/>.
        /// Greater than zero indicates that the <paramref name="x"/> is fresher than the <paramref name="y"/>.
        /// </returns>
        public static int CompareTopologyVersion(TopologyVersion x, TopologyVersion y)
        {
            if (x == null || y == null)
            {
                return -1;
            }

            if (x.ProcessId == y.ProcessId)
            {
                return x.Counter.CompareTo(y.Counter);
            }

            return -1;
        }

        /// <summary>
        /// Attempts to create a TopologyVersion from the supplied BsonDocument.
        /// </summary>
        /// <param name="document">The document. Should contain an ObjectId named "processId" and a BsonInt64 named "counter".</param>
        /// <returns>A TopologyVersion if one could be constructed from the supplied document and null otherwise.</returns>
        public static TopologyVersion FromBsonDocument(BsonDocument document)
        {
            if (document.TryGetValue("processId", out var processIdValue) && processIdValue is BsonObjectId processId &&
                document.TryGetValue("counter", out var counterValue) && counterValue is BsonInt64 counter)
            {
                return new TopologyVersion(processId.Value, counter.Value);
            }

            return null;
        }

        internal static TopologyVersion FromMongoCommandResponse(BsonDocument response)
        {
            if (response != null &&
                response.TryGetValue("topologyVersion", out var topologyVersionValue) && topologyVersionValue is BsonDocument topologyVersion)
            {
                return FromBsonDocument(topologyVersion);
            }

            return null;
        }

        internal static TopologyVersion FromMongoCommandException(MongoCommandException commandException)
        {
            return FromMongoCommandResponse(commandException.Result);
        }

        /// <summary>
        /// Gets whether or not <paramref name="x"/> is fresher than <paramref name="y"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsFresherThan(b) (a "&lt;" b) does not imply
        /// !b.IsFresherThan(a) (b "&gt;" a)
        /// See <seealso cref="CompareTopologyVersion(TopologyVersion, TopologyVersion)"/> for more information.
        /// In the case that this.Equals(<paramref name="y"/>), <paramref name="y"/> will be considered to be fresher.
        /// </summary>
        /// <param name="x">The first TopologyVersion.</param>
        /// <param name="y">The other TopologyVersion.</param>
        /// <returns>
        /// Whether or not this TopologyVersion is fresher than <paramref name="y"/>.
        /// </returns>
        public static bool IsFresherThan(TopologyVersion x, TopologyVersion y) => CompareTopologyVersion(x, y) > 0;

        /// <summary>
        /// Gets whether or not <paramref name="x"/> is fresher than or Equal to <paramref name="y"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsFresherThan(b) (a "&lt;" b) does not imply
        /// !b.IsFresherThan(a) (b "&gt;" a)
        /// See <seealso cref="CompareTopologyVersion(TopologyVersion, TopologyVersion)"/> for more information.
        /// In the case that this.Equals(<paramref name="y"/>), <paramref name="y"/> will be considered to be fresher.
        /// </summary>
        /// <param name="x">The first TopologyVersion.</param>
        /// <param name="y">The other TopologyVersion.</param>
        /// <returns>
        /// Whether or not this TopologyVersion is fresher than <paramref name="y"/>.
        /// </returns>
        public static bool IsFresherThanOrEqualTo(TopologyVersion x, TopologyVersion y) => CompareTopologyVersion(x, y) >= 0;

        /// <summary>
        /// Gets whether or not <paramref name="x"/> is staler than or Equal to <paramref name="y"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThan(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThan(a) (b "&gt;" a).
        /// See <seealso cref="CompareTopologyVersion(TopologyVersion, TopologyVersion)"/> for more information.
        /// In the case that this == <paramref name="y"/>, <paramref name="y"/> will be considered to be fresher.
        /// </summary>
        /// <param name="x">The first TopologyVersion.</param>
        /// <param name="y">The other TopologyVersion.</param>
        /// <returns>
        /// Whether or not this TopologyVersion is staler than <paramref name="y"/>.
        /// </returns>
        public static bool IsStalerThan(TopologyVersion x, TopologyVersion y) => CompareTopologyVersion(x, y) < 0;

        /// <summary>
        /// Gets whether or not <paramref name="x"/> is staler than or Equal to <paramref name="y"/>.
        /// Comparing topology descriptions freshness does not exhibit the reversal property of
        /// inequalities e.g. a.IsStalerThan(b) (a "&lt;" b) does not imply
        /// !b.IsStalerThan(a) (b "&gt;" a).
        /// See <seealso cref="CompareTopologyVersion(TopologyVersion, TopologyVersion)"/> for more information.
        /// In the case that this == <paramref name="y"/>, <paramref name="y"/> will be considered to be fresher.
        /// </summary>
        /// <param name="x">The first TopologyVersion.</param>
        /// <param name="y">The other TopologyVersion.</param>
        /// <returns>
        /// Whether or not this TopologyVersion is staler than <paramref name="y"/>.
        /// </returns>
        public static bool IsStalerThanOrEqualTo(TopologyVersion x, TopologyVersion y) => CompareTopologyVersion(x, y) <= 0;
        #endregion

        // private fields
        private readonly long _counter;
        private readonly int _hashCode;
        private readonly ObjectId _processId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TopologyVersion"/> class.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <param name="counter">The counter.</param>
        public TopologyVersion(ObjectId processId, long counter)
        {
            _processId = processId;
            _counter = counter;
            _hashCode = new Hasher().Hash(_processId).Hash(_counter).GetHashCode();
        }

        // public properties
        // properties
        /// <summary>
        /// Gets the process identifier.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        public long Counter => _counter;

        // properties
        /// <summary>
        /// Gets the process identifier.
        /// </summary>
        /// <value>
        /// The process identifier.
        /// </value>
        public ObjectId ProcessId => _processId;

        // public methods
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as TopologyVersion);
        }

        /// <inheritdoc />
        public bool Equals(TopologyVersion other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return _counter == other._counter && _processId == other._processId;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        /// <inheritdoc/>
        public BsonDocument ToBsonDocument() => new BsonDocument { { "processId", _processId }, { "counter", _counter } };

        /// <inheritdoc/>
        public override string ToString() => ToBsonDocument().ToJson();
    }
}
