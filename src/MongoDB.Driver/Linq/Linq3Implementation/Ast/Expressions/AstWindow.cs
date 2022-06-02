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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal class AstWindow
    {
        private readonly BsonValue _lowerBoundary;
        private readonly string _type;
        private readonly string _unit;
        private readonly BsonValue _upperBoundary;

        public AstWindow(string type, BsonValue lowerBoundary, BsonValue upperBoundary, string unit)
        {
            _type = Ensure.IsNotNull(type, nameof(type));
            _lowerBoundary = Ensure.IsNotNull(lowerBoundary, nameof(lowerBoundary));
            _upperBoundary = Ensure.IsNotNull(upperBoundary, nameof(upperBoundary));
            _unit = unit; // optional
        }

        public BsonValue LowerBoundary => _lowerBoundary;
        public string Type => _type;
        public string Unit => _unit;
        public BsonValue UpperBoundary => _upperBoundary;

        public BsonDocument Render()
        {
            return new BsonDocument
            {
                { _type, new BsonArray { _lowerBoundary, _upperBoundary } },
                { "unit", _unit, _unit != null }
            };
        }
    }
}
