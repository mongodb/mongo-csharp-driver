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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5188Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_C_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(C));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : ['$_t', 'C'] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(true, false, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_D_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(D));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : ['$_t', ['C', 'D']] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(false, true, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_E_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(E));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : ['$_t', ['C', 'D', 'E']] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(false, false, true);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_F_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(F));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : [{ $type : '$_t' }, 'missing'] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(true, false, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_G_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(G));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : ['$_t', 'G'] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(false, true, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_GetType_equals_H_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.GetType() == typeof(H));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $eq : ['$_t', 'H'] }, _id : 0 } }"); // don't match subclasses!

                var results = queryable.ToList();
                results.Should().Equal(false, false, true);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_C_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(C));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { '_t.0' : { $exists : false }, '_t' : 'C' } }"); // don't match subclasses!

                var result = (C)queryable.Single();
                result.Id.Should().Be(1);
                result.X.Should().Be(2);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_D_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(D));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { _t : { $size : 2 }, '_t.0' : 'C', '_t.1' : 'D' } }"); // don't match subclasses!

                var result = (D)queryable.Single();
                result.Id.Should().Be(2);
                result.X.Should().Be(3);
                result.Y.Should().Be(4);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_E_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfC(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(E));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { _t : { $size : 3 }, '_t.0' : 'C', '_t.1' : 'D', '_t.2' : 'E' } }"); // don't match subclasses!

                var result = (E)queryable.Single();
                result.Id.Should().Be(3);
                result.X.Should().Be(4);
                result.Y.Should().Be(5);
                result.Z.Should().Be(6);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_F_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(F));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { '_t' : { $exists : false } } }");

                var result = (F)queryable.Single();
                result.Id.Should().Be(1);
                result.X.Should().Be(2);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_G_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(G));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { '_t.0' : { $exists : false }, '_t' : 'G' } }");

                var result = (G)queryable.Single();
                result.Id.Should().Be(2);
                result.X.Should().Be(3);
                result.Y.Should().Be(4);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_GetType_equals_H_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollectionOfF(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(H));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { '_t.0' : { $exists : false }, '_t' : 'H' } }");

                var result = (H)queryable.Single();
                result.Id.Should().Be(3);
                result.X.Should().Be(4);
                result.Y.Should().Be(5);
                result.Z.Should().Be(6);
            }
        }

        private IMongoCollection<C> GetCollectionOfC(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, X = 2 },
                new D { Id = 2, X = 3, Y = 4 },
                new E { Id = 3, X = 4, Y = 5, Z = 6 });
            return collection;
        }

        private IMongoCollection<F> GetCollectionOfF(LinqProvider linqProvider)
        {
            var collection = GetCollection<F>("test", linqProvider);
            CreateCollection(
                collection,
                new F { Id = 1, X = 2 },
                new G { Id = 2, X = 3, Y = 4 },
                new H { Id = 3, X = 4, Y = 5, Z = 6 });
            return collection;
        }

        [BsonDiscriminator(RootClass = true)]
        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class D : C
        {
            public int Y { get; set; }
        }

        private class E : D
        {
            public int Z { get; set; }
        }

        // no [BsonDiscriminator] attribute
        private class F
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class G : F
        {
            public int Y { get; set; }
        }

        private class H : G
        {
            public int Z { get; set; }
        }
    }
}
