﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Dynamic;
using System.Linq;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class ExpandoSerializerTests
    {
        public abstract class Shape
        {
        }

        public class Circle : Shape
        {
            public int Radius { get; set; }
        }

        public class Square : Shape
        {
            public int Side { get; set; }
        }

        public class TestClass
        {
            public List<Shape> Shapes { get; set; }
        }

        [Fact]
        public void TestRoundTrip()
        {
            dynamic person = new ExpandoObject();

            person.FirstName = "Jack";
            person.LastName = "McJack";
            dynamic hobby1 = new ExpandoObject();
            hobby1.Name = "hiking";
            person.Hobbies = new List<dynamic> { hobby1, 10 };
            person.Spouse = new ExpandoObject();
            person.Spouse.FirstName = "Jane";
            person.Spouse.LastName = "McJane";

            var json = ((ExpandoObject)person).ToJson();
            var expected = "{ 'FirstName' : 'Jack', 'LastName' : 'McJack', 'Hobbies' : [{ 'Name' : 'hiking' }, 10], 'Spouse' : { 'FirstName' : 'Jane', 'LastName' : 'McJane' } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = ((ExpandoObject)person).ToBson();
            var rehydrated = BsonSerializer.Deserialize<ExpandoObject>(bson);
            Assert.True(bson.SequenceEqual((rehydrated).ToBson()));
        }

#if NET45
        [Fact]
        public void TestDeserializingDiscriminatedVersion()
        {
            var oldJson = "{ 'FirstName' : 'Jack', 'LastName' : 'McJack', 'Hobbies' : { '_t' : 'System.Collections.Generic.List`1[System.Object]', '_v' : [{ '_t' : 'System.Dynamic.ExpandoObject', '_v' : { 'Name' : 'hiking' } }, 10] }, 'Spouse' : { '_t' : 'System.Dynamic.ExpandoObject', '_v' : { 'FirstName' : 'Jane', 'LastName' : 'McJane' } } }".Replace("'", "\"");
            var rehydrated = BsonSerializer.Deserialize<ExpandoObject>(oldJson);

            var json = rehydrated.ToJson();
            Assert.Equal(oldJson, json);
        }
#endif

        [Fact]
        public void TestDiscriminatedObjectShouldBeDeserializedAsDynamic()
        {
            var data = new TestClass
            {
                Shapes = new List<Shape>
                {
                    new Circle {Radius = 5},
                    new Square {Side = 3}
                }
            };

            var bson = data.ToBson();

            dynamic rehydrated = BsonSerializer.Deserialize<ExpandoObject>(bson);

            Assert.IsNotType<Circle>(rehydrated.Shapes[0]);
            Assert.IsNotType<Square>(rehydrated.Shapes[1]);

            Assert.IsType<ExpandoObject>(rehydrated.Shapes[0]);
            Assert.IsType<ExpandoObject>(rehydrated.Shapes[1]);
        }
    }
}
