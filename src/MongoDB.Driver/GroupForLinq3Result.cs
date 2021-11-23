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

using System.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the return result from PipelineDefinitionBuilder.GroupForLinq3 method.
    /// </summary>
    /// <typeparam name="TInput">The type of the input documents.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <typeparam name="TOutput">The type of the output documents.</typeparam>
    public class GroupForLinq3Result<TInput, TValue, TOutput>
    {
        internal GroupForLinq3Result(PipelineStageDefinition<TInput, IGrouping<TValue, TInput>> groupStage, PipelineStageDefinition<IGrouping<TValue, TInput>, TOutput> projectStage)
        {
            GroupStage = groupStage;
            ProjectStage = projectStage;
        }

        /// <summary>
        /// The resulting group stage.
        /// </summary>
        public PipelineStageDefinition<TInput, IGrouping<TValue, TInput>> GroupStage { get; }

        /// <summary>
        /// The resulting project stage.
        /// </summary>
        public PipelineStageDefinition<IGrouping<TValue, TInput>, TOutput> ProjectStage { get; }

        /// <summary>
        /// Deconstructs this class into its components.
        /// </summary>
        /// <param name="groupStage">The group stage.</param>
        /// <param name="projectStage">The project stage.</param>
        public void Deconstruct(
            out PipelineStageDefinition<TInput, IGrouping<TValue, TInput>> groupStage,
            out PipelineStageDefinition<IGrouping<TValue, TInput>, TOutput> projectStage)
        {
            groupStage = GroupStage;
            projectStage = ProjectStage;
        }
    }
}
