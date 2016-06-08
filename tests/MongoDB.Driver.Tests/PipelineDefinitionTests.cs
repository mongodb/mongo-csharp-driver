/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PipelineStagePipelineDefinitionTests
    {
        [Fact]
        public void Constructor_should_verify_the_inputs_and_outputs_of_the_stages_and_throw_when_invalid()
        {
            var stages = new IPipelineStageDefinition[] 
            {
                new BsonDocumentPipelineStageDefinition<Person, BsonDocument>(new BsonDocument()),
                new BsonDocumentPipelineStageDefinition<BsonDocument, Pet>(new BsonDocument()),
                new BsonDocumentPipelineStageDefinition<BsonDocument, Person>(new BsonDocument())
            };

            Action act = () => new PipelineStagePipelineDefinition<Person, Person>(stages);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_verify_the_inputs_and_outputs_of_the_stages()
        {
            var stages = new IPipelineStageDefinition[] 
            {
                new BsonDocumentPipelineStageDefinition<Person, BsonDocument>(new BsonDocument()),
                new BsonDocumentPipelineStageDefinition<BsonDocument, Pet>(new BsonDocument()),
                new BsonDocumentPipelineStageDefinition<Pet, Person>(new BsonDocument())
            };

            Action act = () => new PipelineStagePipelineDefinition<Person, Person>(stages);

            act.ShouldNotThrow<ArgumentException>();
        }

        private void Assert<TDocument>(ProjectionDefinition<TDocument> projection, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedProjection = projection.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedProjection.Should().Be(expectedJson);
        }

        private ProjectionDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new ProjectionDefinitionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }
        
            [BsonElement("pets")]
            public Pet[] Pets { get; set; }
        }

        private class Pet
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }    
    }
}
