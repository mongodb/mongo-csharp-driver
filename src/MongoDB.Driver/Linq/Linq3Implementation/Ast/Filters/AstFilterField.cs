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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstFilterField : AstNode
    {
        private string _path;

        public AstFilterField(string path)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
        }

        // the field is the implied document or element, so a sub field of it is the first element name of the
        // (possibly $elemMatch nested) filter document that it ends up in
        public bool IsImpliedDocument => IsImpliedDocumentMarker(_path);
        public string Path => _path;
        public override AstNodeType NodeType => AstNodeType.FilterField;

        // "@<current>" and "@<elem>" are markers and not element names: a name taken from application data that equals
        // one would be read as the marker, and a path that starts with one has it stripped when it is rendered
        public static bool IsImpliedDocumentMarker(string name) => name == "@<current>" || name == "@<elem>";

        public static bool StartsWithImpliedDocumentMarker(string path) =>
            IsImpliedDocumentMarker(path) ||
            path.StartsWith("@<current>.", StringComparison.Ordinal) ||
            path.StartsWith("@<elem>.", StringComparison.Ordinal);

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFilterField(this);
        }

        public AstFilterField SubField(string subFieldName)
        {
            Ensure.IsNotNull(subFieldName, nameof(subFieldName));

            if (_path == "@<current>")
            {
                return new AstFilterField(subFieldName);
            }
            else
            {
                return new AstFilterField(_path + "." + subFieldName);
            }
        }

        public override BsonValue Render() => _path;
    }
}
