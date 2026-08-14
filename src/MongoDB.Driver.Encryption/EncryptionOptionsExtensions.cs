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

namespace MongoDB.Driver.Encryption;

internal static class EncryptionOptionsExtensions
{
    public static BsonDocument CreateDocument(this RangeOptions rangeOptions) =>
        new()
        {
            { "min", rangeOptions.Min, rangeOptions.Min != null },
            { "max", rangeOptions.Max, rangeOptions.Max != null },
            { "precision", rangeOptions.Precision, rangeOptions.Precision != null },
            { "sparsity", rangeOptions.Sparsity, rangeOptions.Sparsity != null },
            { "trimFactor", rangeOptions.TrimFactor, rangeOptions.TrimFactor != null }
        };

    public static BsonDocument CreateDocument(this StringOptions stringOptions) =>
        new()
        {
            { "caseSensitive", stringOptions.CaseSensitive },
            { "diacriticSensitive", stringOptions.DiacriticSensitive },
            {
                "prefix", () => new BsonDocument
                {
                    { "strMaxQueryLength", stringOptions.PrefixOptions.StrMaxQueryLength },
                    { "strMinQueryLength", stringOptions.PrefixOptions.StrMinQueryLength }
                },
                stringOptions.PrefixOptions != null
            },
            {
                "substring", () => new BsonDocument
                {
                    { "strMaxLength", stringOptions.SubstringOptions.StrMaxLength },
                    { "strMaxQueryLength", stringOptions.SubstringOptions.StrMaxQueryLength },
                    { "strMinQueryLength", stringOptions.SubstringOptions.StrMinQueryLength }
                },
                stringOptions.SubstringOptions != null
            },
            {
                "suffix", () => new BsonDocument
                {
                    { "strMaxQueryLength", stringOptions.SuffixOptions.StrMaxQueryLength },
                    { "strMinQueryLength", stringOptions.SuffixOptions.StrMinQueryLength }
                },
                stringOptions.SuffixOptions != null
            }
        };
}
