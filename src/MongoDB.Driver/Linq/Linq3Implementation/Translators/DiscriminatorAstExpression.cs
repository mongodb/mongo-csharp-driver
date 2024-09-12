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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators;

internal static class DiscriminatorAstExpression
{
    public static AstExpression TypeEquals(AstGetFieldExpression discriminatorField, IDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
        return discriminator == null ?
            AstExpression.IsMissing(discriminatorField) :
            AstExpression.Eq(discriminatorField, discriminator);
    }

    public static AstExpression TypeIs(AstGetFieldExpression discriminatorField, IHierarchicalDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
        var lastItem = discriminator is BsonArray array ? array.Last() : discriminator;
        return AstExpression.Cond(
            AstExpression.Eq(AstExpression.Type(discriminatorField), "array"),
            AstExpression.In(lastItem, discriminatorField),
            AstExpression.Eq(discriminatorField, lastItem));
    }

    public static AstExpression TypeIs(AstGetFieldExpression discriminatorField, IScalarDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminators = discriminatorConvention.GetDiscriminatorsForTypeAndSubTypes(actualType);
        return discriminators.Length == 1
            ? AstExpression.Eq(discriminatorField, discriminators.Single())
            : AstExpression.In(discriminatorField, new BsonArray(discriminators));
    }
}
