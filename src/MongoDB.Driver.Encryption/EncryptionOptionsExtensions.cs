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
        CreateStringOptionsDocument(stringOptions.CaseSensitive, stringOptions.DiacriticSensitive, stringOptions.PrefixOptions, stringOptions.SubstringOptions, stringOptions.SuffixOptions);

#pragma warning disable CS0618 // TextOptions is the deprecated alias for StringOptions.
    public static BsonDocument CreateDocument(this TextOptions textOptions) =>
        CreateStringOptionsDocument(textOptions.CaseSensitive, textOptions.DiacriticSensitive, textOptions.PrefixOptions, textOptions.SubstringOptions, textOptions.SuffixOptions);
#pragma warning restore CS0618

    private static BsonDocument CreateStringOptionsDocument(
        bool caseSensitive,
        bool diacriticSensitive,
        PrefixOptions prefixOptions,
        SubstringOptions substringOptions,
        SuffixOptions suffixOptions) =>
        new()
        {
            { "caseSensitive", caseSensitive },
            { "diacriticSensitive", diacriticSensitive },
            {
                "prefix", () => new BsonDocument
                {
                    { "strMaxQueryLength", prefixOptions.StrMaxQueryLength },
                    { "strMinQueryLength", prefixOptions.StrMinQueryLength }
                },
                prefixOptions != null
            },
            {
                "substring", () => new BsonDocument
                {
                    { "strMaxLength", substringOptions.StrMaxLength },
                    { "strMaxQueryLength", substringOptions.StrMaxQueryLength },
                    { "strMinQueryLength", substringOptions.StrMinQueryLength }
                },
                substringOptions != null
            },
            {
                "suffix", () => new BsonDocument
                {
                    { "strMaxQueryLength", suffixOptions.StrMaxQueryLength },
                    { "strMinQueryLength", suffixOptions.StrMinQueryLength }
                },
                suffixOptions != null
            }
        };
}