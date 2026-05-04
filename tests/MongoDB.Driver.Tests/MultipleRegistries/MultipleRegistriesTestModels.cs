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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Tests
{
    // ── Flat-hierarchy models ────────────────────────────────────────────────

    internal class Person
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    // ── Discriminated hierarchy (class-map registration) ─────────────────────

    internal class BasePerson
    {
        [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; }
        public int Age { get; set; }
    }

    internal class DerivedPerson1 : BasePerson
    {
        public string ExtraField1 { get; set; }
    }

    internal class DerivedPerson2 : BasePerson
    {
        public string ExtraField2 { get; set; }
    }

    // ── Discriminated hierarchy (attribute-based) ─────────────────────────────

    [BsonDiscriminator("bp", RootClass = true)]
    internal class BasePersonAttribute
    {
        [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [BsonDiscriminator("dp1")]
    internal class DerivedPersonAttribute1 : BasePersonAttribute
    {
        public string ExtraField1 { get; set; }
    }

    [BsonDiscriminator("dp2")]
    internal class DerivedPersonAttribute2 : BasePersonAttribute
    {
        public string ExtraField2 { get; set; }
    }

    // ── LINQ test models ──────────────────────────────────────────────────────

    internal class NumericModel
    {
        [BsonId] public ObjectId Id { get; set; }
        public double D { get; set; }
    }

    internal enum ApprovalState { Active = 1, Inactive = 2 }

    internal class EnumModel
    {
        [BsonId] public ObjectId Id { get; set; }
        [BsonRepresentation(BsonType.String)]
        public ApprovalState ApprovalState { get; set; }
    }

    // ── Provider-path models ──────────────────────────────────────────────────

    internal interface IAnimal
    {
        string Name { get; set; }
    }

    internal class Dog : IAnimal
    {
        public string Name { get; set; }
        public string Breed { get; set; }
    }

    [BsonSerializer(typeof(CustomAttributeSerializer))]
    internal class TypeWithAttributedSerializer
    {
        public string Value { get; set; }
    }

    internal class CustomAttributeSerializer : SerializerBase<TypeWithAttributedSerializer>
    {
        public override TypeWithAttributedSerializer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartDocument();
            context.Reader.ReadString();
            context.Reader.ReadEndDocument();
            return new TypeWithAttributedSerializer { Value = "deserialized" };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TypeWithAttributedSerializer value)
        {
            context.Writer.WriteStartDocument();
            context.Writer.WriteName("v");
            context.Writer.WriteString(value.Value ?? "");
            context.Writer.WriteEndDocument();
        }
    }

    [BsonSerializer(typeof(DomainAwareAttributeSerializer))]
    internal class TypeWithDomainAwareAttributedSerializer
    {
        public string Value { get; set; }
    }

    internal class DomainAwareAttributeSerializer : SerializerBase<TypeWithDomainAwareAttributedSerializer>, IHasSerializationDomain
    {
        private readonly IBsonSerializationDomain _serializationDomain;

        public DomainAwareAttributeSerializer()
            : this(BsonSerializationDomain.Default)
        {
        }

        internal DomainAwareAttributeSerializer(IBsonSerializationDomain serializationDomain)
        {
            _serializationDomain = serializationDomain;
        }

        IBsonSerializationDomain IHasSerializationDomain.SerializationDomain => _serializationDomain;

        public override TypeWithDomainAwareAttributedSerializer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartDocument();
            context.Reader.ReadString();
            context.Reader.ReadEndDocument();
            return new TypeWithDomainAwareAttributedSerializer { Value = "deserialized" };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TypeWithDomainAwareAttributedSerializer value)
        {
            context.Writer.WriteStartDocument();
            context.Writer.WriteName("v");
            context.Writer.WriteString(value.Value ?? "");
            context.Writer.WriteEndDocument();
        }
    }

    internal class ClassWithObjectMember
    {
        public ObjectId Id { get; set; }
        public object Data { get; set; }
    }

    // ── Shared serializer helpers ─────────────────────────────────────────────

    // Appends _suffix to every serialized string; strips it on deserialization.
    internal class CustomStringSerializer(string appended = "test")
        : SealedClassSerializerBase<string>
    {
        public override int GetHashCode() => 0;

        protected override string DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();
            return bsonType switch
            {
                BsonType.String => context.Reader.ReadString().Replace(appended, ""),
                _ => throw CreateCannotDeserializeFromBsonTypeException(bsonType)
            };
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            context.Writer.WriteString(value + appended);
        }
    }

    internal class CustomObjectIdGenerator : IIdGenerator
    {
        public object GenerateId(object container, object document)
            => ObjectId.Parse("6797b56bf5495bf53aa3078f");

        public bool IsEmpty(object id) => true;
    }
}
