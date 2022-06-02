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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal sealed class AstSortFields : IEnumerable<AstSortField>
    {
        private readonly IReadOnlyList<AstSortField> _fields;

        public AstSortFields(IEnumerable<AstSortField> fields)
        {
            _fields = Ensure.IsNotNull(fields, nameof(fields)).AsReadOnlyList();
        }

        public IReadOnlyList<AstSortField> Fields => _fields;

        public IEnumerator<AstSortField> GetEnumerator() => _fields.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public AstSortFields AddSortField(AstSortField field)
        {
            Ensure.IsNotNull(field, nameof(field));
            return new AstSortFields(_fields.Concat(new[] { field }));
        }

        public BsonDocument Render()
        {
            return new BsonDocument(_fields.Select(f => f.RenderAsElement()));
        }

        public override string ToString() => Render().ToJson();
    }
}
