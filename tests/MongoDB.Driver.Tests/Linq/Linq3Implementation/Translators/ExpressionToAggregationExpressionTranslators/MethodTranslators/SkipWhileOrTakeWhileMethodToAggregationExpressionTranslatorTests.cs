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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class SkipWhileOrTakeWhileMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst, Type expectedSerializerType)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = SkipWhileOrTakeWhileMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType(expectedSerializerType);
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        [
            TestHelpers.MakeLambda<MyModel, IEnumerable<int>>(model => model.Items.SkipWhile(x => x < 3)),
            """
            {
                $let :
                {
                    vars :
                    {
                        "while" :
                        {
                            $reduce :
                            {
                                input : { $getField : { field : 'Items', input : '$$ROOT' } },
                                initialValue : { predicate : true, count : 0 },
                                in :
                                {
                                    $switch :
                                    {
                                        branches :
                                        [
                                            { case : { $not : { $getField : { field : 'predicate', input : '$$value' } } }, then : '$$value' },
                                            { case : { $lt : ['$$this', 3] }, then : { predicate : true, count : { $add : [{ $getField : { field : 'count', input : '$$value' } }, 1] } } },
                                        ],
                                        default : { predicate : false, count : { $getField : { field : 'count', input : '$$value' } } }
                                    }
                                }
                            }
                        }
                    },
                    in : { $slice : [{ $getField : { field : 'Items', input : '$$ROOT' } }, { $getField : { field : 'count', input : '$$while' } }, 2147483647] }
                }
            }
            """,
            typeof(IEnumerableSerializer<int>)
        ],
        [
            TestHelpers.MakeLambda<MyModel, IEnumerable<int>>(model => model.Items.TakeWhile(x => x < 3)),
            """
            {
                $let :
                {
                    vars :
                    {
                        "while" :
                        {
                            $reduce :
                            {
                                input : { $getField : { field : 'Items', input : '$$ROOT' } },
                                initialValue : { predicate : true, count : 0 },
                                in :
                                {
                                    $switch :
                                    {
                                        branches :
                                        [
                                            { case : { $not : { $getField : { field : 'predicate', input : '$$value' } } }, then : '$$value' },
                                            { case : { $lt : ['$$this', 3] }, then : { predicate : true, count : { $add : [{ $getField : { field : 'count', input : '$$value' } }, 1] } } },
                                        ],
                                        default : { predicate : false, count : { $getField : { field : 'count', input : '$$value' } } }
                                    }
                                }
                            }
                        }
                    },
                    in : { $slice : [{ $getField : { field : 'Items', input : '$$ROOT' } }, { $getField : { field : 'count', input : '$$while' } }] }
                }
            }
            """,
            typeof(IEnumerableSerializer<int>)
        ],
        [
            TestHelpers.MakeLambda<MyModel, IEnumerable<int>>(model => model.Items.SkipWhile((x, i) => i < 3)),
            """
            {
                $let :
                {
                    vars :
                    {
                        "while" :
                        {
                            $reduce :
                            {
                                input : { $getField : { field : 'Items', input : '$$ROOT' } },
                                initialValue : { predicate : true, count : 0 },
                                in :
                                {
                                    $switch :
                                    {
                                        branches :
                                        [
                                            { case : { $not : { $getField : { field : 'predicate', input : '$$value' } } }, then : '$$value' },
                                            { case : { $lt : ['$$i', 3] }, then : { predicate : true, count : { $add : [{ $getField : { field : 'count', input : '$$value' } }, 1] } } },
                                        ],
                                        default : { predicate : false, count : { $getField : { field : 'count', input : '$$value' } } }
                                    }
                                },
                                arrayIndexAs : 'i'
                            }
                        }
                    },
                    in : { $slice : [{ $getField : { field : 'Items', input : '$$ROOT' } }, { $getField : { field : 'count', input : '$$while' } }, 2147483647] }
                }
            }
            """,
            typeof(IEnumerableSerializer<int>)
        ],
        [
            TestHelpers.MakeLambda<MyModel, IEnumerable<int>>(model => model.Items.TakeWhile((x, i) => i < 3)),
            """
            {
                $let :
                {
                    vars :
                    {
                        "while" :
                        {
                            $reduce :
                            {
                                input : { $getField : { field : 'Items', input : '$$ROOT' } },
                                initialValue : { predicate : true, count : 0 },
                                in :
                                {
                                    $switch :
                                    {
                                        branches :
                                        [
                                            { case : { $not : { $getField : { field : 'predicate', input : '$$value' } } }, then : '$$value' },
                                            { case : { $lt : ['$$i', 3] }, then : { predicate : true, count : { $add : [{ $getField : { field : 'count', input : '$$value' } }, 1] } } },
                                        ],
                                        default : { predicate : false, count : { $getField : { field : 'count', input : '$$value' } } }
                                    }
                                },
                                arrayIndexAs : 'i'
                            }
                        }
                    },
                    in : { $slice : [{ $getField : { field : 'Items', input : '$$ROOT' } }, { $getField : { field : 'count', input : '$$while' } }] }
                }
            }
            """,
            typeof(IEnumerableSerializer<int>)
        ],
    ];

    public class MyModel
    {
        public IEnumerable<int> Items { get; set; }
    }
}
