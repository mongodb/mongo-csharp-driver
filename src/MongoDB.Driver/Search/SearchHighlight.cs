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
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Represents a result of highlighting.
    /// </summary>
    public sealed class SearchHighlight
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHighlight"/> class.
        /// </summary>
        /// <param name="path">document field which returned a match.</param>
        /// <param name="score">Score assigned to this result.</param>
        /// <param name="texts">Objects containing the matching text and the surrounding text.</param>
        public SearchHighlight(string path, double score, SearchHighlightText[] texts)
        {
            Path = path;
            Score = score;
            Texts = texts;
        }

        /// <summary>
        /// Gets the document field which returned a match.
        /// </summary>
        [BsonElement("path")]
        public string Path { get; }

        /// <summary>
        /// Gets the score assigned to this result.
        /// </summary>
        [BsonElement("score")]
        public double Score { get; }

        /// <summary>
        /// Gets one or more objects containing the matching text and the surrounding text
        /// (if any).
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("texts")]
        public SearchHighlightText[] Texts { get; }
    }

    /// <summary>
    /// Represents the matching text or the surrounding text of a highlighting result.
    /// </summary>
    public sealed class SearchHighlightText
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHighlightText"/> class.
        /// </summary>
        /// <param name="type">Type of search highlight.</param>
        /// <param name="value">Text from the field which returned a match.</param>
        public SearchHighlightText(HighlightTextType type, string value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the type of text, matching or surrounding.
        /// </summary>
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public HighlightTextType Type { get; }

        /// <summary>
        /// Gets the text from the field which returned a match.
        /// </summary>
        [BsonElement("value")]
        public string Value { get; }
    }

    /// <summary>
    /// Represents the type of text in a highlighting result, matching or surrounding.
    /// </summary>
    public enum HighlightTextType
    {
        /// <summary>
        /// Indicates that the text contains a match.
        /// </summary>
        Hit,

        /// <summary>
        /// Indicates that the text contains the text content adjacent to a matching string.
        /// </summary>
        Text
    }
}
