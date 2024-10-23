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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators;

internal static class DiscriminatorAstFilter
{
    public static AstFilter TypeEquals(AstFilterField discriminatorField, IHierarchicalDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
        if (discriminator == null)
        {
            return AstFilter.NotExists(discriminatorField);
        }
        else if (discriminator.IsBsonArray)
        {
            return AstFilter.Eq(discriminatorField, discriminator);
        }
        else
        {
            var discriminatorFieldItemZero = discriminatorField.SubField("0", BsonValueSerializer.Instance);
            return AstFilter.And(
                AstFilter.NotExists(discriminatorFieldItemZero), // required to avoid false matches on subclasses
                AstFilter.Eq(discriminatorField, discriminator));
        }
    }

    public static AstFilter TypeEquals(AstFilterField discriminatorField, IDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
        return discriminator == null ?
            AstFilter.NotExists(discriminatorField) :
            AstFilter.Eq(discriminatorField, discriminator);
    }

    public static AstFieldOperationFilter TypeIs(AstFilterField discriminatorField, IHierarchicalDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
        var lastItem = discriminator is BsonArray array ? array.Last() : discriminator;
        return AstFilter.Eq(discriminatorField, lastItem); // will match subclasses also
    }

    public static AstFieldOperationFilter TypeIs(AstFilterField discriminatorField, IScalarDiscriminatorConvention discriminatorConvention, Type nominalType, Type actualType)
    {
        var discriminators = discriminatorConvention.GetDiscriminatorsForTypeAndSubTypes(actualType);
        return discriminators.Length == 1 ?
            AstFilter.Eq(discriminatorField, discriminators.Single()) :
            AstFilter.In(discriminatorField, discriminators);
    }
}
