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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Builders;

internal class Example
{
    public void ExampleTest()
    {
        var domain = new SerializationDomainBuilder()
            .RegisterDiscriminator(typeof(P1), "myDiscriminator")
            .RegisterSerializer(new CustomSerializer())
            .UseNullIdChecker(true)
            .ConfigureClassMap(classMapBuilder =>
            {
                classMapBuilder
                    .RegisterClassMap<P2>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    })
                    .TryRegisterClassMap<P3>();
            })
            .ConfigureConventionRegistry(conventions =>
            {
                conventions.Register(
                    name: "camelCase",
                    conventions: new ConventionPack { new CamelCaseElementNameConvention() },
                    filter: t => true);
            })
            .ConfigureBsonDefaults(defaults =>
            {
                defaults
                    .SetMaxDocumentSize(10 * 1024 * 1024);
            })
            .Build();
    }
}

internal class P1;
internal class P2;

internal class P3;
internal class CustomSerializer : IBsonSerializer<P1>
{
    public P1 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => new P1();
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        throw new NotImplementedException();
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, P1 value) { }
    public Type ValueType => typeof(P1);
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}