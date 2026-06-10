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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests.Search;

public class CreateVectorSearchIndexModelTests
{
    [Fact]
    public void Rendering_with_Flat_and_hnsw_options_should_throw()
    {
        var model = new CreateVectorSearchIndexModel<Entity>(
            e => e.Floats, "idx", VectorSimilarity.Cosine, dimensions: 2)
        {
            IndexingMethod = VectorIndexingMethod.Flat,
            HnswMaxEdges = 32
        };

        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Entity>();
        var renderArgs = new RenderArgs<Entity>(serializer, BsonSerializer.SerializerRegistry);

        var exception = Record.Exception(() => model.Render(renderArgs));

        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Contain(nameof(CreateVectorSearchIndexModelBase<Entity>.IndexingMethod));
        exception.Message.Should().Contain(nameof(VectorIndexingMethod.Flat));
    }

    private class Entity
    {
        public float[] Floats { get; set; }
    }
}
