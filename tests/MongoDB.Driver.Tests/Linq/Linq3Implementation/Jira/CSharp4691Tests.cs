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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4691Tests : Linq3IntegrationTest
    {
        static CSharp4691Tests()
        {
            BsonClassMap.RegisterClassMap<Activity1>(cm =>
            {
                cm.AutoMap();
                Func<Type, bool> allowedTypes = t => t == typeof(string) || t == typeof(MyActivityObject) || t == typeof(MyActivityObjectDerived);
                var discriminatorConvention = new HierarchicalDiscriminatorConvention("_t");
                var objectSerializer = new ObjectSerializer(discriminatorConvention, allowedTypes);
                cm.MapMember(x => x.Object).SetSerializer(objectSerializer);
            });
        }

        [Fact]
        public void Find_with_object_property_GetType_comparison_to_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object.GetType() == typeof(TActivityObject) &&
                        ((TActivityObject)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t.0' : { $exists : false }, 'Object._t' : 'MyActivityObject', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(3, 4);
            }
        }

        [Fact]
        public void Find_with_object_property_GetType_comparison_to_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object.GetType() == typeof(TActivityObject) &&
                        ((TActivityObject)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : { $size : 2 }, 'Object._t.0' : 'MyActivityObject', 'Object._t.1' : 'MyActivityObjectDerived', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(6);
            }
        }

        [Fact]
        public void Find_with_object_property_is_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object is TActivityObject &&
                        ((TActivityObject)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : 'MyActivityObject', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(3, 4, 6);
            }
        }

        [Fact]
        public void Find_with_object_property_is_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object is TActivityObject &&
                        ((TActivityObject)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : 'MyActivityObjectDerived', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(6);
            }
        }

        [Fact]
        public void Find_with_typed_property_GetType_comparison_to_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object.GetType() == typeof(TActivityObject) &&
                        ((TActivityObject)(object)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t.0' : { $exists : false }, 'Object._t' : 'MyActivityObject', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(3, 4);
            }
        }

        [Fact]
        public void Find_with_typed_property_GetType_comparison_to_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object.GetType() == typeof(TActivityObject) &&
                        ((TActivityObject)(object)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : { $size : 2 }, 'Object._t.0' : 'MyActivityObject',  'Object._t.1' : 'MyActivityObjectDerived', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(6);
            }
        }

        [Fact]
        public void Find_with_typed_property_is_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object is TActivityObject &&
                        ((TActivityObject)(object)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : 'MyActivityObject', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(3, 4, 6);
            }
        }

        [Fact]
        public void Find_with_typed_property_is_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();
                var activityId = 1;

                var find = collection
                    .Find(x =>
                        x.Object != null &&
                        x.Object is TActivityObject &&
                        ((TActivityObject)(object)x.Object).Id.Equals(activityId)
                    );

                var filter = TranslateFindFilter(collection, find);
                var results = find.ToList();

                filter.Should().Be("{ Object : { $ne : null }, 'Object._t' : 'MyActivityObjectDerived', 'Object._id' : 1 }");
                results.Select(x => x.Id).Should().Equal(6);
            }
        }

        [Fact]
        public void Select_with_object_property_GetType_comparison_to_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object.GetType() == typeof(TActivityObject) });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $eq : ['$Object._t', 'MyActivityObject'] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, true, true, true, false);
            }
        }

        [Fact]
        public void Select_with_object_property_GetType_comparison_to_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object.GetType() == typeof(TActivityObject) });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $eq : ['$Object._t', ['MyActivityObject', 'MyActivityObjectDerived']] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, false, false, false, true);
            }
        }

        [Fact]
        public void Select_with_object_property_is_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object is TActivityObject });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $or : [{ $eq : ['$Object._t', 'MyActivityObject'] }, { $and : [{ $isArray : '$Object._t' }, { $in : ['MyActivityObject', '$Object._t'] }]  }] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, true, true, true, true);
            }
        }

        [Fact]
        public void Select_with_object_property_is_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection1();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object is TActivityObject });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $or : [{ $eq : ['$Object._t', 'MyActivityObjectDerived'] }, { $and : [{ $isArray : '$Object._t' }, { $in : ['MyActivityObjectDerived', '$Object._t'] }]  }] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, false, false, false, true);
            }
        }

        [Fact]
        public void Select_with_typed_property_GetType_comparison_to_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object.GetType() == typeof(TActivityObject) });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $eq : ['$Object._t', 'MyActivityObject'] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, true, true, true, false);
            }
        }

        [Fact]
        public void Select_with_typed_property_GetType_comparison_to_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object.GetType() == typeof(TActivityObject) });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $eq : ['$Object._t', ['MyActivityObject', 'MyActivityObjectDerived']] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, false, false, true);
            }
        }

        [Fact]
        public void Select_with_typed_property_is_base_class_should_work()
        {
            Implementation<MyActivityObject, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object is TActivityObject });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $or : [{ $eq : ['$Object._t', 'MyActivityObject'] }, { $and : [{ $isArray : '$Object._t' }, { $in : ['MyActivityObject', '$Object._t'] }]  }] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, true, true, true, true);
            }
        }

        [Fact]
        public void Select_with_typed_property_is_derived_class_should_work()
        {
            Implementation<MyActivityObjectDerived, int>();

            void Implementation<TActivityObject, TId>()
                where TActivityObject : ActivityObject<TId>
                where TId : notnull, IEquatable<TId>, new()
            {
                var collection = GetCollection2();

                var queryable = collection.AsQueryable()
                    .Select(x => new { x.Id, R = x.Object is TActivityObject });

                var stages = Translate(collection, queryable);
                var results = queryable.ToList();

                AssertStages(stages, "{ $project : { _id : '$_id', R : { $or : [{ $eq : ['$Object._t', 'MyActivityObjectDerived'] }, { $and : [{ $isArray : '$Object._t' }, { $in : ['MyActivityObjectDerived', '$Object._t'] }]  }] } } }");
                results.OrderBy(x => x.Id).Select(x => x.R).Should().Equal(false, false, false, false, true);
            }
        }

        private IMongoCollection<Activity1> GetCollection1()
        {
            var collection = GetCollection<Activity1>("test");
            CreateCollection(
                collection,
                new Activity1 { Id = 1, Object = null },
                new Activity1 { Id = 2, Object = "abc" },
                new Activity1 { Id = 3, Object = new MyActivityObject { Id = 1 } },
                new Activity1 { Id = 4, Object = new MyActivityObject { Id = 1 } },
                new Activity1 { Id = 5, Object = new MyActivityObject { Id = 2 } },
                new Activity1 { Id = 6, Object = new MyActivityObjectDerived { Id = 1 } });
            return collection;
        }

        private IMongoCollection<Activity2> GetCollection2()
        {
            var collection = GetCollection<Activity2>("test");
            CreateCollection(
                collection,
                new Activity2 { Id = 1, Object = null },
                new Activity2 { Id = 3, Object = new MyActivityObject { Id = 1 } },
                new Activity2 { Id = 4, Object = new MyActivityObject { Id = 1 } },
                new Activity2 { Id = 5, Object = new MyActivityObject { Id = 2 } },
                new Activity2 { Id = 6, Object = new MyActivityObjectDerived { Id = 1 } });
            return collection;
        }

        private class Activity1
        {
            public int Id { get; set; }
            public object Object { get; set; }
        }

        private class Activity2
        {
            public int Id { get; set; }
            public ActivityObject<int> Object { get; set; }
        }

        private abstract class ActivityObject<TId>
        {
            public abstract TId Id { get; set; }
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(MyActivityObjectDerived))]
        private class MyActivityObject : ActivityObject<int>
        {
            public override int Id { get; set; }
        }

        private class MyActivityObjectDerived : MyActivityObject
        {
        }
    }
}
