/* Copyright 2010-present MongoDB Inc.
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
    /// Represents a boundary for a documents window in SetWindowFields.
    /// </summary>
    public abstract class DocumentsWindowBoundary
    {
        internal DocumentsWindowBoundary() { } // disallow user defined subclasses
        internal abstract BsonValue Render();
    }

    /// <summary>
    /// Represents a keyword boundary for a document window in SetWindowFields (i.e. "unbounded" or "current").
    /// </summary>
    public sealed class KeywordDocumentsWindowBoundary : DocumentsWindowBoundary
    {
        private readonly string _keyword;

        internal KeywordDocumentsWindowBoundary(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// The keyword.
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override string ToString() => $"\"{_keyword}\"";

        internal override BsonValue Render() => _keyword;
    }

    /// <summary>
    /// Represents a position boundary for a document window in SetWindowFields.
    /// </summary>
    public sealed class PositionDocumentsWindowBoundary : DocumentsWindowBoundary
    {
        private readonly int _position;

        /// <summary>
        /// Initializes a new instance of PositionDocumentsWindowBoundary.
        /// </summary>
        /// <param name="position">The position.</param>
        internal PositionDocumentsWindowBoundary(int position)
        {
            _position = position;
        }

        /// <summary>
        /// The position.
        /// </summary>
        public int Position => _position;

        /// <inheritdoc/>
        public override string ToString() => _position.ToString();

        internal override BsonValue Render() => _position;
    }
}
