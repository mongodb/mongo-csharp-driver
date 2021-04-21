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

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    internal sealed class AstCollStatsStageCount
    {
        public BsonDocument Render() => new BsonDocument();
    }

    internal sealed class AstCollStatsStageLatencyStats
    {
        private readonly bool? _histograms;

        private AstCollStatsStageLatencyStats(bool? histograms)
        {
            _histograms = histograms;
        }

        public bool? Histograms => _histograms;

        public BsonDocument Render()
        {
            return
                new BsonDocument
                {
                    { "histograms", _histograms ?? false, _histograms.HasValue }
                };
        }
    }

    internal sealed class AstCollStatsStageQueryExecStats
    {
        public BsonDocument Render() => new BsonDocument();
    }

    internal sealed class AstCollStatsStageStorageStats
    {
        private readonly int? _scale;

        public AstCollStatsStageStorageStats(int? scale)
        {
            _scale = scale;
        }

        public int? Scale => _scale;

        public BsonDocument Render()
        {
            return
                new BsonDocument
                {
                    { "scale", _scale ?? 0, _scale.HasValue }
                };
        }
    }

    internal sealed class AstCollStatsStage : AstStage
    {
        private readonly AstCollStatsStageCount _count;
        private readonly AstCollStatsStageLatencyStats _latencyStats;
        private readonly AstCollStatsStageQueryExecStats _queryExecStats;
        private readonly AstCollStatsStageStorageStats _storageStats;

        public AstCollStatsStage(
            AstCollStatsStageLatencyStats latencyStats = null,
            AstCollStatsStageStorageStats storageStats = null,
            AstCollStatsStageCount count = null,
            AstCollStatsStageQueryExecStats queryExecStats = null)
        {
            _latencyStats = latencyStats; // can be null
            _storageStats = storageStats; // can be null
            _count = count; // can be null
            _queryExecStats = queryExecStats; // can be null
        }

        public new AstCollStatsStageCount Count => _count;
        public AstCollStatsStageLatencyStats LatencyStats => _latencyStats;
        public override AstNodeType NodeType => AstNodeType.CollStatsStage;
        public AstCollStatsStageQueryExecStats QueryExecStats => _queryExecStats;
        public AstCollStatsStageStorageStats StorageStats => _storageStats;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$collStats", new BsonDocument
                    {
                        { "latencyStats", () => _latencyStats.Render(), _latencyStats != null },
                        { "storageStats", () => _storageStats.Render(), _storageStats != null },
                        { "count", () => _count.Render(), _count != null },
                        { "queryExecStats", () => _queryExecStats.Render(), _queryExecStats != null }
                    }
                }
            };
        }
    }
}
