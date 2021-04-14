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
    public static class AstSort
    {
        public static AstSortField Field(string path, AstSortOrder order)
        {
            return new AstSortField(path, order);
        }
    }

    public sealed class AstSortField
    {
        private readonly string _path;
        private readonly AstSortOrder _order;

        public AstSortField(string path, AstSortOrder order)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _order = Ensure.IsNotNull(order, nameof(order));
        }

        public AstSortOrder Order => _order;
        public string Path => _path;

        public BsonElement Render()
        {
            return new BsonElement(_path, _order.Render());
        }
    }

    public abstract class AstSortOrder
    {
        private readonly static AstSortOrder __ascending = new AstAscendingSortOrder();
        private readonly static AstSortOrder __descending = new AstDescendingSortOrder();
        private readonly static AstSortOrder __metaTextScore = new AstMetaTextScoreSortOrder();

        public static AstSortOrder Ascending => __ascending;
        public static AstSortOrder Descending => __descending;
        public static AstSortOrder MetaTextScore => __metaTextScore;

        public abstract BsonValue Render();
    }

    public sealed class AstAscendingSortOrder : AstSortOrder
    {
        public override BsonValue Render() => 1;
    }

    public sealed class AstDescendingSortOrder : AstSortOrder
    {
        public override BsonValue Render() => -1;
    }

    public sealed class AstMetaTextScoreSortOrder : AstSortOrder
    {
        public override BsonValue Render() => new BsonDocument("$meta", "textScore");
    }

    public sealed class AstSortStage : AstStage
    {
        private readonly IReadOnlyList<AstSortField> _fields;

        public AstSortStage(IEnumerable<AstSortField> fields)
        {
            _fields = Ensure.IsNotNull(fields, nameof(fields)).ToList().AsReadOnly();
        }

        public IReadOnlyList<AstSortField> Fields => _fields;
        public override AstNodeType NodeType => AstNodeType.SortStage;

        public AstSortStage AddSortField(AstSortField field)
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
