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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

// Regression test for CSHARP-6091.
public class SelectCheckedAddSubtractProjectionTests : LinqIntegrationTest<SelectCheckedAddSubtractProjectionTests.ClassFixture>
{
    public SelectCheckedAddSubtractProjectionTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_with_checked_or_unchecked_add_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, R>> projection;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                projection = x => new R { V = x.A + x.B };
            }
        }
        else
        {
            projection = x => new R { V = x.A + x.B };
        }

        var queryable = collection.AsQueryable().Select(projection);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { V : { $add : ['$A', '$B'] }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.V).ToList();
        results.Select(x => x.V).Should().Equal(11, 22);
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_with_checked_or_unchecked_subtract_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, R>> projection;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                projection = x => new R { V = x.A - x.B };
            }
        }
        else
        {
            projection = x => new R { V = x.A - x.B };
        }

        var queryable = collection.AsQueryable().Select(projection);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { V : { $subtract : ['$A', '$B'] }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.V).ToList();
        results.Select(x => x.V).Should().Equal(9, 18);
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_with_checked_or_unchecked_multiply_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, R>> projection;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                projection = x => new R { V = x.A * x.B };
            }
        }
        else
        {
            projection = x => new R { V = x.A * x.B };
        }

        var queryable = collection.AsQueryable().Select(projection);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { V : { $multiply : ['$A', '$B'] }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.V).ToList();
        results.Select(x => x.V).Should().Equal(10, 40);
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_with_checked_or_unchecked_negate_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, R>> projection;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                projection = x => new R { V = -x.A };
            }
        }
        else
        {
            projection = x => new R { V = -x.A };
        }

        var queryable = collection.AsQueryable().Select(projection);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { V : { $subtract : [0, '$A'] }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.V).ToList();
        results.Select(x => x.V).Should().Equal(-20, -10);
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_with_checked_or_unchecked_add_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, bool>> predicate;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                predicate = x => x.A + x.B > 15;
            }
        }
        else
        {
            predicate = x => x.A + x.B > 15;
        }

        var queryable = collection.AsQueryable().Where(predicate);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { $expr : { $gt : [{ $add : ['$A', '$B'] }, 15] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_with_checked_or_unchecked_subtract_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, bool>> predicate;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                predicate = x => x.A - x.B > 10;
            }
        }
        else
        {
            predicate = x => x.A - x.B > 10;
        }

        var queryable = collection.AsQueryable().Where(predicate);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { $expr : { $gt : [{ $subtract : ['$A', '$B'] }, 10] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_with_checked_or_unchecked_multiply_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, bool>> predicate;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                predicate = x => x.A * x.B > 30;
            }
        }
        else
        {
            predicate = x => x.A * x.B > 30;
        }

        var queryable = collection.AsQueryable().Where(predicate);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { $expr : { $gt : [{ $multiply : ['$A', '$B'] }, 30] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_with_checked_or_unchecked_negate_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, bool>> predicate;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                predicate = x => -x.A < -15;
            }
        }
        else
        {
            predicate = x => -x.A < -15;
        }

        var queryable = collection.AsQueryable().Where(predicate);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { $expr : { $lt : [{ $subtract : [0, '$A'] }, -15] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_with_checked_or_unchecked_enum_add_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, RE>> projection;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                projection = x => new RE { S = x.Status + 1 };
            }
        }
        else
        {
            projection = x => new RE { S = x.Status + 1 };
        }

        var queryable = collection.AsQueryable().Select(projection);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { S : { $add : ['$Status', 1] }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.S).ToList();
        results.Select(x => x.S).Should().Equal(E.B, E.C);
    }

    public enum E { A = 0, B = 1, C = 2 }

    public class C
    {
        public int Id { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public E Status { get; set; }
    }

    public class R
    {
        public int V { get; set; }
    }

    public class RE
    {
        public E S { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, A = 10, B = 1, Status = E.A },
            new C { Id = 2, A = 20, B = 2, Status = E.B },
        ];
    }
}
