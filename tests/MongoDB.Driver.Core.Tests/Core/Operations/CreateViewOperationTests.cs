/* Copyright 2016 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateViewOperationTests : OperationTestBase
    {
        private readonly BsonDocument[] _pipeline = new[] { new BsonDocument("$match", new BsonDocument("x", 1)) };
        private readonly string _viewName = $"{nameof(CreateViewOperationTests)}-view";

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values("a", "b")]
            string viewName)
        {
            var subject = new CreateViewOperation(_databaseNamespace, viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.DatabaseNamespace.Should().BeSameAs(_databaseNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.Pipeline.Should().Equal(_pipeline);
            subject.ViewName.Should().Be(viewName);
            subject.ViewOn.Should().Be(_collectionNamespace.CollectionName);
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            var exception = Record.Exception(() => new CreateViewOperation(null, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_viewName_is_null()
        {
            var exception = Record.Exception(() => new CreateViewOperation(_databaseNamespace, null, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewName");
        }

        [Fact]
        public void constructor_should_throw_when_viewOn_is_null()
        {
            var exception = Record.Exception(() => new CreateViewOperation(_databaseNamespace, _viewName, null, _pipeline, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("viewOn");
        }

        [Fact]
        public void constructor_should_throw_when_pipeline_is_null()
        {
            var exception = Record.Exception(() => new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_view(
            [Values("a", "b")]
            string viewName,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Views);
            DropView(viewName);
            var subject = new CreateViewOperation(_databaseNamespace, viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings);

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetViewInfo(binding, viewName);
            }

            var options = info["options"].AsBsonDocument;
            options["viewOn"].AsString.Should().Be(_collectionNamespace.CollectionName);
            options["pipeline"].AsBsonArray.Cast<BsonDocument>().Should().Equal(_pipeline);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_view_when_Collation_is_set(
            [Values(null, "en_US")]
            string locale,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Views).VersionGreaterThanOrEqualTo("3.3.13");
            DropView(_viewName);
            var collation = locale == null ? null : new Collation(locale);
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetViewInfo(binding, _viewName);
            }

            var options = info["options"].AsBsonDocument;
            options["viewOn"].AsString.Should().Be(_collectionNamespace.CollectionName);
            options["pipeline"].AsBsonArray.Cast<BsonDocument>().Should().Equal(_pipeline);
            if (collation == null)
            {
                options.Contains("collation").Should().BeFalse();
            }
            else
            {
                options["collation"].AsBsonDocument.Should().Equals(collation.ToBsonDocument());
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_view_when_WriteConcern_is_set(
            [Values("a", "b")]
            string viewName,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Views);
            DropView(viewName);
            var subject = new CreateViewOperation(_databaseNamespace, viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(1)
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetViewInfo(binding, viewName);
            }

            var options = info["options"].AsBsonDocument;
            options["viewOn"].AsString.Should().Be(_collectionNamespace.CollectionName);
            options["pipeline"].AsBsonArray.Cast<BsonDocument>().Should().Equal(_pipeline);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Views).ClusterType(ClusterType.ReplicaSet);
            DropView(_viewName);
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result(
            [Values("a", "b")]
            string viewName)
        {
            var subject = new CreateViewOperation(_databaseNamespace, viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings);

            var result = subject.CreateCommand(Feature.Views.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "create", viewName },
                { "viewOn", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Views.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "create", _viewName },
                { "viewOn", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }
        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new CreateViewOperation(_databaseNamespace, _viewName, _collectionNamespace.CollectionName, _pipeline, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };

            var result = subject.CreateCommand(Feature.Views.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "create", _viewName },
                { "viewOn", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        // private methods
        private void DropView(string viewName)
        {
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, viewName);
            var operation = new DropCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation);
        }

        private BsonDocument GetViewInfo(IReadBinding binding, string viewName)
        {
            var listCollectionsOperation = new ListCollectionsOperation(_collectionNamespace.DatabaseNamespace, _messageEncoderSettings)
            {
                Filter = new BsonDocument("name", viewName)
            };
            return listCollectionsOperation.Execute(binding, CancellationToken.None).Single();
        }
    }
}
