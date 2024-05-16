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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4746Tests
    {
        [Fact]
        public void UpdateDefinitionBuilder_Combine_should_throw_when_PipelineUpdateDefinition_is_combined_with_another_update()
        {
            var pipeline = new EmptyPipelineDefinition<C>();
            var pipelineUpdate = Builders<C>.Update.Pipeline(pipeline);
            var setUpdate = Builders<C>.Update.Set(x => x.X, 2);

            var exception = Record.Exception(() => new UpdateDefinitionBuilder<C>().Combine(pipelineUpdate, setUpdate));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void UpdateDefinitionBuilder_Combine_should_throw_when_an_update_is_combined_with_a_PipelineUpdateDefinition()
        {
            var setUpdate = Builders<C>.Update.Set(x => x.X, 2);
            var pipeline = new EmptyPipelineDefinition<C>();
            var pipelineUpdate = Builders<C>.Update.Pipeline(pipeline);

            var exception = Record.Exception(() => new UpdateDefinitionBuilder<C>().Combine(setUpdate, pipelineUpdate));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
