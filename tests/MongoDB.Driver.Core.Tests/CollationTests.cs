/* Copyright 2015-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    public class CollationTests
    {
        [Fact]
        public void Simple_should_return_expected_result()
        {
            var result = Collation.Simple;

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("simple");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Fact]
        public void FromBsonDocument_should_return_expected_result()
        {
            var document = BsonDocument.Parse("{ locale : 'en_US' }");

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_alternate_element_is_present(
            [Values("non-ignorable", "shifted")]
            string alternateString)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "alternate", alternateString }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().Be(Collation.ToCollationAlternate(alternateString));
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_backwards_element_is_present(
            [Values(false, true)]
            bool backwards)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "backwards", backwards }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().Be(backwards);
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_caseFirst_element_is_present(
            [Values("lower", "off", "upper")]
            string caseFirstString)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "caseFirst", caseFirstString }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().Be(Collation.ToCollationCaseFirst(caseFirstString));
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_caseLevel_element_is_present(
            [Values(false, true)]
            bool caseLevel)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "caseLevel", caseLevel }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().Be(caseLevel);
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_maxVariable_element_is_present(
            [Values("punct", "space")]
            string maxVariableString)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "maxVariable", maxVariableString }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().Be(Collation.ToCollationMaxVariable(maxVariableString));
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_normalization_element_is_present(
            [Values(false, true)]
            bool normalization)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "normalization", normalization }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().Be(normalization);
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_numericOrdering_element_is_present(
            [Values(false, true)]
            bool numericOrdering)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "numericOrdering", numericOrdering }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().Be(numericOrdering);
            result.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void FromBsonDocument_should_return_expected_result_when_strength_element_is_present(
            [Values(1, 2, 3, 4, 5)]
            int strengthInteger)
        {
            var document = new BsonDocument
            {
                { "locale", "en_US" },
                { "strength", strengthInteger }
            };

            var result = Collation.FromBsonDocument(document);

            result.Alternate.Should().BeNull();
            result.Backwards.Should().NotHaveValue();
            result.CaseFirst.Should().BeNull();
            result.CaseLevel.Should().NotHaveValue();
            result.Locale.Should().Be("en_US");
            result.MaxVariable.Should().BeNull();
            result.Normalization.Should().NotHaveValue();
            result.NumericOrdering.Should().NotHaveValue();
            result.Strength.Should().Be(Collation.ToCollationStrength(strengthInteger));
        }

        [Fact]
        public void FromBsonDocument_should_throw_when_invalid_element_is_present()
        {
            var document = BsonDocument.Parse("{ invalid : 1 }");

            var exception = Record.Exception(() => Collation.FromBsonDocument(document));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void FromBsonDocument_should_throw_when_locale_element_is_missing()
        {
            var document = BsonDocument.Parse("{ }");

            var exception = Record.Exception(() => Collation.FromBsonDocument(document));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData("non-ignorable", CollationAlternate.NonIgnorable)]
        [InlineData("shifted", CollationAlternate.Shifted)]
        public void ToCollationAlternate_should_return_expected_result(string value, CollationAlternate expectedResult)
        {
            var result = Collation.ToCollationAlternate(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("lower", CollationCaseFirst.Lower)]
        [InlineData("off", CollationCaseFirst.Off)]
        [InlineData("upper", CollationCaseFirst.Upper)]
        public void ToCollationCaseFirst_should_return_expected_result(string value, CollationCaseFirst expectedResult)
        {
            var result = Collation.ToCollationCaseFirst(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("punct", CollationMaxVariable.Punctuation)]
        [InlineData("space", CollationMaxVariable.Space)]
        public void ToCollationMaxVariable_should_return_expected_result(string value, CollationMaxVariable expectedResult)
        {
            var result = Collation.ToCollationMaxVariable(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1, CollationStrength.Primary)]
        [InlineData(2, CollationStrength.Secondary)]
        [InlineData(3, CollationStrength.Tertiary)]
        [InlineData(4, CollationStrength.Quaternary)]
        [InlineData(5, CollationStrength.Identical)]
        public void ToCollationStrength_should_return_expected_result(int value, CollationStrength expectedResult)
        {
            var result = Collation.ToCollationStrength(value);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(CollationStrength.Primary, 1)]
        [InlineData(CollationStrength.Secondary, 2)]
        [InlineData(CollationStrength.Tertiary, 3)]
        [InlineData(CollationStrength.Quaternary, 4)]
        [InlineData(CollationStrength.Identical, 5)]
        public void ToInt32_with_maxVariable_should_return_expected_result(CollationStrength strength, int expectedResult)
        {
            var result = Collation.ToInt32(strength);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(CollationAlternate.NonIgnorable, "non-ignorable")]
        [InlineData(CollationAlternate.Shifted, "shifted")]
        public void ToString_with_alternate_should_return_expected_result(CollationAlternate alternate, string expectedResult)
        {
            var result = Collation.ToString(alternate);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(CollationCaseFirst.Lower, "lower")]
        [InlineData(CollationCaseFirst.Off, "off")]
        [InlineData(CollationCaseFirst.Upper, "upper")]
        public void ToString_with_caseFirst_should_return_expected_result(CollationCaseFirst caseFirst, string expectedResult)
        {
            var result = Collation.ToString(caseFirst);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(CollationMaxVariable.Punctuation, "punct")]
        [InlineData(CollationMaxVariable.Space, "space")]
        public void ToString_with_maxVariable_should_return_expected_result(CollationMaxVariable maxVariable, string expectedResult)
        {
            var result = Collation.ToString(maxVariable);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale)
        {
            var subject = new Collation(locale);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_alternate_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? alternate)
        {
            var subject = new Collation(locale, alternate: alternate);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().Be(alternate);
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_backwards_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? backwards)
        {
            var subject = new Collation(locale, backwards: backwards);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().Be(backwards);
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_caseFirst_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Off, CollationCaseFirst.Upper)]
            CollationCaseFirst? caseFirst)
        {
            var subject = new Collation(locale, caseFirst: caseFirst);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().Be(caseFirst);
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_caseLevel_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? caseLevel)
        {
            var subject = new Collation(locale, caseLevel: caseLevel);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().Be(caseLevel);
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_maxVariable_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? maxVariable)
        {
            var subject = new Collation(locale, maxVariable: maxVariable);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().Be(maxVariable);
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_normalization_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? normalization)
        {
            var subject = new Collation(locale, normalization: normalization);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().Be(normalization);
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_numericOrdering_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? numericOrdering)
        {
            var subject = new Collation(locale, numericOrdering: numericOrdering);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().Be(numericOrdering);
            subject.Strength.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_strength_should_initialize_instance(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationStrength.Primary, CollationStrength.Tertiary, CollationStrength.Identical)]
            CollationStrength? strength)
        {
            var subject = new Collation(locale, strength: strength);

            subject.Locale.Should().BeSameAs(locale);

            subject.Alternate.Should().BeNull();
            subject.Backwards.Should().NotHaveValue();
            subject.CaseFirst.Should().BeNull();
            subject.CaseLevel.Should().NotHaveValue();
            subject.MaxVariable.Should().BeNull();
            subject.Normalization.Should().NotHaveValue();
            subject.NumericOrdering.Should().NotHaveValue();
            subject.Strength.Should().Be(strength);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_alternate_fields(
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? lhsAlternate,
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? rhsAlternate)
        {
            var lhs = new Collation("en_US", alternate: lhsAlternate);
            var rhs = new Collation("en_US", alternate: rhsAlternate);

            var result1 = lhs.Equals(rhs);
            var result2 = lhs.Equals((object)rhs);
            var lhsHashCode = lhs.GetHashCode();
            var rhsHashCode = rhs.GetHashCode();

            var expectedResult = lhsAlternate == rhsAlternate;
            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
            (lhsHashCode == rhsHashCode).Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_backwards_fields(
            [Values(null, false, true)]
            bool? lhsBackwards,
            [Values(null, false, true)]
            bool? rhsBackwards)
        {
            var lhs = new Collation("en_US", backwards: lhsBackwards);
            var rhs = new Collation("en_US", backwards: rhsBackwards);

            Equals_Act_and_Assert(lhs, rhs, lhsBackwards == rhsBackwards);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_caseFirst_fields(
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Upper)]
            CollationCaseFirst? lhsCaseFirst,
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Upper)]
            CollationCaseFirst? rhsCaseFirst)
        {
            var lhs = new Collation("en_US", caseFirst: lhsCaseFirst);
            var rhs = new Collation("en_US", caseFirst: rhsCaseFirst);

            Equals_Act_and_Assert(lhs, rhs, lhsCaseFirst == rhsCaseFirst);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_caseLevel_fields(
            [Values(null, false, true)]
            bool? lhsCaseLevel,
            [Values(null, false, true)]
            bool? rhsCaseLevel)
        {
            var lhs = new Collation("en_US", caseLevel: lhsCaseLevel);
            var rhs = new Collation("en_US", caseLevel: rhsCaseLevel);

            Equals_Act_and_Assert(lhs, rhs, lhsCaseLevel == rhsCaseLevel);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_locale_fields(
            [Values("en_US", "fr_CA")]
            string lhsLocale,
            [Values("en_US", "fr_CA")]
            string rhsLocale)
        {
            var lhs = new Collation(lhsLocale);
            var rhs = new Collation(rhsLocale);

            Equals_Act_and_Assert(lhs, rhs, lhsLocale == rhsLocale);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_maxVariable_fields(
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? lhsMaxVariable,
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? rhsMaxVariable)
        {
            var lhs = new Collation("en_US", maxVariable: lhsMaxVariable);
            var rhs = new Collation("en_US", maxVariable: rhsMaxVariable);

            Equals_Act_and_Assert(lhs, rhs, lhsMaxVariable == rhsMaxVariable);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_normalization_fields(
            [Values(null, false, true)]
            bool? lhsNormalization,
            [Values(null, false, true)]
            bool? rhsNormalization)
        {
            var lhs = new Collation("en_US", normalization: lhsNormalization);
            var rhs = new Collation("en_US", normalization: rhsNormalization);

            Equals_Act_and_Assert(lhs, rhs, lhsNormalization == rhsNormalization);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_numericOrdering_fields(
            [Values(null, false, true)]
            bool? lhsNumericOrdering,
            [Values(null, false, true)]
            bool? rhsNumericOrdering)
        {
            var lhs = new Collation("en_US", numericOrdering: lhsNumericOrdering);
            var rhs = new Collation("en_US", numericOrdering: rhsNumericOrdering);

            Equals_Act_and_Assert(lhs, rhs, lhsNumericOrdering == rhsNumericOrdering);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_strenght_fields(
            [Values(null, CollationStrength.Primary, CollationStrength.Identical)]
            CollationStrength? lhsStrength,
            [Values(null, CollationStrength.Primary, CollationStrength.Identical)]
            CollationStrength? rhsStrength)
        {
            var lhs = new Collation("en_US", strength: lhsStrength);
            var rhs = new Collation("en_US", strength: rhsStrength);

            Equals_Act_and_Assert(lhs, rhs, lhsStrength == rhsStrength);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result(
            [Values("en_US", "fr_CA")]
            string locale)
        {
            var subject = new Collation(locale);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument("locale", locale);
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_alternate_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? alternate)
        {
            var subject = new Collation(locale, alternate: alternate);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "alternate", () => Collation.ToString(alternate.Value), alternate.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_backwards_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? backwards)
        {
            var subject = new Collation(locale, backwards: backwards);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "backwards", () => backwards.Value, backwards.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_caseFirst_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Upper)]
            CollationCaseFirst? caseFirst)
        {
            var subject = new Collation(locale, caseFirst: caseFirst);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "caseFirst", () => Collation.ToString(caseFirst.Value), caseFirst.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_caseLevel_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? caseLevel)
        {
            var subject = new Collation(locale, caseLevel: caseLevel);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "caseLevel", () => caseLevel.Value, caseLevel.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_maxVariable_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? maxVariable)
        {
            var subject = new Collation(locale, maxVariable: maxVariable);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "maxVariable", () => Collation.ToString(maxVariable.Value), maxVariable.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_normalization_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? normalization)
        {
            var subject = new Collation(locale, normalization: normalization);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "normalization", () => normalization.Value, normalization.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_numericOrdering_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, false, true)]
            bool? numericOrdering)
        {
            var subject = new Collation(locale, numericOrdering: numericOrdering);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "numericOrdering", () => numericOrdering.Value, numericOrdering.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonDocument_should_return_expected_result_when_strength_is_set(
            [Values("en_US", "fr_CA")]
            string locale,
            [Values(null, CollationStrength.Primary, CollationStrength.Identical)]
            CollationStrength? strength)
        {
            var subject = new Collation(locale, strength: strength);

            var result = subject.ToBsonDocument();
            var json = subject.ToString();

            var expectedResult = new BsonDocument
            {
                { "locale", locale },
                { "strength", () => Collation.ToInt32(strength.Value), strength.HasValue }
            };
            result.Should().Be(expectedResult);
            json.Should().Be(expectedResult.ToJson());
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result(
            [Values("en_US", "fr_CA")]
            string locale)
        {
            var subject = new Collation(locale);

            var result = subject.With();

            result.Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_alternate_is_set(
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? originalAlternate,
            [Values(null, CollationAlternate.NonIgnorable, CollationAlternate.Shifted)]
            CollationAlternate? alternate)
        {
            var subject = new Collation("en_US", alternate: originalAlternate);

            var result = subject.With(alternate: alternate);

            result.Alternate.Should().Be(alternate);
            result.With(alternate: originalAlternate).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_backwards_is_set(
            [Values(null, false, true)]
            bool? originalBackwards,
            [Values(null, false, true)]
            bool? backwards)
        {
            var subject = new Collation("en_US", backwards: originalBackwards);

            var result = subject.With(backwards: backwards);

            result.Backwards.Should().Be(backwards);
            result.With(backwards: originalBackwards).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_caseFirst_is_set(
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Upper)]
            CollationCaseFirst? originalCaseFirst,
            [Values(null, CollationCaseFirst.Lower, CollationCaseFirst.Upper)]
            CollationCaseFirst? caseFirst)
        {
            var subject = new Collation("en_US", caseFirst: originalCaseFirst);

            var result = subject.With(caseFirst: caseFirst);

            result.CaseFirst.Should().Be(caseFirst);
            result.With(caseFirst: originalCaseFirst).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_caseLevel_is_set(
            [Values(null, false, true)]
            bool? originalCaseLevel,
            [Values(null, false, true)]
            bool? caseLevel)
        {
            var subject = new Collation("en_US", caseLevel: originalCaseLevel);

            var result = subject.With(caseLevel: caseLevel);

            result.CaseLevel.Should().Be(caseLevel);
            result.With(caseLevel: originalCaseLevel).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_locale_is_set(
            [Values("en_US", "fr_CA")]
            string originalLocale,
            [Values("en_US", "fr_CA")]
            string locale)
        {
            var subject = new Collation(originalLocale);

            var result = subject.With(locale: locale);

            result.Locale.Should().Be(locale);
            result.With(locale: originalLocale).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_maxVariable_is_set(
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? originalMaxVariable,
            [Values(null, CollationMaxVariable.Punctuation, CollationMaxVariable.Space)]
            CollationMaxVariable? maxVariable)
        {
            var subject = new Collation("en_US", maxVariable: originalMaxVariable);

            var result = subject.With(maxVariable: maxVariable);

            result.MaxVariable.Should().Be(maxVariable);
            result.With(maxVariable: originalMaxVariable).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_normalization_is_set(
            [Values(null, false, true)]
            bool? originalNormalization,
            [Values(null, false, true)]
            bool? normalization)
        {
            var subject = new Collation("en_US", normalization: originalNormalization);

            var result = subject.With(normalization: normalization);

            result.Normalization.Should().Be(normalization);
            result.With(normalization: originalNormalization).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_numericOrdering_is_set(
            [Values(null, false, true)]
            bool? originalNumericOrdering,
            [Values(null, false, true)]
            bool? numericOrdering)
        {
            var subject = new Collation("en_US", numericOrdering: originalNumericOrdering);

            var result = subject.With(numericOrdering: numericOrdering);

            result.NumericOrdering.Should().Be(numericOrdering);
            result.With(numericOrdering: originalNumericOrdering).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_expected_result_when_strength_is_set(
            [Values(null, CollationStrength.Primary, CollationStrength.Identical)]
            CollationStrength? originalStrength,
            [Values(null, CollationStrength.Primary, CollationStrength.Identical)]
            CollationStrength? strength)
        {
            var subject = new Collation("en_US", strength: originalStrength);

            var result = subject.With(strength: strength);

            result.Strength.Should().Be(strength);
            result.With(strength: originalStrength).Should().Be(subject);
        }

        // helper methods
        private void Equals_Act_and_Assert(Collation lhs, Collation rhs, bool expectedResult)
        {
            var result1 = lhs.Equals(rhs);
            var result2 = lhs.Equals((object)rhs);
            var lhsHashCode = lhs.GetHashCode();
            var rhsHashCode = rhs.GetHashCode();

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
            (lhsHashCode == rhsHashCode).Should().Be(expectedResult);
        }
    }
}
