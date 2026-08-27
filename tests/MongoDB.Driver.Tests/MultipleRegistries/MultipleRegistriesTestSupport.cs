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

namespace MongoDB.Driver.Tests;

internal static class MultipleRegistriesTestHelpers
{
    public static IMongoCollection<T> GetTypedCollection<T>(IMongoClient client) =>
        client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
            .GetCollection<T>(DriverTestConfiguration.CollectionNamespace.CollectionName);

    public static IMongoClient CreateClientWithDomain(IBsonSerializationDomain domain, bool dropCollection = true)
    {
        var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = domain);
        if (dropCollection)
        {
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
        }
        return client;
    }
}

internal class Person
{
    [BsonId] public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

internal class BasePerson
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public int Age { get; set; }
}

internal class NumericModel
{
    [BsonId] public ObjectId Id { get; set; }
    public double D { get; set; }
}

// Appends a suffix to every serialized string; strips it on deserialization.
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
