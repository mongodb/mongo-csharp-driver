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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Support
{
    internal static class InternalExtensions
    {
        #region IPipelineStageDefinition

        public static IRenderedPipelineStageDefinition RenderInternal(this IPipelineStageDefinition pipelineStageDefinition, IBsonSerializer inputSerializer,
            IBsonSerializationDomain serializationDomain, IBsonSerializerRegistry serializerRegistry, ExpressionTranslationOptions translationOptions)
        {
            if (pipelineStageDefinition is IPipelineStageStageDefinitionInternal pipelineStageStageDefinitionInternal)
            {
                return pipelineStageStageDefinitionInternal
                    .Render(inputSerializer, serializationDomain, translationOptions);
            }

            return pipelineStageDefinition
                .Render(inputSerializer, serializerRegistry, translationOptions);
        }

        #endregion
    }
}