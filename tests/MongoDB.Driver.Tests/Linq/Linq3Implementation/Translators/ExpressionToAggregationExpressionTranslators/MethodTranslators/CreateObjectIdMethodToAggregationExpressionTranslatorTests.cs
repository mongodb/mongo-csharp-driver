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

using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class CreateObjectIdMethodToAggregationExpressionTranslatorTests
{
    [Fact]
    public void Translate_should_produce_proper_ast()
    {
        var expression = TestHelpers.MakeLambda<MyModel, ObjectId>(model => Mql.CreateObjectId());
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = CreateObjectIdMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<ObjectIdSerializer>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse("{ $createObjectId: { } }"));
    }

    public class MyModel
    {
    }
}

