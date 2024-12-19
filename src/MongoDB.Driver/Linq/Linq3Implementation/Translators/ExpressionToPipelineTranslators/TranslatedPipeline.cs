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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal sealed class TranslatedPipeline
    {
        #region static
        public static TranslatedPipeline Empty(IBsonSerializer outputSerializer)
        {
            return new TranslatedPipeline(AstPipeline.Empty, outputSerializer);
        }
        #endregion

        private readonly AstPipeline _astPipeline;
        private readonly IBsonSerializer _outputSerializer;

        public TranslatedPipeline(AstPipeline astPipeline, IBsonSerializer outputSerializer)
        {
            _astPipeline = Ensure.IsNotNull(astPipeline, nameof(astPipeline));
            _outputSerializer = Ensure.IsNotNull(outputSerializer, nameof(outputSerializer));
        }

        public AstPipeline AstPipeline => _astPipeline;
        public IReadOnlyList<AstStage> AstStages => _astPipeline.Stages;
        public IBsonSerializer OutputSerializer => _outputSerializer;

        public TranslatedPipeline AddStages(
            IBsonSerializer newOutputSerializer,
            params AstStage[] newStages)
        {
            var oldAstStages = _astPipeline.Stages;
            var newAstPipeline = new AstPipeline(oldAstStages.Concat(newStages));
            return new TranslatedPipeline(newAstPipeline, newOutputSerializer);
        }

        public TranslatedPipeline ReplaceLastStage(
            IBsonSerializer newOutputSerializer,
            AstStage newLastStage)
        {
            var oldAstStages = _astPipeline.Stages;
            var newAstPipeline = new AstPipeline(oldAstStages.Take(oldAstStages.Count - 1).Concat([newLastStage]));
            return new TranslatedPipeline(newAstPipeline, newOutputSerializer);
        }

        public TranslatedPipeline ReplaceStagesAtEnd(
            IBsonSerializer newOutputSerializer,
            int numberOfStagesToReplace,
            params AstStage[] newStages)
        {
            var oldAstStages = _astPipeline.Stages;
            var newAstPipeline = new AstPipeline(oldAstStages.Take(oldAstStages.Count - numberOfStagesToReplace).Concat(newStages));
            return new TranslatedPipeline(newAstPipeline, newOutputSerializer);
        }
    }
}
