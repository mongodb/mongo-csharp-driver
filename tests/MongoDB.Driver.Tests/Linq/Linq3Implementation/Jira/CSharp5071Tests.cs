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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5071Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData("intconstant+stringproperty", "wrong:{ $project : { __fld0 : { $add : [1, '$B'] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("intconstant+stringproperty", "{ $project : { _v : { $concat : ['1', '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty", "{ $project : { __fld0 : { $concat : ['X', '$B'] }, _id : 0 } }", "XB", LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty", "{ $project : { _v : { $concat : ['X', '$B'] }, _id : 0 } }", "XB", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant", "wrong:{ $project : { __fld0 : { $concat : ['$A', 2] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '2'] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant", "{ $project : { __fld0 : { $concat : ['$A', 'X'] }, _id : 0 } }", "AX", LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', 'X'] }, _id : 0 } }", "AX", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty", "{ $project : { __fld0 : { $concat : ['$A', '$B'] }, _id : 0 } }", "AB", LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B'] }, _id : 0 } }", "AB", LinqProvider.V3)]
        public void Add_with_two_terms_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intconstant+stringproperty" => collection.AsQueryable().Select(x => 1 + x.B),
                "intproperty+stringproperty" => collection.AsQueryable().Select(x => x.I + x.B),
                "stringconstant+stringproperty" => collection.AsQueryable().Select(x => "X" + x.B),
                "stringproperty+intconstant" => collection.AsQueryable().Select(x => x.A + 2),
                "stringproperty+intproperty" => collection.AsQueryable().Select(x => x.A + x.J),
                "stringproperty+stringconstant" => collection.AsQueryable().Select(x => x.A + "X"),
                "stringproperty+stringproperty" => collection.AsQueryable().Select(x => x.A + x.B),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("intconstant+stringproperty+intconstant", "wrong:{ $project : { __fld0 : { $concat : [{ $add : [1, '$B'] }, 3] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['1', '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['1', '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringconstant", "wrong:{ $project : { __fld0 : { $concat : [{ $add : [1, '$B'] }, 'Z'] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['1', '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringproperty", "wrong:{ $project : { __fld0 : { $concat : [{ $add : [1, '$B'] }, '$C'] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['1', '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intconstant", "wrong:{ $project : { __fld0 : { $concat : ['X', '$B', 3] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['X', '$B', '3'] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['X', '$B', { $toString : '$K' }] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringconstant", "{ $project : { __fld0 : { $concat : ['X', '$B', 'Z'] }, _id : 0 } }", "XBZ", LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['X', '$B', 'Z'] }, _id : 0 } }", "XBZ", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringproperty", "{ $project : { __fld0 : { $concat : ['X', '$B', '$C'] }, _id : 0 } }", "XBC", LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['X', '$B', '$C'] }, _id : 0 } }", "XBC", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant+stringproperty", "wrong:{ $project : { __fld0 : { $concat : ['$A', 2, '$C'] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("stringproperty+intconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', '2', '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }, '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant+stringproperty", "{ $project : { __fld0 : { $concat : ['$A', 'Y', '$C'] }, _id : 0 } }", "AYC", LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', 'Y', '$C'] }, _id : 0 } }", "AYC", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intconstant", "wrong:{ $project : { __fld0 : { $concat : ['$A', '$B', 3] }, _id : 0 } }", "throws:MongoCommandException", LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '$B', '3'] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', '$B', { $toString : '$K' }] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringconstant", "{ $project : { __fld0 : { $concat : ['$A', '$B', 'Z'] }, _id : 0 } }", "ABZ", LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', '$B', 'Z'] }, _id : 0 } }", "ABZ", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringproperty", "{ $project : { __fld0 : { $concat : ['$A', '$B', '$C'] }, _id : 0 } }", "ABC", LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B', '$C'] }, _id : 0 } }", "ABC", LinqProvider.V3)]
        public void Add_with_three_terms_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => 1 + x.B + 3),
                "intconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => 1 + x.B + x.K),
                "intconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => 1 + x.B + "Z"),
                "intconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => 1 + x.B + x.C),
                "intproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => x.I + x.B + 3),
                "intproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => x.I + x.B + x.K),
                "intproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => x.I + x.B + "Z"),
                "intproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => x.I + x.B + x.C),
                "stringconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => "X" + x.B + 3),
                "stringconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => "X" + x.B + x.K),
                "stringconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => "X" + x.B + "Z"),
                "stringconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => "X" + x.B + x.C),
                "stringproperty+intconstant+stringproperty" => collection.AsQueryable().Select(x => x.A + 2 + x.C),
                "stringproperty+intproperty+stringproperty" => collection.AsQueryable().Select(x => x.A + x.J + x.C),
                "stringproperty+stringconstant+stringproperty" => collection.AsQueryable().Select(x => x.A + "Y" + x.C),
                "stringproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => x.A + x.B + 3),
                "stringproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => x.A + x.B + x.K),
                "stringproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => x.A + x.B + "Z"),
                "stringproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => x.A + x.B + x.C),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("intproperty", "throws:ArgumentException", null, LinqProvider.V2)]
        [InlineData("intproperty", "{ $project : { _v : { $concat : { $toString : '$I' } }, _id : 0 } }", "1", LinqProvider.V3)]
        [InlineData("stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty", "{ $project : { _v : { $concat : '$A' }, _id : 0 } }", "A", LinqProvider.V3)]
        public void Concat_with_one_argument_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intproperty" => collection.AsQueryable().Select(x => string.Concat(x.I)),
                "stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A)),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("intconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty", "{ $project : { _v : { $concat : ['1', '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty", "throws:ArgumentException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty", "{ $project : { _v : { $concat : ['X', '$B'] }, _id : 0 } }", "XB", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '2'] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty", "throws:ArgumentException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', 'X'] }, _id : 0 } }", "AX", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B'] }, _id : 0 } }", "AB", LinqProvider.V3)]
        public void Concat_with_two_arguments_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(1, x.B)),
                "intproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.I, x.B)),
                "stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat("X", x.B)),
                "stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(x.A, 2)),
                "stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(x.A, x.J)),
                "stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(x.A, "X")),
                "stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A, x.B)),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("intconstant+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['1', '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['1', '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['1', '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['1', '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['X', '$B', '3'] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['X', '$B', { $toString : '$K' }] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['X', '$B', 'Z'] }, _id : 0 } }", "XBZ", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['X', '$B', '$C'] }, _id : 0 } }", "XBC", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', '2', '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }, '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', 'Y', '$C'] }, _id : 0 } }", "AYC", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '$B', '3'] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', '$B', { $toString : '$K' }] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', '$B', 'Z'] }, _id : 0 } }", "ABZ", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B', '$C'] }, _id : 0 } }", "ABC", LinqProvider.V3)]
        public void Concat_with_three_arguments_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(1 + x.B + 3)),
                "intconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(1 + x.B + x.K)),
                "intconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(1 + x.B + "Z")),
                "intconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(1 + x.B + x.C)),
                "intproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(x.I + x.B + 3)),
                "intproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(x.I + x.B + x.K)),
                "intproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(x.I + x.B + "Z")),
                "intproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.I + x.B + x.C)),
                "stringconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat("X" + x.B + 3)),
                "stringconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat("X" + x.B + x.K)),
                "stringconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat("X" + x.B + "Z")),
                "stringconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat("X" + x.B + x.C)),
                "stringproperty+intconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A + 2 + x.C)),
                "stringproperty+intproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A + x.J + x.C)),
                "stringproperty+stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A + "Y" + x.C)),
                "stringproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(x.A + x.B + 3)),
                "stringproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(x.A + x.B + x.K)),
                "stringproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(x.A + x.B + "Z")),
                "stringproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(x.A + x.B + x.C)),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty", "{ $project : { _v : { $concat : { $toString : '$I' } }, _id : 0 } }", "1", LinqProvider.V3)]
        [InlineData("stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty", "{ $project : { _v : { $concat : '$A' }, _id : 0 } }", "A", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty", "{ $project : { _v : { $concat : ['1', '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B'] }, _id : 0 } }", "1B", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty", "{ $project : { _v : { $concat : ['X', '$B'] }, _id : 0 } }", "XB", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '2'] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }] }, _id : 0 } }", "A2", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', 'X'] }, _id : 0 } }", "AX", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B'] }, _id : 0 } }", "AB", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['1', '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['1', '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['1', '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intconstant+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("intconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['1', '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '3'] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', { $toString : '$K' }] }, _id : 0 } }", "1B3", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringconstant", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', 'Z'] }, _id : 0 } }", "1BZ", LinqProvider.V3)]
        [InlineData("intproperty+stringproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("intproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : [{ $toString : '$I' }, '$B', '$C'] }, _id : 0 } }", "1BC", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intconstant", "{ $project : { _v : { $concat : ['X', '$B', '3'] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+intproperty", "{ $project : { _v : { $concat : ['X', '$B', { $toString : '$K' }] }, _id : 0 } }", "XB3", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['X', '$B', 'Z'] }, _id : 0 } }", "XBZ", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['X', '$B', '$C'] }, _id : 0 } }", "XBC", LinqProvider.V3)]
        [InlineData("stringproperty+intconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', '2', '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+intproperty+stringproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+intproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', { $toString : '$J' }, '$C'] }, _id : 0 } }", "A2C", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', 'Y', '$C'] }, _id : 0 } }", "AYC", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intconstant", "{ $project : { _v : { $concat : ['$A', '$B', '3'] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+intproperty", "throws:InvalidOperationException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+intproperty", "{ $project : { _v : { $concat : ['$A', '$B', { $toString : '$K' }] }, _id : 0 } }", "AB3", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', '$B', 'Z'] }, _id : 0 } }", "ABZ", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B', '$C'] }, _id : 0 } }", "ABC", LinqProvider.V3)]
        public void Concat_with_array_of_object_argument_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            if (expectedStage.Contains("$toString"))
            {
                RequireServer.Check().Supports(Feature.AggregateToString);
            }

            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I })),
                "stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A })),
                "intconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { 1, x.B })),
                "intproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I, x.B })),
                "stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { "X", x.B })),
                "stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A, 2 })),
                "stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A, x.J })),
                "stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A, "X" })),
                "stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A, x.B })),
                "intconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { 1 + x.B + 3 })),
                "intconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { 1 + x.B + x.K })),
                "intconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { 1 + x.B + "Z" })),
                "intconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { 1 + x.B + x.C })),
                "intproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I + x.B + 3 })),
                "intproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I + x.B + x.K })),
                "intproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I + x.B + "Z" })),
                "intproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.I + x.B + x.C })),
                "stringconstant+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { "X" + x.B + 3 })),
                "stringconstant+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { "X" + x.B + x.K })),
                "stringconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { "X" + x.B + "Z" })),
                "stringconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { "X" + x.B + x.C })),
                "stringproperty+intconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + 2 + x.C })),
                "stringproperty+intproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + x.J + x.C })),
                "stringproperty+stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + "Y" + x.C })),
                "stringproperty+stringproperty+intconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + x.B + 3 })),
                "stringproperty+stringproperty+intproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + x.B + x.K })),
                "stringproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + x.B + "Z" })),
                "stringproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new object[] { x.A + x.B + x.C })),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        [Theory]
        [InlineData("stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty", "{ $project : { _v : { $concat : '$A' }, _id : 0 } }", "A", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty", "{ $project : { _v : { $concat : ['X', '$B'] }, _id : 0 } }", "XB", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', 'X'] }, _id : 0 } }", "AX", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B'] }, _id : 0 } }", "AB", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['X', '$B', 'Z'] }, _id : 0 } }", "XBZ", LinqProvider.V3)]
        [InlineData("stringconstant+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringconstant+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['X', '$B', '$C'] }, _id : 0 } }", "XBC", LinqProvider.V3)]
        [InlineData("stringproperty+stringconstant+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringconstant+stringproperty", "{ $project : { _v : { $concat : ['$A', 'Y', '$C'] }, _id : 0 } }", "AYC", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringconstant", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringconstant", "{ $project : { _v : { $concat : ['$A', '$B', 'Z'] }, _id : 0 } }", "ABZ", LinqProvider.V3)]
        [InlineData("stringproperty+stringproperty+stringproperty", "throws:NotSupportedException", null, LinqProvider.V2)]
        [InlineData("stringproperty+stringproperty+stringproperty", "{ $project : { _v : { $concat : ['$A', '$B', '$C'] }, _id : 0 } }", "ABC", LinqProvider.V3)]
        public void Concat_with_array_of_string_argument_should_work(string scenario, string expectedStage, string expectedResult, LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = scenario switch
            {
                "stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A })),
                "stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { "X", x.B })),
                "stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A, "X" })),
                "stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A, x.B })),
                "stringconstant+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new string[] { "X" + x.B + "Z" })),
                "stringconstant+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { "X" + x.B + x.C })),
                "stringproperty+stringconstant+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A + "Y" + x.C })),
                "stringproperty+stringproperty+stringconstant" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A + x.B + "Z" })),
                "stringproperty+stringproperty+stringproperty" => collection.AsQueryable().Select(x => string.Concat(new string[] { x.A + x.B + x.C })),
                _ => throw new Exception()
            };

            Assert(collection, queryable, expectedStage, expectedResult);
        }

        private void Assert(IMongoCollection<Document> collection, IQueryable<string> queryable, string expectedStage, string expectedResult)
        {
            if (expectedStage.StartsWith("throws:"))
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().NotBeNull();
                exception.GetType().Name.Should().Be(expectedStage.Substring(7));
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, expectedStage.StartsWith("wrong:") ? expectedStage.Substring(6) : expectedStage);

                if (expectedResult.StartsWith("throws:"))
                {
                    var exception = Record.Exception(() => queryable.Single());
                    exception.Should().NotBeNull();
                    exception.GetType().Name.Should().Be(expectedResult.Substring(7));
                }
                else
                {
                    var result = queryable.Single();
                    result.Should().Be(expectedResult);
                }
            }
        }

        private IMongoCollection<Document> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<Document>("test", linqProvider);
            CreateCollection(
                collection,
                new Document
                {
                    Id = 1,
                    A = "A",
                    B = "B",
                    C = "C",
                    I = 1,
                    J = 2,
                    K = 3
                });
            return collection;
        }

        private class Document
        {
            public int Id { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public int I { get; set; }
            public int J { get; set; }
            public int K { get; set; }
        }
    }
}
