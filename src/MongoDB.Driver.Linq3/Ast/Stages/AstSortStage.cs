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
using MongoDB.Driver.Core.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    public sealed class AstSortStageField
    {
        private readonly string _field;
        private readonly AstSortStageSortOrder _order;

        public AstSortStageField(string field, AstSortStageSortOrder order)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _order = Ensure.IsNotNull(order, nameof(order));
        }

        public string Field => _field;
        public AstSortStageSortOrder Order => _order;

        public BsonElement Render()
        {
            return new BsonElement(_field, _order.Render());
        }
    }

    public abstract class AstSortStageSortOrder
    {
        private readonly static AstSortStageSortOrder __ascending = new AstSortStageAscendingSortOrder();
        private readonly static AstSortStageSortOrder __descending = new AstSortStageDescendingSortOrder();
        private readonly static AstSortStageSortOrder __metaTextScore = new AstSortStageMetaTextScoreSortOrder();

        public static AstSortStageSortOrder Ascending => __ascending;
        public static AstSortStageSortOrder Descending => __descending;
        public static AstSortStageSortOrder MetaTextScore => __metaTextScore;

        public abstract BsonValue Render();
    }

    public sealed class AstSortStageAscendingSortOrder : AstSortStageSortOrder
    {
        public override BsonValue Render() => 1;
    }

    public sealed class AstSortStageDescendingSortOrder : AstSortStageSortOrder
    {
        public override BsonValue Render() => -1;
    }

    public sealed class AstSortStageMetaTextScoreSortOrder : AstSortStageSortOrder
    {
        public override BsonValue Render() => new BsonDocument("$meta", "textScore");
    }

    public sealed class AstSortStage : AstStage
    {
        private readonly IReadOnlyList<AstSortStageField> _fields;

        public AstSortStage(IEnumerable<AstSortStageField> fields)
        {
            _fields = Ensure.IsNotNull(fields, nameof(fields)).ToList().AsReadOnly();
        }

        public IReadOnlyList<AstSortStageField> Fields => _fields;
        public override AstNodeType NodeType => AstNodeType.SortStage;

        public AstSortStage AddSortField(AstSortStageField field)
        {
            Ensure.IsNotNull(field, nameof(field));
            return new AstSortStage(_fields.Concat(new[] { field }));
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$sort", new BsonDocument(_fields.Select(f => f.Render())));
        }
    }
}
