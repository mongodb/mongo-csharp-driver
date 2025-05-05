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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal static class AstExpressionExtensions
    {
        public static bool IsBooleanConstant(this AstExpression expression)
            => expression.IsConstant<BsonBoolean>(out _);

        public static bool IsBooleanConstant(this AstExpression expression, out bool booleanConstant)
        {
            if (expression.IsConstant<BsonBoolean>(out var bsonBooleanConstant))
            {
                booleanConstant = bsonBooleanConstant.Value;
                return true;
            }

            booleanConstant = default;
            return false;
        }

        public static bool IsBsonNull(this AstExpression expression)
            => expression.IsConstant(out var constant) && constant.IsBsonNull;

        public static bool IsConstant(this AstExpression expression, out BsonValue constant)
        {
            if (expression is AstConstantExpression constantExpression)
            {
                constant = constantExpression.Value;
                return true;
            }

            constant = null;
            return false;
        }

        public static bool IsConstant<TBsonValue>(this AstExpression expression, out TBsonValue constant)
            where TBsonValue : BsonValue
        {
            if (expression.IsConstant(out var bsonValueConstant) && bsonValueConstant is TBsonValue derivedBsonValueConstant)
            {
                constant = derivedBsonValueConstant;
                return true;
            }

            constant = null;
            return false;
        }

        public static bool IsInt32Constant(this AstExpression expression)
            => expression.IsConstant<BsonInt32>(out _);

        public static bool IsInt32Constant(this AstExpression expression, int comparand)
            => expression.IsInt32Constant(out var int32Constant) && int32Constant == comparand;

        public static bool IsInt32Constant(this AstExpression expression, out int int32Constant)
        {
            if (expression.IsConstant<BsonInt32>(out var bsonInt32Constant))
            {
                int32Constant = bsonInt32Constant.Value;
                return true;
            }

            int32Constant = default;
            return false;
        }

        public static bool IsMaxInt32(this AstExpression expression)
            => expression.IsInt32Constant(out var int32Constant) && int32Constant == int.MaxValue;

        public static bool IsRootVar(this AstExpression expression)
            => expression is AstVarExpression varExpression && varExpression.Name == "ROOT" && varExpression.IsCurrent;

        public static bool IsStringConstant(this AstExpression expression, string comparand)
            => expression.IsStringConstant(out var stringConstant) && stringConstant == comparand;

        public static bool IsStringConstant(this AstExpression expression, out string stringConstant)
        {
            if (expression.IsConstant<BsonString>(out var bsonStringConstant))
            {
                stringConstant = bsonStringConstant.Value;
                return true;
            }

            stringConstant = default;
            return false;
        }

       public static bool IsZero(this AstExpression expression)
            => expression.IsConstant(out var constant) && constant == 0; // works for all numeric BSON types
    }
}
