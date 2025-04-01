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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using Shouldly;
using Xunit;

namespace MongoDB.Bson.TestHelpers;

[ShouldlyMethods]
public static class AssertionExtensions
{
    public static void ShouldBe(this BsonDocument subject, string expected)
    {
        var expectedDocument = expected == null ? null : BsonDocument.Parse(expected);
        subject.ShouldBe(expectedDocument);
    }

    public static void ShouldBe(this BsonArray subject, string expected)
    {
        var expectedArray = expected == null ? null : BsonSerializer.Deserialize<BsonArray>(expected);
        subject.ShouldBe(expectedArray);
    }

    public static void ShouldHaveCount<T>(this IEnumerable<T> subject, int expectedCount)
    {
        subject.Count().ShouldBe(expectedCount);
    }

    public static void ShouldContain(this BsonDocument subject, string expected)
    {
        subject.Elements.Select(e => e.Name).ShouldContain(expected);
    }

    public static void ShouldNotContain(this BsonDocument subject, string expected)
    {
        subject.Elements.Select(e => e.Name).ShouldNotContain(expected);
    }

    public static void ShouldMatch<T>(this T subject, Predicate<T> predicate)
    {
        predicate(subject).ShouldBeTrue();
    }

    public static void ShouldNotHaveValue(this object subject)
    {
        subject.ShouldBeNull();
    }

    public static void ShouldBeTrue(this bool? subject)
    {
        subject.ShouldBe(true);
    }

    public static void ShouldBeFalse(this bool? subject)
    {
        subject.ShouldBe(false);
    }

    public static void ShouldOnlyContain<T>(this IEnumerable<T> subject, Expression<Func<T, bool>> predicate)
    {
        subject.ShouldAllBe(predicate);
    }
}
