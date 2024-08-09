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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4522Tests : Linq3IntegrationTest
    {
        static CSharp4522Tests()
        {
            CreateClassMaps();
        }

        [Fact]
        public void User_provided_example_should_work()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            AddSomeData(database);

            //just test we get all animals
            var getAllAnimals = GetThings<AAnimal>(
                database,
                filter: null);
            getAllAnimals.Count.Should().Be(6);

            //should only get 1
            var getAnimalsBasedOnSomething = GetThings<AAnimal>(
                database,
                filter: x => (x as Pig).WillBeFood);
            getAnimalsBasedOnSomething.Count.Should().Be(1);
        }

        public static void CreateClassMaps()
        {
            var types = new Type[]
            {
                typeof(AMongoThing),
                typeof(AAnimal),
                typeof(Cat),
                typeof(Horse),
                typeof(Pig)
            };

            foreach (var item in types)
            {
                if (!item.IsInterface)
                {
                    var classMap = new BsonClassMap(item);
                    classMap.AutoMap();
                    classMap.SetDiscriminator(item.FullName);

                    if (!BsonClassMap.IsClassMapRegistered(item))
                    {
                        BsonClassMap.RegisterClassMap(classMap);
                    }
                }
            }
        }

        public static void AddSomeData(IMongoDatabase DB)
        {
            UpsertThing<Cat>(
                DB,
                filter: null,
                new Cat()
                {
                    ID = "63e169c103f81b89b23add99", // only setting this manually to prevent duplicates when re-running the program
                    IsDomesticated = true,
                    Age = 1,
                    Gender = "Male",
                    Name = "Fluffanutter"
                }
            );
            UpsertThing<Cat>(
                DB,
                filter: null,
                new Cat()
                {
                    ID = "63e169f4b42641ce7c5e85af", // only setting this manually to prevent duplicates when re-running the program
                    IsDomesticated = false,
                    Age = 2,
                    Gender = "Female",
                    Name = "Brown Cat"
                }
            );
            UpsertThing<Horse>(
                DB,
                filter: null,
                new Horse()
                {
                    ID = "63e169f73aad61eaad4a78aa", // only setting this manually to prevent duplicates when re-running the program
                    LivesOnFarm = true,
                    Age = 6,
                    Gender = "Male",
                    Name = "Neigh Neigh"
                }
            );
            UpsertThing<Horse>(
                DB,
                filter: null,
                new Horse()
                {
                    ID = "63e169fbbfb45bed8c515fe4", // only setting this manually to prevent duplicates when re-running the program
                    LivesOnFarm = false,
                    Age = 12,
                    Gender = "Male",
                    Name = "Mr. Ed"
                }
            );
            UpsertThing<Pig>(
                DB,
                filter: null,
                new Pig()
                {
                    ID = "63e169ffb57d09e93a93251c", // only setting this manually to prevent duplicates when re-running the program
                    WillBeFood = true,
                    Age = 3,
                    Gender = "Male",
                    Name = "Wilbur"
                }
            );
            UpsertThing<Pig>(
                DB,
                filter: null,
                new Pig()
                {
                    ID = "63e16a03db8e428dd6240b43", // only setting this manually to prevent duplicates when re-running the program
                    WillBeFood = false,
                    Age = 15,
                    Gender = "Female",
                    Name = "Sir Oinks"
                }
            );
        }

        public static T UpsertThing<T>(
            IMongoDatabase DB,
            Expression<Func<T, bool>> filter,
            T record)
        {
            var collectionName = (record as AMongoThing).StorageGrouping;
            var coll = DB.GetCollection<T>(collectionName);

            if ((record as AMongoThing).Created == null)
            {
                (record as AMongoThing).Created = DateTime.UtcNow;
            }

            (record as AMongoThing).LastModified = DateTime.UtcNow;

            if (string.IsNullOrEmpty((record as AMongoThing).ID))
            {
                coll.InsertOne(record);
                return record;
            }
            else
            {
                if (filter == null)
                {
                    filter = x => (x as AMongoThing).ID == (record as AMongoThing).ID;
                }
                else
                {
                    filter = filter.And<T>(x => (x as AMongoThing).ID == (record as AMongoThing).ID);
                }

                return coll.FindOneAndReplace(
                    filter,
                    record,
                    new FindOneAndReplaceOptions<T, T> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            }
        }

        public static List<T> GetThings<T>(
            IMongoDatabase DB,
            Expression<Func<T, bool>> filter)
        {
            var collectionName = "Unknown";

            if (typeof(T) == typeof(AMongoThing) || typeof(T).IsSubclassOf(typeof(AMongoThing)))
            {
                var temp = Activator.CreateInstance(typeof(T));
                collectionName = (temp as AMongoThing).StorageGrouping;
            }

            var coll = DB.GetCollection<T>(collectionName);
            var myCursor = coll.FindSync<T>(filter ?? FilterDefinition<T>.Empty);

            List<T> returnValue = new List<T>();
            while (myCursor.MoveNext())
            {
                returnValue.AddRange(myCursor.Current as List<T>);
            }

            return returnValue;
        }

        [BsonIgnoreExtraElements]
        public class AMongoThing
        {
            [BsonId]
            [BsonIgnoreIfDefault]
            [BsonRepresentation(BsonType.ObjectId)]
            public string ID { get; set; }

            public string Name { get; set; } = "";

            public string StorageGrouping { get; set; }

            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime? Created { get; set; }

            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime? LastModified { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class AAnimal : AMongoThing
        {
            public string Gender { get; set; }
            public int Age { get; set; }

            public AAnimal()
            {
                this.StorageGrouping = "Animals";
            }
        }

        [BsonIgnoreExtraElements]
        public class Cat : AAnimal
        {
            public bool IsDomesticated { get; set; }

            public Cat()
            {
                this.StorageGrouping = "Animals";
            }
        }

        [BsonIgnoreExtraElements]
        public class Horse : AAnimal
        {
            public bool LivesOnFarm { get; set; }

            public Horse()
            {
                this.StorageGrouping = "Animals";
            }
        }

        [BsonIgnoreExtraElements]
        public class Pig : AAnimal
        {
            public bool WillBeFood { get; set; }

            public Pig()
            {
                this.StorageGrouping = "Animals";
            }
        }

    }

    public static class ExpressionCombiner
    {
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> exp, Expression<Func<T, bool>> newExp)
        {
            var visitor = new ParameterUpdateVisitor(newExp.Parameters.First(), exp.Parameters.First());
            newExp = visitor.Visit(newExp) as Expression<Func<T, bool>>;
            var binExp = Expression.And(exp.Body, newExp.Body);
            return Expression.Lambda<Func<T, bool>>(binExp, newExp.Parameters);
        }

        public class CsharpLegacyGuidSerializationProvider : IBsonSerializationProvider
        {
            public IBsonSerializer GetSerializer(Type type)
            {
                if (type == typeof(Guid))
                    return new GuidSerializer(GuidRepresentation.Standard);

                return null;
            }
        }

        public class ParameterUpdateVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private ParameterExpression _oldParameter;
            private ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (object.ReferenceEquals(node, _oldParameter))
                    return _newParameter;

                return base.VisitParameter(node);
            }
        }
    }
}
