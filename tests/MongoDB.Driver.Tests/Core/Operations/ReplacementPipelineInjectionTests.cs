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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    // CSHARP-6158: a replacement is only ever meant to be a document, but nothing between the public
    // ReplaceOne API and the wire enforces that. An attacker-supplied array reaches the server as the
    // update 'u' field, where an array means "aggregation pipeline" rather than "replacement document".
    public class ReplacementPipelineInjectionTests
    {
        private static BsonArray AttackerPipeline() =>
            new BsonArray
            {
                new BsonDocument("$set", new BsonDocument("role", "admin")),
                new BsonDocument("$unset", "auditTrail")
            };

        [Fact]
        public void UpdateRequest_should_reject_an_array_when_update_type_is_Update_but_accepts_it_for_Replacement()
        {
            // for a real update an array is legitimate (a pipeline update), and an empty one is rejected
            Action emptyPipelineUpdate = () => new UpdateRequest(UpdateType.Update, new BsonDocument(), new BsonArray());
            emptyPipelineUpdate.ShouldThrow<ArgumentException>();

            // for a Replacement, EnsureUpdateIsValid does nothing at all: an array is accepted, and so is a scalar
            Action arrayReplacement = () => new UpdateRequest(UpdateType.Replacement, new BsonDocument(), AttackerPipeline());
            Action scalarReplacement = () => new UpdateRequest(UpdateType.Replacement, new BsonDocument(), BsonValue.Create(42));

            arrayReplacement.ShouldNotThrow();
            scalarReplacement.ShouldNotThrow();
        }

        [Fact]
        public void ReplacementElementNameValidator_should_not_guard_child_content()
        {
            var subject = ElementNameValidatorFactory.ForUpdateType(UpdateType.Replacement);

            // a top-level operator is correctly rejected ...
            subject.IsValidElementName("$set").Should().BeFalse();

            // ... but array indexes are valid names, and children are handed a no-op validator,
            // so every pipeline-stage operator nested inside the array goes unchecked
            subject.IsValidElementName("0").Should().BeTrue();
            subject.GetValidatorForChildContent("0").Should().BeOfType<NoOpElementNameValidator>();
            subject.GetValidatorForChildContent("0").IsValidElementName("$set").Should().BeTrue();
        }

        [Fact]
        public void An_array_of_pipeline_stages_is_rejected_by_the_element_name_validator()
        {
            var request = new UpdateRequest(UpdateType.Replacement, new BsonDocument("_id", 1), AttackerPipeline());

            Action action = () => SerializeUpdateFieldTheSameWayTheOperationDoes(request);

            // Despite GetValidatorForChildContent returning a no-op validator, the pipeline is still blocked.
            // BsonWriter only swaps in the child validator when _useChildValidator is set, and that flag is set
            // by WriteName and then cleared again by PushElementNameValidator. SerializeUpdate pushes the
            // validator *after* writing the "u" name, so the flag is false and GetValidatorForChildContent is
            // never consulted. On top of that, BsonArraySerializer never calls WriteName for its items (array
            // indexes are emitted directly by WriteNameHelper), so nothing ever swaps the validator either.
            // The ReplacementElementNameValidator therefore stays in effect all the way into the array items,
            // where it rejects every "$"-prefixed stage name.
            action.ShouldThrow<BsonSerializationException>()
                .WithMessage("Element name '$set' is not valid'.");
        }

        [Fact]
        public void A_scalar_replacement_is_serialized_verbatim_as_the_wire_update_field()
        {
            var request = new UpdateRequest(UpdateType.Replacement, new BsonDocument("_id", 1), BsonValue.Create(42));

            // a scalar contains no element names at all, so no validator ever sees it and it reaches the wire
            var rendered = SerializeUpdateFieldTheSameWayTheOperationDoes(request);

            rendered["u"].BsonType.Should().Be(BsonType.Int32);
            rendered["u"].AsInt32.Should().Be(42);
        }

        [Fact]
        public void An_array_of_non_operator_documents_is_serialized_verbatim_as_the_wire_update_field()
        {
            // no "$" prefixes, so the validator is satisfied, yet the server still receives an array 'u'
            // and will interpret it as an aggregation pipeline rather than a replacement document
            var request = new UpdateRequest(
                UpdateType.Replacement,
                new BsonDocument("_id", 1),
                new BsonArray { new BsonDocument("role", "admin") });

            var rendered = SerializeUpdateFieldTheSameWayTheOperationDoes(request);

            rendered["u"].BsonType.Should().Be(BsonType.Array);
        }

        [Fact]
        public void ReplaceOneModel_should_reject_a_non_document_replacement()
        {
            Action array = () => new ReplaceOneModel<BsonValue>(new BsonDocument(), AttackerPipeline());
            Action scalar = () => new ReplaceOneModel<BsonValue>(new BsonDocument(), BsonValue.Create(42));

            array.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("replacement");
            scalar.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("replacement");
        }

        [Fact]
        public void BulkWriteReplaceOneModel_should_reject_a_non_document_replacement()
        {
            var ns = CollectionNamespace.FromFullName("db.coll");

            Action array = () => new BulkWriteReplaceOneModel<BsonValue>(ns, new BsonDocument(), AttackerPipeline());
            Action scalar = () => new BulkWriteReplaceOneModel<BsonValue>(ns, new BsonDocument(), BsonValue.Create(42));

            array.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("replacement");
            scalar.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("replacement");
        }

        [Fact]
        public void ReplaceOneModel_should_still_accept_a_document_replacement()
        {
            Action action = () => new ReplaceOneModel<BsonValue>(new BsonDocument(), new BsonDocument("a", 1));

            action.ShouldNotThrow();
        }

        // mirrors RetryableUpdateCommandOperation.UpdateRequestSerializer.SerializeUpdate
        private static BsonDocument SerializeUpdateFieldTheSameWayTheOperationDoes(UpdateRequest request)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                writer.WriteStartDocument();
                writer.WriteName("u");
                writer.PushElementNameValidator(ElementNameValidatorFactory.ForUpdateType(request.UpdateType));
                try
                {
                    BsonValueSerializer.Instance.Serialize(context, request.Update);
                }
                finally
                {
                    writer.PopElementNameValidator();
                }
                writer.WriteEndDocument();
            }

            return document;
        }
    }
}
