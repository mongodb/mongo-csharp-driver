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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal class TranslatedFilterField
    {
        private readonly AstFilterField _ast;
        private readonly IBsonSerializer _serializer;

        public TranslatedFilterField(AstFilterField ast, IBsonSerializer serializer)
        {
            _ast = ast;
            _serializer = serializer;
        }

        public AstFilterField Ast => _ast;
        public IBsonSerializer Serializer => _serializer;

        public TranslatedFilterField SubField(string subFieldName, IBsonSerializer subFieldSerializer)
        {
            var astSubField = _ast.SubField(subFieldName);
            return new TranslatedFilterField(astSubField, subFieldSerializer);
        }

        public override string ToString() => _ast.ToString();
    }
}
