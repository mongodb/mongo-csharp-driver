/* Copyright 2017-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class DocumentationExamples
    {
        private readonly IMongoClient client;
        private readonly IMongoCollection<BsonDocument> collection;
        private readonly IMongoDatabase database;

        public DocumentationExamples()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            client = new MongoClient(connectionString);
            database = client.GetDatabase("test");
            collection = database.GetCollection<BsonDocument>("inventory");
            database.DropCollection("inventory");
        }

        [Fact]
        public void Example_1()
        {
            // db.inventory.insertOne( { item: "canvas", qty: 100, tags: ["cotton"], size: { h: 28, w: 35.5, uom: "cm" } } ) 

            // Start Example 1
            var document = new BsonDocument
            {
                { "item", "canvas" },
                { "qty", 100 },
                { "tags", new BsonArray { "cotton" } },
                { "size", new BsonDocument { { "h", 28 }, { "w", 35.5 }, { "uom", "cm" } } }
            };
            collection.InsertOne(document);
            // End Example 1

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"canvas\", qty: 100, tags: [\"cotton\"], size: { h: 28, w: 35.5, uom: \"cm\" } }"));
        }

        [Fact]
        public void Example_2()
        {
            // db.inventory.find( { item: "canvas" } )

            // Start Example 2
            var filter = Builders<BsonDocument>.Filter.Eq("item", "canvas");
            var result = collection.Find(filter).ToList();
            // End Example 2

            Render(filter).Should().Be("{ item: \"canvas\" }");
        }

        [Fact]
        public void Example_3()
        {
            // db.inventory.insertMany([ 
            //   { item: "journal", qty: 25, tags: ["blank", "red"], size: { h: 14, w: 21, uom: "cm" } },
            //   { item: "mat", qty: 85, tags: ["gray"], size: { h: 27.9, w: 35.5, uom: "cm" } },
            //   { item: "mousepad", qty: 25, tags: ["gel", "blue"], size: { h: 19, w: 22.85, uom: "cm" } } ]) 

            // Start Example 3
            var documents = new BsonDocument[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "tags", new BsonArray { "blank", "red" } },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, {  "uom", "cm"} } }
                },
                new BsonDocument
                {
                    { "item", "mat" },
                    { "qty", 85 },
                    { "tags", new BsonArray { "gray" } },
                    { "size", new BsonDocument { { "h", 27.9 }, { "w", 35.5 }, {  "uom", "cm"} } }
                },
                new BsonDocument
                {
                    { "item", "mousepad" },
                    { "qty", 25 },
                    { "tags", new BsonArray { "gel", "blue" } },
                    { "size", new BsonDocument { { "h", 19 }, { "w", 22.85 }, {  "uom", "cm"} } }
                },
            };
            collection.InsertMany(documents);
            // End Example 3

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
               "{ item: \"journal\", qty: 25, tags: [\"blank\", \"red\"], size: { h: 14, w: 21, uom: \"cm\" } }",
               "{ item: \"mat\", qty: 85, tags: [\"gray\"], size: { h: 27.9, w: 35.5, uom: \"cm\" } }",
               "{ item: \"mousepad\", qty: 25, tags: [\"gel\", \"blue\"], size: { h: 19, w: 22.85, uom: \"cm\" } }"));
        }

        [Fact]
        public void Example_6()
        {
            // db.inventory.insertMany([
            //   { item: "journal", qty: 25, size: { h: 14, w: 21, uom: "cm" }, status: "A" },
            //   { item: "notebook", qty: 50, size: { h: 8.5, w: 11, uom: "in" }, status: "A" },
            //   { item: "paper", qty: 100, size: { h: 8.5, w: 11, uom: "in" }, status: "D" },
            //   { item: "planner", qty: 75, size: { h: 22.85, w: 30, uom: "cm" }, status: "D" },
            //   { item: "postcard", qty: 45, size: { h: 10, w: 15.25, uom: "cm" }, status: "A" } ]) 

            // Start Example 6
            var documents = new BsonDocument[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, {  "uom", "cm"} } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "qty", 50 },
                    { "size", new BsonDocument { { "h",  8.5 }, { "w", 11 }, {  "uom", "in"} } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "qty", 100 },
                    { "size", new BsonDocument { { "h",  8.5 }, { "w", 11 }, {  "uom", "in"} } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "qty", 75 },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30  }, {  "uom", "cm"} } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "qty", 45 },
                    { "size", new BsonDocument { { "h", 10 }, { "w", 15.25 }, {  "uom", "cm"} } },
                    { "status", "A" }
                },
            };
            collection.InsertMany(documents);
            // End Example 6

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
               "{ item: \"journal\", qty: 25, size: { h: 14, w: 21, uom: \"cm\" }, status: \"A\" }",
               "{ item: \"notebook\", qty: 50, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"A\" }",
               "{ item: \"paper\", qty: 100, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"D\" }",
               "{ item: \"planner\", qty: 75, size: { h: 22.85, w: 30, uom: \"cm\" }, status: \"D\" }",
               "{ item: \"postcard\", qty: 45, size: { h: 10, w: 15.25, uom: \"cm\" }, status: \"A\" }"));
        }

        [Fact]
        public void Example_7()
        {
            // db.inventory.find( { } )

            // Start Example 7
            var filter = Builders<BsonDocument>.Filter.Empty;
            var result = collection.Find(filter).ToList();
            // End Example 7

            Render(filter).Should().Be("{}");
        }

        [Fact]
        public void Example_9()
        {
            // db.inventory.find( { status: "D" } )

            // Start Example 9
            var filter = Builders<BsonDocument>.Filter.Eq("status", "D");
            var result = collection.Find(filter).ToList();
            // End Example 9

            Render(filter).Should().Be("{ status : \"D\" }");
        }

        [Fact]
        public void Example_10()
        {
            // db.inventory.find( { status: { $in: [ "A", "D" ] } } )

            // Start Example 10
            var filter = Builders<BsonDocument>.Filter.In("status", new[] { "A", "D" });
            var result = collection.Find(filter).ToList();
            // End Example 10

            Render(filter).Should().Be("{ status: { $in: [ \"A\", \"D\" ] } }");
        }

        [Fact]
        public void Example_11()
        {
            // db.inventory.find( { status: "A", qty: { $lt: 30 } } )

            // Start Example 11
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(builder.Eq("status", "A"), builder.Lt("qty", 30));
            var result = collection.Find(filter).ToList();
            // End Example 11

            Render(filter).Should().Be("{ status: \"A\", qty: { $lt: 30 } }");
        }

        [Fact]
        public void Example_12()
        {
            // db.inventory.find( { $or: [ { status: "A" }, { qty: { $lt: 30 } } ] } )

            // Start Example 12
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Or(builder.Eq("status", "A"), builder.Lt("qty", 30));
            var result = collection.Find(filter).ToList();
            // End Example 12

            Render(filter).Should().Be("{ $or: [ { status: \"A\" }, { qty: { $lt: 30 } } ] }");
        }

        [Fact]
        public void Example_13()
        {
            // db.inventory.find( { status: "A", $or: [ { qty: { $lt: 30 } }, { item: /^p/ } ] } )

            // Start Example 13
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(
                builder.Eq("status", "A"),
                builder.Or(builder.Lt("qty", 30), builder.Regex("item", new BsonRegularExpression("^p"))));
            var result = collection.Find(filter).ToList();
            // End Example 13

            Render(filter).Should().Be("{ status: \"A\", $or: [ { qty: { $lt: 30 } }, { item: /^p/ } ] }");
        }

        [Fact]
        public void Example_14()
        {
            // db.inventory.insertMany( [ 
            //   { item: "journal", qty: 25, size: { h: 14, w: 21, uom: "cm" }, status: "A" },
            //   { item: "notebook", qty: 50, size: { h: 8.5, w: 11, uom: "in" }, status: "A" },
            //   { item: "paper", qty: 100, size: { h: 8.5, w: 11, uom: "in" }, status: "D" },
            //   { item: "planner", qty: 75, size: { h: 22.85, w: 30, uom: "cm" }, status: "D" },
            //   { item: "postcard", qty: 45, size: { h: 10, w: 15.25, uom: "cm" }, status: "A" } ]);

            // Start Example 14
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "qty", 50 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "qty", 100 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "qty", 75 },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30 }, { "uom", "cm" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "qty", 45 },
                    { "size", new BsonDocument { { "h", 10 }, { "w", 15.25 }, { "uom", "cm" } } },
                    { "status", "A" } },
            };
            collection.InsertMany(documents);
            // End Example 14

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"journal\", qty: 25, size: { h: 14, w: 21, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"notebook\", qty: 50, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"A\" }",
                "{ item: \"paper\", qty: 100, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"D\" }",
                "{ item: \"planner\", qty: 75, size: { h: 22.85, w: 30, uom: \"cm\" }, status: \"D\" }",
                "{ item: \"postcard\", qty: 45, size: { h: 10, w: 15.25, uom: \"cm\" }, status: \"A\" }"));
        }

        [Fact]
        public void Example_15()
        {
            // db.inventory.find( { size: { h: 14, w: 21, uom: "cm" } } )

            // Start Example 15
            var filter = Builders<BsonDocument>.Filter.Eq("size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } });
            var result = collection.Find(filter).ToList();
            // End Example 15

            Render(filter).Should().Be("{ size: { h: 14, w: 21, uom: \"cm\" } }");
        }

        [Fact]
        public void Example_16()
        {
            // db.inventory.find( { size: { w: 21, h: 14, uom: "cm" } } )

            // Start Example 16
            var filter = Builders<BsonDocument>.Filter.Eq("size", new BsonDocument { { "w", 21 }, { "h", 14 }, { "uom", "cm" } });
            var result = collection.Find(filter).ToList();
            // End Example 16

            Render(filter).Should().Be("{ size: { w: 21, h: 14, uom: \"cm\" } }");
        }

        [Fact]
        public void Example_17()
        {
            // db.inventory.find( { "size.uom": "in" } )

            // Start Example 17
            var filter = Builders<BsonDocument>.Filter.Eq("size.uom", "in");
            var result = collection.Find(filter).ToList();
            // End Example 17

            Render(filter).Should().Be("{ \"size.uom\": \"in\" }");
        }

        [Fact]
        public void Example_18()
        {
            // db.inventory.find( { "size.h": { $lt: 15 } } )

            // Start Example 18
            var filter = Builders<BsonDocument>.Filter.Lt("size.h", 15);
            var result = collection.Find(filter).ToList();
            // End Example 18

            Render(filter).Should().Be("{ \"size.h\": { $lt: 15 } }");
        }

        [Fact]
        public void Example_19()
        {
            // db.inventory.find( { "size.h": { $lt: 15 }, "size.uom": "in", status: "D" } )

            // Start Example 19
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(builder.Lt("size.h", 15), builder.Eq("size.uom", "in"), builder.Eq("status", "D"));
            var result = collection.Find(filter).ToList();
            // End Example 19

            Render(filter).Should().Be("{ \"size.h\": { $lt: 15 }, \"size.uom\": \"in\", status: \"D\" }");
        }

        [Fact]
        public void Example_20()
        {
            // db.inventory.insertMany([ 
            //   { item: "journal", qty: 25, tags: ["blank", "red"], dim_cm: [ 14, 21 ] },
            //   { item: "notebook", qty: 50, tags: ["red", "blank"], dim_cm: [ 14, 21 ] },
            //   { item: "paper", qty: 100, tags: ["red", "blank", "plain"], dim_cm: [ 14, 21 ] },
            //   { item: "planner", qty: 75, tags: ["blank", "red"], dim_cm: [ 22.85, 30 ] },
            //   { item: "postcard", qty: 45, tags: ["blue"], dim_cm: [ 10, 15.25 ] } ]);

            // Start Example 20
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "tags", new BsonArray { "blank", "red" } },
                    { "dim_cm", new BsonArray { 14, 21 } }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "qty", 50 },
                    { "tags", new BsonArray { "red", "blank" } },
                    { "dim_cm", new BsonArray { 14, 21 } }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "qty", 100 },
                    { "tags", new BsonArray { "red", "blank", "plain" } },
                    { "dim_cm", new BsonArray { 14, 21 } }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "qty", 75 },
                    { "tags", new BsonArray { "blank", "red" } },
                    { "dim_cm", new BsonArray { 22.85, 30 } }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "qty", 45 },
                    { "tags", new BsonArray { "blue" } },
                    { "dim_cm", new BsonArray { 10, 15.25 } }
                }
            };
            collection.InsertMany(documents);
            // End Example 20

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"journal\", qty: 25, tags: [\"blank\", \"red\"], dim_cm: [ 14, 21 ] }",
                "{ item: \"notebook\", qty: 50, tags: [\"red\", \"blank\"], dim_cm: [ 14, 21 ] }",
                "{ item: \"paper\", qty: 100, tags: [\"red\", \"blank\", \"plain\"], dim_cm: [ 14, 21 ] }",
                "{ item: \"planner\", qty: 75, tags: [\"blank\", \"red\"], dim_cm: [ 22.85, 30 ] }",
                "{ item: \"postcard\", qty: 45, tags: [\"blue\"], dim_cm: [ 10, 15.25 ] }"));
        }

        [Fact]
        public void Example_21()
        {
            // db.inventory.find( { tags: ["red", "blank"] } )

            // Start Example 21
            var filter = Builders<BsonDocument>.Filter.Eq("tags", new[] { "red", "blank" });
            var result = collection.Find(filter).ToList();
            // End Example 21

            Render(filter).Should().Be("{ tags: [\"red\", \"blank\"] }");
        }

        [Fact]
        public void Example_22()
        {
            // db.inventory.find( { tags: { $all: ["red", "blank"] } } )

            // Start Example 22
            var filter = Builders<BsonDocument>.Filter.All("tags", new[] { "red", "blank" });
            var result = collection.Find(filter).ToList();
            // End Example 22

            Render(filter).Should().Be("{ tags: { $all: [\"red\", \"blank\"] } }");
        }

        [Fact]
        public void Example_23()
        {
            // db.inventory.find( { tags: "red" } )

            // Start Example 23
            var filter = Builders<BsonDocument>.Filter.Eq("tags", "red");
            var result = collection.Find(filter).ToList();
            // End Example 23

            Render(filter).Should().Be("{ tags: \"red\" }");
        }

        [Fact]
        public void Example_24()
        {
            // db.inventory.find( { dim_cm: { $gt: 25 } } )

            // Start Example 24
            var filter = Builders<BsonDocument>.Filter.Gt("dim_cm", 25);
            var result = collection.Find(filter).ToList();
            // End Example 24

            Render(filter).Should().Be("{ dim_cm: { $gt: 25 } }");
        }

        [Fact]
        public void Example_25()
        {
            // db.inventory.find( { dim_cm: { $gt: 15, $lt: 20 } } )

            // Start Example 25
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(builder.Gt("dim_cm", 15), builder.Lt("dim_cm", 20));
            var result = collection.Find(filter).ToList();
            // End Example 25

            Render(filter).Should().Be("{ dim_cm: { $gt: 15, $lt: 20 } }");
        }

        [Fact]
        public void Example_26()
        {
            // db.inventory.find( { dim_cm: { $elemMatch: { $gt: 22, $lt: 30 } } } )

            // Start Example 26
            var filter = Builders<BsonDocument>.Filter.ElemMatch<BsonValue>("dim_cm", new BsonDocument { { "$gt", 22 }, { "$lt", 30 } });
            var result = collection.Find(filter).ToList();
            // End Example 26

            Render(filter).Should().Be("{ dim_cm: { $elemMatch: { $gt: 22, $lt: 30 } } }");
        }

        [Fact]
        public void Example_27()
        {
            // db.inventory.find( { "dim_cm.1": { $gt: 25 } } )

            // Start Example 27
            var filter = Builders<BsonDocument>.Filter.Gt("dim_cm.1", 25);
            var result = collection.Find(filter).ToList();
            // End Example 27

            Render(filter).Should().Be("{ \"dim_cm.1\": { $gt: 25 } }");
        }

        [Fact]
        public void Example_28()
        {
            // db.inventory.find( { "tags": { $size: 3 } } )

            // Start Example 28
            var filter = Builders<BsonDocument>.Filter.Size("tags", 3);
            var result = collection.Find(filter).ToList();
            // End Example 28

            Render(filter).Should().Be("{ tags: { $size: 3 } }");
        }

        [Fact]
        public void Example_29()
        {
            // db.inventory.insertMany( [ 
            //   { item: "journal", instock: [ { warehouse: "A", qty: 5 }, { warehouse: "C", qty: 15 } ] },
            //   { item: "notebook", instock: [ { warehouse: "C", qty: 5 } ] },
            //   { item: "paper", instock: [ { warehouse: "A", qty: 60 }, { warehouse: "B", qty: 15 } ] },
            //   { item: "planner", instock: [ { warehouse: "A", qty: 40 }, { warehouse: "B", qty: 5 } ] },
            //   { item: "postcard", instock: [ { warehouse: "B", qty: 15 }, { warehouse: "C", qty: 35 } ] } ]);

            // Start Example 29
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 5 } },
                            new BsonDocument { { "warehouse", "C" }, { "qty", 15 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "C" }, { "qty", 5 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 60 } },
                            new BsonDocument { { "warehouse", "B" }, { "qty", 15 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 40 } },
                            new BsonDocument { { "warehouse", "B" }, { "qty", 5 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "B" }, { "qty", 15 } },
                            new BsonDocument { { "warehouse", "C" }, { "qty", 35 } } }
                        }
                }
            };
            collection.InsertMany(documents);
            // End Example 29

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"journal\", instock: [ { warehouse: \"A\", qty: 5 }, { warehouse: \"C\", qty: 15 } ] }",
                "{ item: \"notebook\", instock: [ { warehouse: \"C\", qty: 5 } ] }",
                "{ item: \"paper\", instock: [ { warehouse: \"A\", qty: 60 }, { warehouse: \"B\", qty: 15 } ] }",
                "{ item: \"planner\", instock: [ { warehouse: \"A\", qty: 40 }, { warehouse: \"B\", qty: 5 } ] }",
                "{ item: \"postcard\", instock: [ { warehouse: \"B\", qty: 15 }, { warehouse: \"C\", qty: 35 } ] }"));
        }

        [Fact]
        public void Example_30()
        {
            // db.inventory.find( { "instock": { warehouse: "A", qty: 5 } } )

            // Start Example 30
            var filter = Builders<BsonDocument>.Filter.AnyEq("instock", new BsonDocument { { "warehouse", "A" }, { "qty", 5 } });
            var result = collection.Find(filter).ToList();
            // End Example 30

            Render(filter).Should().Be("{ instock: { warehouse: \"A\", qty: 5 } }");
        }

        [Fact]
        public void Example_31()
        {
            // db.inventory.find( { "instock": { qty: 5, warehouse: "A" } } )

            // Start Example 31
            var filter = Builders<BsonDocument>.Filter.AnyEq("instock", new BsonDocument { { "qty", 5 }, { "warehouse", "A" } });
            var result = collection.Find(filter).ToList();
            // End Example 31

            Render(filter).Should().Be("{ instock: { qty: 5, warehouse: \"A\" } }");
        }

        [Fact]
        public void Example_32()
        {
            // db.inventory.find( { 'instock.0.qty': { $lte: 20 } } )

            // Start Example 32
            var filter = Builders<BsonDocument>.Filter.Lte("instock.0.qty", 20);
            var result = collection.Find(filter).ToList();
            // End Example 32

            Render(filter).Should().Be("{ \"instock.0.qty\": { $lte: 20 } }");
        }

        [Fact]
        public void Example_33()
        {
            // db.inventory.find( { 'instock.qty': { $lte: 20 } } )

            // Start Example 33
            var filter = Builders<BsonDocument>.Filter.Lte("instock.qty", 20);
            var result = collection.Find(filter).ToList();
            // End Example 33

            Render(filter).Should().Be("{ \"instock.qty\": { $lte: 20 } }");
        }

        [Fact]
        public void Example_34()
        {
            // db.inventory.find( { "instock": { $elemMatch: { qty: 5, warehouse: "A" } } } )

            // Start Example 34
            var filter = Builders<BsonDocument>.Filter.ElemMatch<BsonValue>("instock", new BsonDocument { { "qty", 5 }, { "warehouse", "A" } });
            var result = collection.Find(filter).ToList();
            // End Example 34

            Render(filter).Should().Be("{ instock: { $elemMatch: { qty: 5, warehouse: \"A\" } } }");
        }

        [Fact]
        public void Example_35()
        {
            // db.inventory.find( { "instock": { $elemMatch: { qty: { $gt: 10, $lte: 20 } } } } )

            // Start Example 35
            var filter = Builders<BsonDocument>.Filter.ElemMatch<BsonValue>("instock", new BsonDocument { { "qty", new BsonDocument { { "$gt", 10 }, { "$lte", 20 } } } });
            var result = collection.Find(filter).ToList();
            // End Example 35

            Render(filter).Should().Be("{ instock: { $elemMatch: { qty: { $gt: 10, $lte: 20 } } } }");
        }

        [Fact]
        public void Example_36()
        {
            // db.inventory.find( { "instock.qty": { $gt: 10, $lte: 20 } } )

            // Start Example 36
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(builder.Gt("instock.qty", 10), builder.Lte("instock.qty", 20));
            var result = collection.Find(filter).ToList();
            // End Example 36

            Render(filter).Should().Be("{ \"instock.qty\": { $gt: 10, $lte: 20 } }");
        }

        [Fact]
        public void Example_37()
        {
            // db.inventory.find( { "instock.qty": 5, "instock.warehouse": "A" } )

            // Start Example 37
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(builder.Eq("instock.qty", 5), builder.Eq("instock.warehouse", "A"));
            var result = collection.Find(filter).ToList();
            // End Example 37

            Render(filter).Should().Be("{ \"instock.qty\": 5, \"instock.warehouse\": \"A\" }");
        }

        [Fact]
        public void Example_38()
        {
            // db.inventory.insertMany([ { _id: 1, item: null }, { _id: 2 } ])

            // Start Example 38
            var documents = new[]
            {
                new BsonDocument { { "_id", 1 }, { "item", BsonNull.Value } },
                new BsonDocument { { "_id", 2 } }
            };
            collection.InsertMany(documents);
            // End Example 38

            var result = collection.Find("{}").ToList();
            result.Should().Equal(ParseMultiple(
                "{ _id: 1, item: null }",
                "{ _id: 2 }"));
        }

        [Fact]
        public void Example_39()
        {
            // db.inventory.find( { item: null } )

            // Start Example 39
            var filter = Builders<BsonDocument>.Filter.Eq("item", BsonNull.Value);
            var result = collection.Find(filter).ToList();
            // End Example 39

            Render(filter).Should().Be("{ item: null }");
        }

        [Fact]
        public void Example_40()
        {
            // db.inventory.find( { item : { $type: 10 } } )

            // Start Example 40
            var filter = Builders<BsonDocument>.Filter.Type("item", BsonType.Null);
            var result = collection.Find(filter).ToList();
            // End Example 40

            Render(filter).Should().Be("{ item : { $type: 10 } }");
        }

        [Fact]
        public void Example_41()
        {
            // db.inventory.find( { item : { $exists: false } } )

            // Start Example 41
            var filter = Builders<BsonDocument>.Filter.Exists("item", false);
            var result = collection.Find(filter).ToList();
            // End Example 41

            Render(filter).Should().Be("{ item : { $exists: false } }");
        }

        [Fact]
        public void Example_42()
        {
            // db.inventory.insertMany( [ 
            //   { item: "journal", status: "A", size: { h: 14, w: 21, uom: "cm" }, instock: [ { warehouse: "A", qty: 5 } ] },
            //   { item: "notebook", status: "A", size: { h: 8.5, w: 11, uom: "in" }, instock: [ { warehouse: "C", qty: 5 } ] },
            //   { item: "paper", status: "D", size: { h: 8.5, w: 11, uom: "in" }, instock: [ { warehouse: "A", qty: 60 } ] },
            //   { item: "planner", status: "D", size: { h: 22.85, w: 30, uom: "cm" }, instock: [ { warehouse: "A", qty: 40 } ] },
            //   { item: "postcard", status: "A", size: { h: 10, w: 15.25, uom: "cm" }, instock: [ { warehouse: "B", qty: 15 }, { warehouse: "C", qty: 35 } ] } ]);

            // Start Example 42
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "status", "A" },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } } },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 5 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "status", "A" },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "C" }, { "qty", 5 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "status", "D" },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 60 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "status", "D" },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30 }, { "uom", "cm" } } },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "A" }, { "qty", 40 } } }
                        }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "status", "A" },
                    { "size", new BsonDocument { { "h", 10 }, { "w", 15.25 }, { "uom", "cm" } } },
                    { "instock", new BsonArray
                        {
                            new BsonDocument { { "warehouse", "B" }, { "qty", 15 } },
                            new BsonDocument { { "warehouse", "C" }, { "qty", 35 } } }
                        }
                }
            };
            collection.InsertMany(documents);
            // End Example 42

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"journal\", status: \"A\", size: { h: 14, w: 21, uom: \"cm\" }, instock: [ { warehouse: \"A\", qty: 5 } ] }",
                "{ item: \"notebook\", status: \"A\", size: { h: 8.5, w: 11, uom: \"in\" }, instock: [ { warehouse: \"C\", qty: 5 } ] }",
                "{ item: \"paper\", status: \"D\", size: { h: 8.5, w: 11, uom: \"in\" }, instock: [ { warehouse: \"A\", qty: 60 } ] }",
                "{ item: \"planner\", status: \"D\", size: { h: 22.85, w: 30, uom: \"cm\" }, instock: [ { warehouse: \"A\", qty: 40 } ] }",
                "{ item: \"postcard\", status: \"A\", size: { h: 10, w: 15.25, uom: \"cm\" }, instock: [ { warehouse: \"B\", qty: 15 }, { warehouse: \"C\", qty: 35 } ] }"));

        }

        [Fact]
        public void Example_43()
        {
            // db.inventory.find( { status: "A" } )

            // Start Example 43
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var result = collection.Find(filter).ToList();
            // End Example 43

            Render(filter).Should().Be("{ status: \"A\" }");
        }

        [Fact]
        public void Example_44()
        {
            // db.inventory.find( { status: "A" }, { item: 1, status: 1 } )

            // Start Example 44
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Include("item").Include("status");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 44

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ item: 1, status: 1 }");
        }

        [Fact]
        public void Example_45()
        {
            // db.inventory.find( { status: "A" }, { item: 1, status: 1, _id: 0 } )

            // Start Example 45
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Include("item").Include("status").Exclude("_id");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 45

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ item: 1, status: 1, _id: 0 }");
        }

        [Fact]
        public void Example_46()
        {
            // db.inventory.find( { status: "A" }, { status: 0, instock: 0 } )

            // Start Example 46
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Exclude("status").Exclude("instock");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 46

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ status: 0, instock: 0 }");
        }

        [Fact]
        public void Example_47()
        {
            // db.inventory.find( { status: "A" }, { item: 1, status: 1, "size.uom": 1 } )

            // Start Example 47
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Include("item").Include("status").Include("size.uom");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 47

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ item: 1, status: 1, \"size.uom\": 1 }");
        }

        [Fact]
        public void Example_48()
        {
            // db.inventory.find( { status: "A" }, { "size.uom": 0 } )

            // Start Example 48
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Exclude("size.uom");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 48

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ \"size.uom\": 0 }");
        }

        [Fact]
        public void Example_49()
        {
            // db.inventory.find( { status: "A" }, { item: 1, status: 1, "instock.qty": 1 } )

            // Start Example 49
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Include("item").Include("status").Include("instock.qty");
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 49

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ item: 1, status: 1, \"instock.qty\": 1 }");
        }

        [Fact]
        public void Example_50()
        {
            // db.inventory.find( { status: "A" }, { item: 1, status: 1, instock: { $slice: -1 } } )

            // Start Example 50
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var projection = Builders<BsonDocument>.Projection.Include("item").Include("status").Slice("instock", -1);
            var result = collection.Find<BsonDocument>(filter).Project(projection).ToList();
            // End Example 50

            Render(filter).Should().Be("{ status: \"A\" }");
            Render(projection).Should().Be("{ item: 1, status: 1, instock: { $slice: -1 } }");
        }

        [Fact]
        public void Example_51()
        {
            // db.inventory.insertMany( [ 
            //   { item: "canvas", qty: 100, size: { h: 28, w: 35.5, uom: "cm" }, status: "A" },
            //   { item: "journal", qty: 25, size: { h: 14, w: 21, uom: "cm" }, status: "A" },
            //   { item: "mat", qty: 85, size: { h: 27.9, w: 35.5, uom: "cm" }, status: "A" },
            //   { item: "mousepad", qty: 25, size: { h: 19, w: 22.85, uom: "cm" }, status: "P" },
            //   { item: "notebook", qty: 50, size: { h: 8.5, w: 11, uom: "in" }, status: "P" },
            //   { item: "paper", qty: 100, size: { h: 8.5, w: 11, uom: "in" }, status: "D" },
            //   { item: "planner", qty: 75, size: { h: 22.85, w: 30, uom: "cm" }, status: "D" },
            //   { item: "postcard", qty: 45, size: { h: 10, w: 15.25, uom: "cm" }, status: "A" },
            //   { item: "sketchbook", qty: 80, size: { h: 14, w: 21, uom: "cm" }, status: "A" },
            //   { item: "sketch pad", qty: 95, size: { h: 22.85, w: 30.5, uom: "cm" }, status: "A" } ]); 

            // Start Example 51
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "canvas" },
                    { "qty", 100 },
                    { "size", new BsonDocument { { "h", 28 }, { "w", 35.5 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "mat" },
                    { "qty", 85 },
                    { "size", new BsonDocument { { "h", 27.9 }, { "w", 35.5 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "mousepad" },
                    { "qty", 25 },
                    { "size", new BsonDocument { { "h", 19 }, { "w", 22.85 }, { "uom", "cm" } } },
                    { "status", "P" }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "qty", 50 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "P" } },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "qty", 100 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "qty", 75 },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30 }, { "uom", "cm" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "qty", 45 },
                    { "size", new BsonDocument { { "h", 10 }, { "w", 15.25 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "sketchbook" },
                    { "qty", 80 },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "sketch pad" },
                    { "qty", 95 },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30.5 }, { "uom", "cm" } } }, { "status", "A" } },
            };
            collection.InsertMany(documents);
            // End Example 51

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"canvas\", qty: 100, size: { h: 28, w: 35.5, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"journal\", qty: 25, size: { h: 14, w: 21, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"mat\", qty: 85, size: { h: 27.9, w: 35.5, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"mousepad\", qty: 25, size: { h: 19, w: 22.85, uom: \"cm\" }, status: \"P\" }",
                "{ item: \"notebook\", qty: 50, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"P\" }",
                "{ item: \"paper\", qty: 100, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"D\" }",
                "{ item: \"planner\", qty: 75, size: { h: 22.85, w: 30, uom: \"cm\" }, status: \"D\" }",
                "{ item: \"postcard\", qty: 45, size: { h: 10, w: 15.25, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"sketchbook\", qty: 80, size: { h: 14, w: 21, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"sketch pad\", qty: 95, size: { h: 22.85, w: 30.5, uom: \"cm\" }, status: \"A\" }"));
        }

        [Fact]
        public void Example_52()
        {
            // db.inventory.updateOne( { item: "paper" }, { $set: { "size.uom": "cm", status: "P" }, $currentDate: { lastModified: true } } )

            // Start Example 52
            var filter = Builders<BsonDocument>.Filter.Eq("item", "paper");
            var update = Builders<BsonDocument>.Update.Set("size.uom", "cm").Set("status", "P").CurrentDate("lastModified");
            var result = collection.UpdateOne(filter, update);
            // End Example 52

            Render(filter).Should().Be("{ item: \"paper\" }");
            Render(update).Should().Be("{ $set: { \"size.uom\": \"cm\", status: \"P\" }, $currentDate: { lastModified: true } }");
        }

        [Fact]
        public void Example_53()
        {
            // db.inventory.updateMany( { "qty": { $lt: 50 } }, { $set: { "size.uom": "in", status: "P" }, $currentDate: { lastModified: true } } )

            // Start Example 53
            var filter = Builders<BsonDocument>.Filter.Lt("qty", 50);
            var update = Builders<BsonDocument>.Update.Set("size.uom", "in").Set("status", "P").CurrentDate("lastModified");
            var result = collection.UpdateMany(filter, update);
            // End Example 53

            Render(filter).Should().Be("{ qty: { $lt: 50 } }");
            Render(update).Should().Be("{ $set: { \"size.uom\": \"in\", status: \"P\" }, $currentDate: { lastModified: true } }");
        }

        [Fact]
        public void Example_54()
        {
            // db.inventory.replaceOne( { item: "paper" }, { item: "paper", instock: [ { warehouse: "A", qty: 60 }, { warehouse: "B", qty: 40 } ] } )

            // Start Example 54
            var filter = Builders<BsonDocument>.Filter.Eq("item", "paper");
            var replacement = new BsonDocument
            {
                { "item", "paper" },
                { "instock", new BsonArray
                    {
                        new BsonDocument { { "warehouse", "A" }, { "qty", 60 } },
                        new BsonDocument { { "warehouse", "B" }, { "qty", 40 } } }
                    }
            };
            var result = collection.ReplaceOne(filter, replacement);
            // End Example 54

            Render(filter).Should().Be("{ item: \"paper\" }");
            replacement.Should().Be("{ item: \"paper\", instock: [ { warehouse: \"A\", qty: 60 }, { warehouse: \"B\", qty: 40 } ] }");
        }

        [Fact]
        public void Example_55()
        {
            // db.inventory.insertMany( [ 
            //   { item: "journal", qty: 25, size: { h: 14, w: 21, uom: "cm" }, status: "A" },
            //   { item: "notebook", qty: 50, size: { h: 8.5, w: 11, uom: "in" }, status: "P" },
            //   { item: "paper", qty: 100, size: { h: 8.5, w: 11, uom: "in" }, status: "D" },
            //   { item: "planner", qty: 75, size: { h: 22.85, w: 30, uom: "cm" }, status: "D" },
            //   { item: "postcard", qty: 45, size: { h: 10, w: 15.25, uom: "cm" }, status: "A" }, ]); 

            // Start Example 55
            var documents = new[]
            {
                new BsonDocument
                {
                    { "item", "journal" },
                    { "qty", 25 },
                    { "size", new BsonDocument { { "h", 14 }, { "w", 21 }, { "uom", "cm" } } },
                    { "status", "A" }
                },
                new BsonDocument
                {
                    { "item", "notebook" },
                    { "qty", 50 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "P" }
                },
                new BsonDocument
                {
                    { "item", "paper" },
                    { "qty", 100 },
                    { "size", new BsonDocument { { "h", 8.5 }, { "w", 11 }, { "uom", "in" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "planner" },
                    { "qty", 75 },
                    { "size", new BsonDocument { { "h", 22.85 }, { "w", 30 }, { "uom", "cm" } } },
                    { "status", "D" }
                },
                new BsonDocument
                {
                    { "item", "postcard" },
                    { "qty", 45 },
                    { "size", new BsonDocument { { "h", 10 }, { "w", 15.25 }, { "uom", "cm" } } },
                    { "status", "A" }
                }
            };
            collection.InsertMany(documents);
            // End Example 55

            var result = collection.Find("{}").ToList();
            RemoveIds(result);
            result.Should().Equal(ParseMultiple(
                "{ item: \"journal\", qty: 25, size: { h: 14, w: 21, uom: \"cm\" }, status: \"A\" }",
                "{ item: \"notebook\", qty: 50, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"P\" }",
                "{ item: \"paper\", qty: 100, size: { h: 8.5, w: 11, uom: \"in\" }, status: \"D\" }",
                "{ item: \"planner\", qty: 75, size: { h: 22.85, w: 30, uom: \"cm\" }, status: \"D\" }",
                "{ item: \"postcard\", qty: 45, size: { h: 10, w: 15.25, uom: \"cm\" }, status: \"A\" }"));
        }

        [Fact]
        public void Example_56()
        {
            // db.inventory.deleteMany({})

            // Start Example 56
            var filter = Builders<BsonDocument>.Filter.Empty;
            var result = collection.DeleteMany(filter);
            // End Example 56

            Render(filter).Should().Be("{}");
        }

        [Fact]
        public void Example_57()
        {
            // db.inventory.deleteMany({ status : "A" })

            // Start Example 57
            var filter = Builders<BsonDocument>.Filter.Eq("status", "A");
            var result = collection.DeleteMany(filter);
            // End Example 57

            Render(filter).Should().Be("{ status : \"A\" }");
        }

        [Fact]
        public void Example_58()
        {
            // db.inventory.deleteOne({ status : "D" })

            // Start Example 58
            var filter = Builders<BsonDocument>.Filter.Eq("status", "D");
            var result = collection.DeleteOne(filter);
            // End Example 58

            Render(filter).Should().Be("{ status : \"D\" }");
        }

        [Fact]
        public void Aggregation_Example_1()
        {
            RequireServer.Check().Supports(Feature.Aggregate);

            //db.sales.aggregate([ 
            //    { $match : { "items.fruit":"banana" } },
            //    { $sort : { "date" : 1 } }
            //])

            // Start Aggregation Example 1
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Match(Builders<BsonDocument>.Filter.Eq("items.fruit", "banana"))
                .Sort(Builders<BsonDocument>.Sort.Ascending("date"));

            var cursor = collection.Aggregate(pipeline);
            // End Aggregation Example 1

            Render(pipeline).Should().Be("[{ $match : { \"items.fruit\" : \"banana\" } },   { $sort : { \"date\" : 1 } }]");
        }

        [Fact]
        public void Aggregation_Example_2()
        {
            RequireServer.Check().Supports(Feature.Aggregate);

            //db.sales.aggregate([
            //{
            //    $unwind: "$items"
            //},
            //{   $match: { "items.fruit" : "banana", }
            //},
            //{
            //    $group: { _id: { day: { $dayOfWeek: "$date" } }, count: { $sum: "$items.quantity" } }
            //},
            //{
            //    $project: { dayOfWeek: "$_id.day", numberSold: "$count", _id: 0 }
            //},
            //{
            //    $sort: { "numberSold": 1 }
            //}])

            // Start Aggregation Example 2
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Unwind("items")
                .Match(Builders<BsonDocument>.Filter.Eq("items.fruit", "banana"))
                .Group(new BsonDocument
                {
                    { "_id", new BsonDocument("day", new BsonDocument("$dayOfWeek", "$date")) },
                    { "count", new BsonDocument("$sum", "$items.quantity") },
                })
                .Project(new BsonDocument
                {
                    { "dayOfWeek", "$_id.day" },
                    { "numberSold", "$count" },
                    { "_id", 0 }
                })
                .Sort(Builders<BsonDocument>.Sort.Ascending("numberSold"));

            var cursor = collection.Aggregate(pipeline);
            // End Aggregation Example 2

            Render(pipeline).Should().Be(@"
                [{
                    $unwind : '$items'
                },
                {
                    $match : { 'items.fruit' : 'banana' }
                },
                {
                    $group : {
                        _id : { day : { $dayOfWeek : '$date' } },
                        count : { $sum : '$items.quantity' }
                    }
                },
                {
                    $project : { dayOfWeek : '$_id.day', numberSold : '$count', _id : 0 }
                },
                {
                    $sort : { 'numberSold' : 1 }
                }]");
        }

        [Fact]
        public void Aggregation_Example_3()
        {
            RequireServer.Check().Supports(Feature.Aggregate);

            //db.sales.aggregate([
            //{
            //    $unwind: "$items"
            //},
            //{
            //    $group: {
            //        _id: { day: { $dayOfWeek: "$date" } },
            //        items_sold: { $sum: "$items.quantity" },
            //        revenue: { $sum: { $multiply: ["$items.quantity", "$items.price"] } }
            //    }
            //},
            //{
            //    $project: { day: "$_id.day", revenue: 1, items_sold: 1, 
            //                discount: { $cond: { if : { $lte: ["$revenue", 250] }, then: 25, else : 0 }}
            //    }
            //}])

            // Start Aggregation Example 3
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .Unwind("items")
                .Group(new BsonDocument
                {
                    { "_id", new BsonDocument("day", new BsonDocument("$dayOfWeek", "$date")) },
                    { "items_sold", new BsonDocument("$sum", "$items.quantity") },
                    { "revenue", new BsonDocument("$sum", new BsonDocument("$multiply", new BsonArray { "$items.quantity", "$items.price" })) }
                })
                .Project(new BsonDocument
                {
                    { "day", "$_id.day" },
                    { "revenue", 1 },
                    { "items_sold", 1 },
                    { "discount",  new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$lte", new BsonArray { "$revenue", 250 }) },
                        { "then", 25 },
                        { "else", 0 }
                    })}
                });

            var cursor = collection.Aggregate(pipeline);
            // End Aggregation Example 3

            Render(pipeline).Should().Be(@"
                [{
                    $unwind : '$items'
                },
                {
                    $group : {
                        _id : {
                            day : { $dayOfWeek : '$date' }
                        },
                        items_sold : { $sum : '$items.quantity' },
                        revenue : {
                            $sum : { $multiply : ['$items.quantity', '$items.price'] }
                        }
                    }
                },
                {
                    $project : {
                        day : '$_id.day',
                        revenue : 1,
                        items_sold : 1,
                        discount : { $cond : { if : { $lte : ['$revenue', 250] }, then : 25, else : 0 } }
                    }
                }]");
        }

        [Fact]
        public void Aggregation_Example_4()
        {
            RequireServer.Check().Supports(Feature.AggregateLet);

            //db.air_alliances.aggregate( [
            //{
            //    $lookup:
            //    {
            //        from: "air_airlines",
            //        let: { constituents: "$airlines" },
            //        pipeline: [ {  $match: { $expr: { $in : [ "$name", "$$constituents" ]  } } } ],
            //        as : "airlines"
            //    }
            //},
            //{
            //    $project : {
            //        "_id" : 0,
            //        "name" : 1,
            //        airlines : { 
            //            $filter : {
            //                input : "$airlines",
            //                   as : "airline",
            //                 cond : { $eq: ["$$airline.country", "Canada"] }
            //            }
            //        }
            //    }
            //}
            //])

            // Start Aggregation Example 4
            var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage<BsonDocument, BsonDocument, BsonDocument>(new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "air_airlines"},
                    { "let", new BsonDocument("constituents", "$airlines")},
                    {
                        "pipeline",
                        new BsonArray
                        {
                            new BsonDocument("$match",
                                new BsonDocument("$expr",
                                    new BsonDocument("$in", new BsonArray { "$name", "$$constituents" })))
                        }
                    },
                    { "as", "airlines"}
                }))
                .Project(new BsonDocument
                {
                    { "_id", 0 },
                    { "name", 1 },
                    {
                        "airlines", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$airlines"},
                            { "as", "airline"},
                            {
                                "cond", new BsonDocument("$eq", new BsonArray { "$$airline.country", "Canada" })
                            }
                        })
                    }
                });

            var cursor = collection.Aggregate(pipeline);
            // End Aggregation Example 4

            Render(pipeline).Should().Be(@"
                [{
                    $lookup : {
                        from : 'air_airlines',
                        let : { constituents : '$airlines' },
                        pipeline : [{ $match : { $expr : { $in : ['$name', '$$constituents'] } } }],
                        as : 'airlines'
                    }
                },
                {
                    $project : {
                        _id : 0,
                        name : 1,
                        airlines : {
                            $filter : {
                                input : '$airlines',
                                as : 'airline',
                                cond : { $eq : ['$$airline.country', 'Canada'] }
                            }
                        }
                    }
                }]");
        }

        [Fact]
        public void RunCommand_Example_1()
        {
            //db.runCommand({buildInfo: 1})

            // Start runCommand Example 1
            var command = new JsonCommand<BsonDocument>("{ buildInfo : 1 }");
            var result = database.RunCommand(command);
            // End runCommand Example 1

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void RunCommand_Example_2()
        {
            //db.runCommand({collStats:"restaurants"})

            const string collectionName = "restaurants";
            if (database.ListCollectionNames().ToList().Any(c => c == collectionName))
            {
                database.DropCollection(collectionName);
            }
            database.CreateCollection(collectionName);
            
            // Start runCommand Example 2
            var command = new JsonCommand<BsonDocument>("{ collStats : 'restaurants' }");
            var result = database.RunCommand(command);
            // End runCommand Example 2

            database.DropCollection(collectionName);

            result["ok"].ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void Index_Example_1()
        {
            RequireServer.Check().Supports(Feature.CreateIndexesCommand);

            //db.records.createIndex( { score: 1 } )

            // Start Index Example 1
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("score");
            var indexModel = new CreateIndexModel<BsonDocument>(keys);
            var result = collection.Indexes.CreateOne(indexModel);
            // End Index Example 1

            result.Should().Be("score_1");
        }

        [Fact]
        public void Index_Example_2()
        {
            RequireServer.Check().Supports(Feature.PartialIndexes);

            //db.restaurants.createIndex(
            //{ cuisine: 1, name: 1 },
            //{ partialFilterExpression: { rating: { $gt: 5 } } }
            //)

            // Start Index Example 2
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("cuisine").Ascending("name");
            var indexOptions = new CreateIndexOptions<BsonDocument>
            {
                PartialFilterExpression = Builders<BsonDocument>.Filter.Gt(document => document["rating"], 5)
            };
            var indexModel = new CreateIndexModel<BsonDocument>(keys, indexOptions);
            var result = collection.Indexes.CreateOne(indexModel);
            // End Index Example 2

            Render(indexModel.Options.PartialFilterExpression).Should().Be("{ \"rating\" : { \"$gt\" : 5 } }");
            result.Should().Be("cuisine_1_name_1");
        }

        [Fact]
        public void Exploiting_The_Power_Of_Arrays_Example()
        {
            RequireServer.Check().Supports(Feature.ArrayFilters);

            var document = new BsonDocument
            {
                { "_id", 1 },
                { "a", new BsonArray { new BsonDocument("b", 0), new BsonDocument("b", 1) } }
            };

            var arrayUpdatesTestCollectionName = "arrayUpdatesTest";
            var testDatabase = client.GetDatabase("test");
            testDatabase.DropCollection(arrayUpdatesTestCollectionName);
            var arrayUpdatesTestCollection = testDatabase.GetCollection<BsonDocument>(arrayUpdatesTestCollectionName);
            arrayUpdatesTestCollection.InsertOne(document);

            // Start Exploiting The Power Of Arrays Example
            var collection = client
                .GetDatabase("test")
                .GetCollection<BsonDocument>("arrayUpdatesTest");

            collection.UpdateOne(
                Builders<BsonDocument>.Filter.Eq("_id", 1),
                Builders<BsonDocument>.Update.Set("a.$[i].b", 2),
                new UpdateOptions()
                {
                    ArrayFilters = new List<ArrayFilterDefinition<BsonValue>>()
                    {
                        "{ 'i.b' : 0 }"
                    }
                });
            // End Exploiting The Power Of Arrays Example

            var result = arrayUpdatesTestCollection.Find("{}").FirstOrDefault();
            result.Should().Be("{ \"_id\" : 1, \"a\" : [{ \"b\" : 2 }, { \"b\" : 1 }] }");
        }

        // private methods
        private IEnumerable<BsonDocument> ParseMultiple(params string[] documents)
        {
            return documents.Select(d => BsonDocument.Parse(d));
        }

        private BsonDocument Render(FilterDefinition<BsonDocument> filter)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<BsonDocument>();
            return filter.Render(serializer, registry);
        }

        private BsonDocument Render(ProjectionDefinition<BsonDocument, BsonDocument> projection)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<BsonDocument>();
            return projection.Render(serializer, registry).Document;
        }

        private BsonDocument Render(UpdateDefinition<BsonDocument> update)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<BsonDocument>();
            return update.Render(serializer, registry);
        }

        private BsonArray Render(PipelineDefinition<BsonDocument, BsonDocument> pipeline)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<BsonDocument>();
            var renderedPipeline = pipeline.Render(serializer, registry);
            return new BsonArray(renderedPipeline.Documents);
        }

        private void RemoveIds(IEnumerable<BsonDocument> documents)
        {
            foreach (var document in documents)
            {
                document.Remove("_id");
            }
        }
    }
}
