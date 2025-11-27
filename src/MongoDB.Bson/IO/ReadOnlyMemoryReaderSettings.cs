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

namespace MongoDB.Bson.IO;

/// <summary>
/// Represents settings for a <see cref="ReadOnlyMemoryBsonReader"/>.
/// </summary>
public sealed class ReadOnlyMemoryReaderSettings : BsonBinaryReaderSettings
{
    // constructors
    /// <summary>
    /// Initializes a new instance of the ReadOnlyMemoryReaderSettings class.
    /// </summary>
    public ReadOnlyMemoryReaderSettings()
    {
    }

    internal ReadOnlyMemoryReaderSettings(BsonBinaryReaderSettings readerSettings)
    {
        Encoding = readerSettings.Encoding;
        FixOldBinarySubTypeOnInput = readerSettings.FixOldBinarySubTypeOnInput;
        FixOldDateTimeMaxValueOnInput = readerSettings.FixOldDateTimeMaxValueOnInput;
        MaxDocumentSize = readerSettings.MaxDocumentSize;
    }

    // public static properties
    /// <summary>
    /// Gets the default settings for a <see cref="ReadOnlyMemoryBsonReader"/>
    /// </summary>
    public static new ReadOnlyMemoryReaderSettings Defaults { get; } = new();

    // public methods
    /// <summary>
    /// Creates a clone of the settings.
    /// </summary>
    /// <returns>A clone of the settings.</returns>
    public new ReadOnlyMemoryReaderSettings Clone() => (ReadOnlyMemoryReaderSettings)CloneImplementation();

    // protected methods
    /// <summary>
    /// Creates a clone of the settings.
    /// </summary>
    /// <returns>A clone of the settings.</returns>
    protected override BsonReaderSettings CloneImplementation() =>
        new ReadOnlyMemoryReaderSettings
        {
            Encoding = Encoding,
            FixOldBinarySubTypeOnInput = FixOldBinarySubTypeOnInput,
            FixOldDateTimeMaxValueOnInput = FixOldDateTimeMaxValueOnInput,
            MaxDocumentSize = MaxDocumentSize
        };
}
