/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// Controls whether spaces and punctuation are considered base characters.
    /// </summary>
    public enum CollationAlternate
    {
        /// <summary>
        /// Spaces and punctuation are considered base characters (the default).
        /// </summary>
        NonIgnorable = 0,
        /// <summary>
        /// Spaces and characters are not considered base characters, and are only distinguised at strength > 3.
        /// </summary>
        Shifted
    }

    /// <summary>
    /// Uppercase or lowercase first.
    /// </summary>
    public enum CollationCaseFirst
    {
        /// <summary>
        /// Off (the default).
        /// </summary>
        Off = 0,
        /// <summary>
        /// Uppercase first.
        /// </summary>
        Upper,
        /// <summary>
        /// Lowercase first.
        /// </summary>
        Lower
    }

    /// <summary>
    /// Controls which characters are affected by alternate: "Shifted".
    /// </summary>
    public enum CollationMaxVariable
    {
        /// <summary>
        /// Punctuation and spaces are affected (the default).
        /// </summary>
        Punctuation = 0,
        /// <summary>
        /// Only spaces.
        /// </summary>
        Space
    }

    /// <summary>
    /// Prioritizes the comparison properties.
    /// </summary>
    public enum CollationStrength
    {
        /// <summary>
        /// Primary.
        /// </summary>
        Primary = 1,
        /// <summary>
        /// Secondary.
        /// </summary>
        Secondary = 2,
        /// <summary>
        /// Tertiary (the default).
        /// </summary>
        Tertiary = 3,
        /// <summary>
        /// Quaternary.
        /// </summary>
        Quaternary = 4,
        /// <summary>
        /// Identical.
        /// </summary>
        Identical = 5
    }

    /// <summary>
    /// Represents a MongoDB collation.
    /// </summary>
    public class Collation : IEquatable<Collation>, IConvertibleToBsonDocument
    {
        #region static
        // private static fields
        private static readonly Collation __simple = new Collation("simple");

        // public static properties
        /// <summary>
        /// Gets the simple binary compare collation.
        /// </summary>
        public static Collation Simple
        {
            get { return __simple; }
        }

        // public static methods
        /// <summary>
        /// Creates a Collation instance from a BsonDocument.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>A Collation instance.</returns>
        public static Collation FromBsonDocument(BsonDocument document)
        {
            var locale = document["locale"].AsString;

            bool? caseLevel = null;
            CollationCaseFirst? caseFirst = null;
            CollationStrength? strength = null;
            bool? numericOrdering = null;
            CollationAlternate? alternate = null;
            CollationMaxVariable? maxVariable = null;
            bool? normalization = null;
            bool? backwards = null;

            BsonValue value;
            if (document.TryGetValue("caseLevel", out value))
            {
                caseLevel = value.ToBoolean();
            }
            if (document.TryGetValue("caseFirst", out value))
            {
                caseFirst = ToCollationCaseFirst(value.AsString);
            }
            if (document.TryGetValue("strength", out value))
            {
                strength = ToCollationStrength(value.ToInt32());
            }
            if (document.TryGetValue("numericOrdering", out value))
            {
                numericOrdering = value.ToBoolean();
            }
            if (document.TryGetValue("alternate", out value))
            {
                alternate = ToCollationAlternate(value.AsString);
            }
            if (document.TryGetValue("maxVariable", out value))
            {
                maxVariable = ToCollationMaxVariable(value.AsString);
            }
            if (document.TryGetValue("normalization", out value))
            {
                normalization = value.ToBoolean();
            }
            if (document.TryGetValue("backwards", out value))
            {
                backwards = value.ToBoolean();
            }

            return new Collation(
                locale,
                caseLevel,
                caseFirst,
                strength,
                numericOrdering,
                alternate,
                maxVariable,
                normalization,
                backwards);
        }

        // private static methods
        private static CollationAlternate ToCollationAlternate(string value)
        {
            switch (value)
            {
                case "non-ignorable": return CollationAlternate.NonIgnorable;
                case "shifted": return CollationAlternate.Shifted;
                default: throw new ArgumentException($"Invalid CollationAlternate value: {value}.");
            }
        }

        private static CollationCaseFirst ToCollationCaseFirst(string value)
        {
            switch (value)
            {
                case "lower": return CollationCaseFirst.Lower;
                case "off": return CollationCaseFirst.Off;
                case "upper": return CollationCaseFirst.Upper;
                default: throw new ArgumentException($"Invalid CollationCaseFirst value: {value}.");
            }
        }

        private static CollationMaxVariable ToCollationMaxVariable(string value)
        {
            switch (value)
            {
                case "punct": return CollationMaxVariable.Punctuation;
                case "space": return CollationMaxVariable.Space;
                default: throw new ArgumentException($"Invalid CollationMaxVariable value: {value}.");
            }
        }

        private static CollationStrength ToCollationStrength(int value)
        {
            switch (value)
            {
                case 1: return CollationStrength.Primary;
                case 2: return CollationStrength.Secondary;
                case 3: return CollationStrength.Tertiary;
                case 4: return CollationStrength.Quaternary;
                case 5: return CollationStrength.Identical;
                default: throw new ArgumentException($"Invalid CollationStrength value: {value}.");
            }
        }
        #endregion

        // private fields
        private readonly CollationAlternate? _alternate;
        private readonly bool? _backwards;
        private readonly CollationCaseFirst? _caseFirst;
        private readonly bool? _caseLevel;
        private readonly string _locale;
        private readonly CollationMaxVariable? _maxVariable;
        private readonly bool? _normalization;
        private readonly bool? _numericOrdering;
        private readonly CollationStrength? _strength;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Collation" /> class.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="caseLevel">The case level.</param>
        /// <param name="caseFirst">The case that is ordered first.</param>
        /// <param name="strength">The strength.</param>
        /// <param name="numericOrdering">Whether numbers are ordered numerically.</param>
        /// <param name="alternate">The alternate.</param>
        /// <param name="maxVariable">The maximum variable.</param>
        /// <param name="normalization">The normalization.</param>
        /// <param name="backwards">Whether secondary differences are to be considered in reverse order.</param>
        public Collation(
            string locale,
            Optional<bool?> caseLevel = default(Optional<bool?>),
            Optional<CollationCaseFirst?> caseFirst = default(Optional<CollationCaseFirst?>),
            Optional<CollationStrength?> strength = default(Optional<CollationStrength?>),
            Optional<bool?> numericOrdering = default(Optional<bool?>),
            Optional<CollationAlternate?> alternate = default(Optional<CollationAlternate?>),
            Optional<CollationMaxVariable?> maxVariable = default(Optional<CollationMaxVariable?>),
            Optional<bool?> normalization = default(Optional<bool?>),
            Optional<bool?> backwards = default(Optional<bool?>))
        {
            _locale = Ensure.IsNotNull(locale, nameof(locale));
            _caseLevel = caseLevel.WithDefault(null);
            _caseFirst = caseFirst.WithDefault(null);
            _strength = strength.WithDefault(null);
            _numericOrdering = numericOrdering.WithDefault(null);
            _alternate = alternate.WithDefault(null);
            _maxVariable = maxVariable.WithDefault(null);
            _normalization = normalization.WithDefault(null);
            _backwards = backwards.WithDefault(null);
        }

        // public properties
        /// <summary>
        /// Gets whether spaces and punctuation are considered base characters.
        /// </summary>
        public CollationAlternate? Alternate
        {
            get { return _alternate; }
        }

        /// <summary>
        /// Gets whether secondary differencs are to be considered in reverse order.
        /// </summary>
        public bool? Backwards
        {
            get { return _backwards; }
        }

        /// <summary>
        /// Gets whether upper case or lower case is ordered first.
        /// </summary>
        public CollationCaseFirst? CaseFirst
        {
            get { return _caseFirst; }
        }

        /// <summary>
        /// Gets whether the collation is case sensitive at strength 1 and 2.
        /// </summary>
        public bool? CaseLevel
        {
            get { return _caseLevel; }
        }

        /// <summary>
        /// Gets the locale.
        /// </summary>
        public string Locale
        {
            get { return _locale; }
        }

        /// <summary>
        /// Gets which characters are affected by the alternate: "Shifted".
        /// </summary>
        public CollationMaxVariable? MaxVariable
        {
            get { return _maxVariable; }
        }

        /// <summary>
        /// Gets the normalization.
        /// </summary>
        public bool? Normalization
        {
            get { return _normalization; }
        }

        /// <summary>
        /// Gets whether numbers are ordered numerically.
        /// </summary>
        public bool? NumericOrdering
        {
            get { return _numericOrdering; }
        }

        /// <summary>
        /// Gets the strength.
        /// </summary>
        public CollationStrength? Strength
        {
            get { return _strength; }
        }

        // public methods
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Collation other)
        {
            if (object.ReferenceEquals(other, null) || other.GetType() != typeof(Collation))
            {
                return false;
            }

            return
                _alternate == other._alternate &&
                _backwards == other._backwards &&
                _caseFirst == other._caseFirst &&
                _caseLevel == other._caseLevel &&
                _locale == other._locale &&
                _maxVariable == other._maxVariable &&
                _numericOrdering == other._numericOrdering &&
                _strength == other._strength;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_alternate)
                .Hash(_backwards)
                .Hash(_caseFirst)
                .Hash(_caseLevel)
                .Hash(_locale)
                .Hash(_maxVariable)
                .Hash(_numericOrdering)
                .Hash(_strength)
                .GetHashCode();
        }

        /// <inheritdoc/>
        public BsonDocument ToBsonDocument()
        {
            return new BsonDocument
            {
                { "locale", _locale },
                { "caseLevel", () => _caseLevel.Value, _caseLevel != null },
                { "caseFirst", () => ToString(_caseFirst.Value), _caseFirst != null },
                { "strength", () => ToString(_strength.Value), _strength != null },
                { "numericOrdering", () => _numericOrdering.Value, _numericOrdering != null },
                { "alternate", () => ToString(_alternate.Value), _alternate != null },
                { "maxVariable", () => ToString(_maxVariable.Value), _maxVariable != null },
                { "normalization", () => _normalization.Value, _normalization != null },
                { "backwards", () => _backwards.Value, _backwards != null }
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToBsonDocument().ToJson();
        }

        /// <summary>
        /// Creates a new Collation instance with some properties changed.
        /// </summary>
        /// <param name="locale">The new locale.</param>
        /// <param name="caseLevel">The new case level.</param>
        /// <param name="caseFirst">The new case first.</param>
        /// <param name="strength">The new strength.</param>
        /// <param name="numericOrdering">The new numeric ordering.</param>
        /// <param name="alternate">The new alternate.</param>
        /// <param name="maxVariable">The new maximum variable.</param>
        /// <param name="normalization">The new normalization.</param>
        /// <param name="backwards">The new backwards.</param>
        /// <returns></returns>
        public Collation With(
            Optional<string> locale = default(Optional<string>),
            Optional<bool?> caseLevel = default(Optional<bool?>),
            Optional<CollationCaseFirst?> caseFirst = default(Optional<CollationCaseFirst?>),
            Optional<CollationStrength?> strength = default(Optional<CollationStrength?>),
            Optional<bool?> numericOrdering = default(Optional<bool?>),
            Optional<CollationAlternate?> alternate = default(Optional<CollationAlternate?>),
            Optional<CollationMaxVariable?> maxVariable = default(Optional<CollationMaxVariable?>),
            Optional<bool?> normalization = default(Optional<bool?>),
            Optional<bool?> backwards = default(Optional<bool?>))
        {
            return new Collation(
                locale.WithDefault(_locale),
                caseLevel.WithDefault(_caseLevel),
                caseFirst.WithDefault(_caseFirst),
                strength.WithDefault(_strength),
                numericOrdering.WithDefault(_numericOrdering),
                alternate.WithDefault(_alternate),
                maxVariable.WithDefault(_maxVariable),
                normalization.WithDefault(_normalization),
                backwards.WithDefault(_backwards));
        }

        // private methods
        private string ToString(CollationAlternate alternate)
        {
            switch (alternate)
            {
                case CollationAlternate.NonIgnorable: return "non-ignorable";
                case CollationAlternate.Shifted: return "shifted";
                default: throw new ArgumentException($"Invalid alternate: {alternate}.", nameof(alternate));
            }
        }

        private string ToString(CollationMaxVariable maxVariable)
        {
            switch (maxVariable)
            {
                case CollationMaxVariable.Punctuation: return "punct";
                case CollationMaxVariable.Space: return "space";
                default: throw new ArgumentException($"Invalid maxVariable: {maxVariable}.", nameof(maxVariable));
            }
        }

        private string ToString(CollationCaseFirst caseFirst)
        {
            switch (caseFirst)
            {
                case CollationCaseFirst.Lower: return "lower";
                case CollationCaseFirst.Off: return "off";
                case CollationCaseFirst.Upper: return "upper";
                default: throw new ArgumentException($"Invalid caseFirst: {caseFirst}.", nameof(caseFirst));
            }
        }

        private int ToString(CollationStrength strength)
        {
            switch (strength)
            {
                case CollationStrength.Primary: return 1;
                case CollationStrength.Secondary: return 2;
                case CollationStrength.Tertiary: return 3;
                case CollationStrength.Quaternary: return 4;
                case CollationStrength.Identical: return 5;
                default: throw new ArgumentException($"Invalid strength: {strength}.", nameof(strength));
            }
        }
    }
}
