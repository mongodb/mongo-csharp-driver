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

namespace MongoDB.Bson.Serialization.Options
{
    /// <summary>
    /// Represents the format to use with a DateOnly serializer when the representation is BsonType.Document.
    /// </summary>
    public enum DateOnlyDocumentFormat
    {
        /// <summary>
        /// The document will contain "DateTime" (BsonType.DateTime) and "Ticks" (BsonType.Int64).
        /// </summary>
        DateTimeTicks,

        /// <summary>
        /// The document will contain "Year", "Month" and "Day" (all BsonType.Int32).
        /// </summary>
        YearMonthDay
    }
}