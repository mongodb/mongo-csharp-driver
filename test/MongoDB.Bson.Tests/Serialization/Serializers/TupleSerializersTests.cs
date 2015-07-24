/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    [TestFixture]
    public class TupleT1SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();

            var subject = new TupleSerializer<bool>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();

            var subject = new TupleSerializer<bool>(
                item1Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool>(
                false);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT2SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();

            var subject = new TupleSerializer<bool, int>(
                item1Serializer,
                item2Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int>(
                false, 1);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT3SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int, bool>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();

            var subject = new TupleSerializer<bool, int, bool>(
                item1Serializer,
                item2Serializer,
                item3Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool>(
                false, 1, true);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT4SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool, int> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int, bool, int>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(intSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item4Serializer = Substitute.For<IBsonSerializer<int>>();

            var subject = new TupleSerializer<bool, int, bool, int>(
                item1Serializer,
                item2Serializer,
                item3Serializer,
                item4Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(item4Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool, int>(
                false, 1, true, 2);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT5SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool, int, bool> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int, bool, int, bool>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(boolSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item4Serializer = Substitute.For<IBsonSerializer<int>>();
            var item5Serializer = Substitute.For<IBsonSerializer<bool>>();

            var subject = new TupleSerializer<bool, int, bool, int, bool>(
                item1Serializer,
                item2Serializer,
                item3Serializer,
                item4Serializer,
                item5Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(item4Serializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(item5Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool, int, bool>(
                false, 1, true, 2, false);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT6SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool, int, bool, int> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(intSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item4Serializer = Substitute.For<IBsonSerializer<int>>();
            var item5Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item6Serializer = Substitute.For<IBsonSerializer<int>>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int>(
                item1Serializer,
                item2Serializer,
                item3Serializer,
                item4Serializer,
                item5Serializer,
                item6Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(item4Serializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(item5Serializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(item6Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool, int, bool, int>(
                false, 1, true, 2, false, 3);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT7SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool, int, bool, int, bool> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int, bool>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item7Serializer, Is.SameAs(boolSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item4Serializer = Substitute.For<IBsonSerializer<int>>();
            var item5Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item6Serializer = Substitute.For<IBsonSerializer<int>>();
            var item7Serializer = Substitute.For<IBsonSerializer<bool>>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int, bool>(
                item1Serializer,
                item2Serializer,
                item3Serializer,
                item4Serializer,
                item5Serializer,
                item6Serializer,
                item7Serializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(item4Serializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(item5Serializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(item6Serializer));
            Assert.That(subject.Item7Serializer, Is.SameAs(item7Serializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3, true] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3, true] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool, int, bool, int, bool>(
                false, 1, true, 2, false, 3, true);
            return new C { Id = 1, T = tuple };
        }
    }

    [TestFixture]
    public class TupleT8SerializersTests
    {
        public class C
        {
            public int Id;
            public Tuple<bool, int, bool, int, bool, int, bool, Tuple<int>> T;

            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                var other = (C)obj;
                return Id == other.Id && object.Equals(T, other.T);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var boolSerializer = BsonSerializer.LookupSerializer<bool>();
            var intSerializer = BsonSerializer.LookupSerializer<int>();
            var restSerializer = BsonSerializer.LookupSerializer<Tuple<int>>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int, bool, Tuple<int>>();

            Assert.That(subject.Item1Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(intSerializer));
            Assert.That(subject.Item7Serializer, Is.SameAs(boolSerializer));
            Assert.That(subject.RestSerializer, Is.SameAs(restSerializer));
        }

        [Test]
        public void constructor_with_serializers_should_initialize_instance()
        {
            var item1Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item2Serializer = Substitute.For<IBsonSerializer<int>>();
            var item3Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item4Serializer = Substitute.For<IBsonSerializer<int>>();
            var item5Serializer = Substitute.For<IBsonSerializer<bool>>();
            var item6Serializer = Substitute.For<IBsonSerializer<int>>();
            var item7Serializer = Substitute.For<IBsonSerializer<bool>>();
            var restSerializer = Substitute.For<IBsonSerializer<Tuple<int>>>();

            var subject = new TupleSerializer<bool, int, bool, int, bool, int, bool, Tuple<int>>(
                item1Serializer,
                item2Serializer,
                item3Serializer,
                item4Serializer,
                item5Serializer,
                item6Serializer,
                item7Serializer,
                restSerializer);

            Assert.That(subject.Item1Serializer, Is.SameAs(item1Serializer));
            Assert.That(subject.Item2Serializer, Is.SameAs(item2Serializer));
            Assert.That(subject.Item3Serializer, Is.SameAs(item3Serializer));
            Assert.That(subject.Item4Serializer, Is.SameAs(item4Serializer));
            Assert.That(subject.Item5Serializer, Is.SameAs(item5Serializer));
            Assert.That(subject.Item6Serializer, Is.SameAs(item6Serializer));
            Assert.That(subject.Item7Serializer, Is.SameAs(item7Serializer));
            Assert.That(subject.RestSerializer, Is.SameAs(restSerializer));
        }

        [Test]
        public void Deserialize_should_deserialize_null()
        {
            var json = "{ \"_id\" : 1, \"T\" : null }";
            var expectedValue = new C { Id = 1, T = null };

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Deserialize_should_deserialize_value()
        {
            var json = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3, true, [4]] }";
            var expectedValue = CreateValue();

            var value = BsonSerializer.Deserialize<C>(json);

            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Serialize_should_serialize_null()
        {
            var value = new C { Id = 1, T = null };
            var expectedJson = "{ \"_id\" : 1, \"T\" : null }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Serialize_should_serialize_value()
        {
            var value = CreateValue();
            var expectedJson = "{ \"_id\" : 1, \"T\" : [false, 1, true, 2, false, 3, true, [4]] }";

            var json = value.ToJson();

            Assert.That(json, Is.EqualTo(expectedJson));
        }

        // helper methods
        private C CreateValue()
        {
            var tuple = new Tuple<bool, int, bool, int, bool, int, bool, Tuple<int>>(
                false, 1, true, 2, false, 3, true, new Tuple<int>(4));
            return new C { Id = 1, T = tuple };
        }
    }
}
