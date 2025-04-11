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
            =>
                expression is AstConstantExpression constantExpression &&
                constantExpression.Value.IsBoolean;

        public static bool IsBooleanConstant(this AstExpression expression, out bool value)
        {
            if (expression is AstConstantExpression constantExpression && constantExpression.Value is BsonBoolean bsonBoolean)
            {
                value = bsonBoolean.Value;
                return true;
            }

            value = default;
            return false;
        }

        public static bool IsBsonNull(this AstExpression expression)
            =>
                expression is AstConstantExpression constantExpression &&
                constantExpression.Value.IsBsonNull;

        public static bool IsConstant(this AstExpression expression, out BsonValue value)
        {
            if (expression is AstConstantExpression constantExpression)
            {
                value = constantExpression.Value;
                return true;
            }

            value = null;
            return false;
        }

        public static bool IsConstant<TBsonValue>(this AstExpression expression, out TBsonValue value)
            where TBsonValue : BsonValue
        {
            if (expression is AstConstantExpression constantExpression && constantExpression.Value is TBsonValue bsonValue)
            {
                value = bsonValue;
                return true;
            }

            value = null;
            return false;
        }

        public static bool IsInt32Constant(this AstExpression expression)
            =>
                expression is AstConstantExpression constantExpression &&
                constantExpression.Value.IsInt32;

        public static bool IsInt32Constant(this AstExpression expression, int value)
            =>
                expression is AstConstantExpression constantExpression &&
                constantExpression.Value is BsonInt32 bsonInt32 &&
                bsonInt32.Value == value;

        public static bool IsInt32Constant(this AstExpression expression, out int value)
        {
            if (expression is AstConstantExpression constantExpression && constantExpression.Value is BsonInt32 bsonInt32)
            {
                value = bsonInt32.Value;
                return true;
            }

            value = default;
            return false;
        }

        public static bool IsMaxInt32(this AstExpression expression)
            => expression.IsInt32Constant(out var value) && value == int.MaxValue;

        public static bool IsRootVar(this AstExpression expression)
            => expression is AstVarExpression varExpression && varExpression.Name == "ROOT" && varExpression.IsCurrent;

        public static bool IsStringConstant(this AstExpression expression, string value)
            =>
                expression is AstConstantExpression constantExpression &&
                constantExpression.Value is BsonString bsonString &&
                bsonString.Value == value;

        public static bool IsStringConstant(this AstExpression expression, out string value)
        {
            if (expression is AstConstantExpression constantExpression && constantExpression.Value is BsonString bsonString)
            {
                value = bsonString.Value;
                return true;
            }

            value = default;
            return false;
        }

       public static bool IsZero(this AstExpression expression)
            => expression is AstConstantExpression constantExpression && constantExpression.Value == 0;
    }
}
