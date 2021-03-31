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
using MongoDB.Bson;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public abstract class AstExpression : AstNode
    {
        // public implicit conversions
        public static implicit operator AstExpression(BsonValue value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(bool value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(int value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(string value)
        {
            return new AstConstantExpression(value);
        }

        // public static methods
        public static AstExpression Add(params AstExpression[] args)
        {
            if (AllArgsAreConstantInt32s(args, out var values))
            {
                var sum = values.Sum();
                return new AstConstantExpression(sum);
            }

            return new AstNaryExpression(AstNaryOperator.Add, args);
        }

        // private static methods
        private static bool AllArgsAreConstantInt32s(AstExpression[] args, out List<int> values)
        {
            if (args.All(arg => arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.Int32))
            {
                values = args.Select(arg => ((AstConstantExpression)arg).Value.AsInt32).ToList();
                return true;
            }

            values = null;
            return false;
        }
    }
}
