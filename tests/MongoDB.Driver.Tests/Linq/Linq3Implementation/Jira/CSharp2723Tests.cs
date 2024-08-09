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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2723Tests
    {
        [Fact]
        public void Nested_Select_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("foo");
            var parents = database.GetCollection<Parent>("parents");
            var children = database.GetCollection<Child>("children");
            var grandChildren = database.GetCollection<GrandChild>("grandChildren");

            database.DropCollection("parents");
            database.DropCollection("children");
            database.DropCollection("grandChildren");

            parents.InsertMany(new[]
            {
                    new Parent { Id = 1, Name = "parent1" },
                    new Parent { Id = 2, Name = "parent2" }
                });

            children.InsertMany(new[]
            {
                    new Child { Id = 1, Name = "child1", ParentId = 2 },
                    new Child { Id = 2, Name = "child2", ParentId = 1 }
                });

            grandChildren.InsertMany(new[]
            {
                    new GrandChild { Id = 1, Name = "grandchild1", ChildId = 2 },
                    new GrandChild { Id = 2, Name = "grandchild2", ChildId = 1 }
                });

            var aggregate = parents
                .Aggregate()
                .Lookup<Parent, Child, ParentProjection>(
                    children,
                    parent => parent.Id,
                    child => child.ParentId,
                    parentProjection => parentProjection.Children)
                .Lookup<ParentProjection, GrandChild, GrandChild, IEnumerable<GrandChild>, ParentProjection>(
                    grandChildren,
                    let: new BsonDocument { { "children", "$Children" } },
                    lookupPipeline: new BsonDocumentStagePipelineDefinition<GrandChild, GrandChild>(
                        new[] { BsonDocument.Parse(@"{ $match : { $expr : { $and : [ { $in : [ ""$ChildId"", ""$$children._id"" ] } ] } } }") }),
                    childProjection => childProjection.GrandChildren)
                .Project(parent => new ParentTree
                {
                    Id = parent.Id,
                    ParentName = parent.Name,
                    Children = parent.Children
                        .Select(child => new ChildTree
                        {
                            Id = child.Id,
                            Name = child.Name,
                            GrandChildren = parent
                                .GrandChildren
                                .Where(gc =>
                                    gc.ChildId ==
                                    child.Id)
                                .Select(gc =>
                                    new GrandChildProjection()
                                    {
                                        Id = gc.Id,
                                        Name = gc.Name
                                    })
                        })
                });

            var stages = Linq3TestHelpers.Translate(parents, aggregate);
            var expectedStages = new[]
            {
                @"{
                    '$lookup':{
                        'from':'children',
                        'localField':'_id',
                        'foreignField':'ParentId',
                        'as':'Children'
                    }
                }",
                @"{
                    '$lookup':{
                        'from':'grandChildren',
                        'let':{'children':'$Children'},
                        'pipeline':[
                        {
                            '$match':{'$expr':{'$and':[{'$in':['$ChildId','$$children._id']}]}}}
                        ],
                        'as':'GrandChildren'
                    }
                }",
                    @"{
                    '$project':{
                        '_id':'$_id',
                        'ParentName':'$Name',
                        'Children':{
                            '$map':{
                                'input':'$Children',
                                'as':'child',
                                'in':{
                                    '_id':'$$child._id',
                                    'Name':'$$child.Name',
                                    'GrandChildren':{
                                        '$map':{
                                            'input':{
                                                '$filter':{
                                                    'input':'$GrandChildren',
                                                    'as':'gc',
                                                    'cond':{
                                                        '$eq':[
                                                            '$$gc.ChildId',
                                                            '$$child._id'
                                                        ]
                                                    }
                                                }
                                            },
                                            'as':'gc',
                                            'in':{
                                                '_id':'$$gc._id',
                                                'Name':'$$gc.Name'
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }"
                };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var pipelineDefinition = new BsonDocumentStagePipelineDefinition<Parent, BsonDocument>(expectedStages.Select(s => BsonDocument.Parse(s)));
            var resultBsonDocuments = parents.Aggregate(pipelineDefinition).ToList();
            var expectedBsonDocuments = new[]
            {
                    "{ '_id' : 1, 'ParentName' : 'parent1', 'Children' : [{ '_id' : 2, 'Name' : 'child2', 'GrandChildren' : [{ '_id' : 1, 'Name' : 'grandchild1' }] }] }",
                    "{ '_id' : 2, 'ParentName' : 'parent2', 'Children' : [{ '_id' : 1, 'Name' : 'child1', 'GrandChildren' : [{ '_id' : 2, 'Name' : 'grandchild2' }] }] }"
                };
            resultBsonDocuments.Should().Equal(expectedBsonDocuments.Select(d => BsonDocument.Parse(d)));

            var result = aggregate.ToList();
            var expectedResults = new[]
            {
                    new ParentTree { Id = 1, ParentName = "parent1", Children = new[] { new ChildTree { Id = 2, Name = "child2", GrandChildren = new[] { new GrandChildProjection { Id = 1, Name = "grandchild1" } } } } },
                    new ParentTree { Id = 2, ParentName = "parent2", Children = new[] { new ChildTree { Id = 1, Name = "child1", GrandChildren = new[] { new GrandChildProjection { Id = 2, Name = "grandchild2" } } } } }
                };
            var parentTreeComparer = new ParentTreeComparer();
            result.Should().Equal(expectedResults, (x, y) => parentTreeComparer.Equals(x, y));
        }

        public class Parent
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Child
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ParentId { get; set; }
        }

        public class GrandChild
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ChildId { get; set; }
        }

        public class ParentProjection
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<Child> Children { get; set; }
            public IEnumerable<GrandChild> GrandChildren { get; set; }
        }

        public class GrandChildProjection
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class ParentTree
        {
            public int Id { get; set; }
            public string ParentName { get; set; }
            public IEnumerable<ChildTree> Children { get; set; }
        }

        public class ChildTree
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<GrandChildProjection> GrandChildren { get; set; }
        }

        private class ParentTreeComparer : IEqualityComparer<ParentTree>
        {
            private readonly IEqualityComparer<ChildTree> _childTreeComparer = new ChildTreeComparer();

            public bool Equals(ParentTree x, ParentTree y)
            {
                return
                    x.Id == y.Id &&
                    x.ParentName == y.ParentName &&
                    x.Children.SequenceEqual(y.Children, _childTreeComparer);
            }

            public int GetHashCode(ParentTree obj) => throw new System.NotImplementedException();
        }

        private class ChildTreeComparer : IEqualityComparer<ChildTree>
        {
            private readonly IEqualityComparer<GrandChildProjection> _grandChildProjectionComparer = new GrandChildProjectionComparer();

            public bool Equals(ChildTree x, ChildTree y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.GrandChildren.SequenceEqual(y.GrandChildren, _grandChildProjectionComparer);
            }

            public int GetHashCode(ChildTree obj) => throw new System.NotImplementedException();
        }

        private class GrandChildProjectionComparer : IEqualityComparer<GrandChildProjection>
        {
            public bool Equals(GrandChildProjection x, GrandChildProjection y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name;
            }

            public int GetHashCode(GrandChildProjection obj) => throw new System.NotImplementedException();
        }
    }
}
