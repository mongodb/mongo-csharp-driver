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

using System;
using System.Globalization;

namespace MongoDB.Bson.IO;

/// <summary>
/// Provides utility methods for working with <see cref="IBsonReader"/> instances in binary BSON format.
/// </summary>
public static class BsonBinaryReaderUtils
{
    /// <summary>
    /// Creates an instance of <see cref="IBsonReader"/> for the given byte buffer and reader settings.
    /// The result <see cref="IBsonReader"/> instance does not own the byte buffer, and will not Dispose it.
    /// For continuous single chunk buffers an optimized implementation of <see cref="IBsonReader"/> is created.
    /// </summary>
    /// <param name="byteBuffer">The byte buffer containing BSON data.</param>
    /// <param name="settings">The settings to configure the BSON reader.</param>
    /// <returns>An <see cref="IBsonReader"/>Bson reader.</returns>
    public static IBsonReader CreateBinaryReader(IByteBuffer byteBuffer, BsonBinaryReaderSettings settings)
    {
        if (byteBuffer is ReadOnlyMemoryBuffer readOnlyMemoryBuffer)
        {
            return new ReadOnlyMemoryBsonReader(readOnlyMemoryBuffer.Memory, new ReadOnlyMemoryReaderSettings(settings));
        }

        var backingBytes = byteBuffer.AccessBackingBytes(0);
        if (backingBytes.Count == byteBuffer.Length)
        {
            return new ReadOnlyMemoryBsonReader(backingBytes, new ByteBufferSlicer(byteBuffer), new ReadOnlyMemoryReaderSettings(settings));
        }

        var stream = new ByteBufferStream(byteBuffer, ownsBuffer: false);
        return new BsonBinaryReader(stream, settings);
    }

    internal static string GenerateDottedElementName(BsonBinaryReaderContext context, BsonBinaryReaderContext[] parentContexts, Func<string> elementNameReader)
    {
        string elementName;
        if (context.ContextType == ContextType.Document)
        {
            try
            {
                elementName = elementNameReader();
            }
            catch
            {
                elementName = "?"; // ignore exception
            }
        }
        else if (context.ContextType == ContextType.Array)
        {
            elementName = context.ArrayIndex.ToString(NumberFormatInfo.InvariantInfo);
        }
        else
        {
            elementName = "?";
        }

        return GenerateDottedElementName(parentContexts, 0, elementName);
    }

    private static string GenerateDottedElementName(BsonBinaryReaderContext[] contexts, int currentContextIndex, string elementName)
    {
        if (currentContextIndex >= contexts.Length)
            return elementName;

        var context = contexts[currentContextIndex];
        var nextIndex = currentContextIndex + 1;

        if (context.ContextType == ContextType.Document)
        {
            return GenerateDottedElementName(contexts,  nextIndex, (context.ElementName ?? "?") + "." + elementName);
        }

        if (context.ContextType == ContextType.Array)
        {
            var indexElementName = context.ArrayIndex.ToString(NumberFormatInfo.InvariantInfo);
            return GenerateDottedElementName(contexts,  nextIndex, indexElementName + "." + elementName);
        }

        if (nextIndex < contexts.Length)
        {
            return GenerateDottedElementName(contexts, nextIndex, "?." + elementName);
        }

        return elementName;
    }
}
