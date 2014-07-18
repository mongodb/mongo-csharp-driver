/* Copyright 2013-2014 MongoDB Inc.
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
using System.Diagnostics;
using FluentAssertions;
using FluentAssertions.Numeric;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Tests
{
    [DebuggerNonUserCode]
    public static class AssertionExtensions
    {
        public static ComparableTypeAssertions<BsonDocument> Should(this BsonDocument document)
        {
            return ((IComparable<BsonDocument>)document).Should();
        }

        public static AndConstraint<ComparableTypeAssertions<BsonDocument>> Be(this ComparableTypeAssertions<BsonDocument> assertions, string json)
        {
            return assertions.Be(BsonDocument.Parse(json));
        }
    }
}