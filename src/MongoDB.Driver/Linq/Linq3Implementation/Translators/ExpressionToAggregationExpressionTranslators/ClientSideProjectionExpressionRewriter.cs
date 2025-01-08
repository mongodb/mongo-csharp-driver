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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ClientSideProjectionExpressionRewriter
    {
        public static (AstProjectStage, IBsonSerializer) CreateClientSideProjection(
            TranslationContext context,
            LambdaExpression projectionLambda,
            IBsonSerializer sourceSerializer)
        {
            var (snippetsExpression, snippetsProjectionDeserializer) = ClientSideProjectionExpressionRewriter.TranslateLambdaBodyUsingSnippets(context, sourceSerializer, projectionLambda);
            if (snippetsExpression == null)
            {
                return (null, snippetsProjectionDeserializer);
            }
            else
            {
                var snippetsTranslation = new AggregationExpression(projectionLambda, snippetsExpression, snippetsProjectionDeserializer);
                return ProjectionHelper.CreateProjectStage(snippetsTranslation);
            }
        }

        private static (AstComputedDocumentExpression, IBsonSerializer) TranslateLambdaBodyUsingSnippets(
            TranslationContext context,
            IBsonSerializer sourceSerializer,
            LambdaExpression projectionLambda)
        {
            var wireVersion = context.TranslationOptions.CompatibilityLevel.ToWireVersion();
            if (!Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var clientSideProjectionDeserializer = ClientSideProjectionDeserializer.Create(sourceSerializer, projectionLambda);
                return (null, clientSideProjectionDeserializer); // project directly off $$ROOT with no snippets
            }

            var snippets = ClientSideProjectionSnippetsTranslator.TranslateSnippets(context, projectionLambda, sourceSerializer);

            if (snippets.Length == 0 || snippets.Any(IsRoot))
            {
                var clientSideProjectionDeserializer = ClientSideProjectionDeserializer.Create(sourceSerializer, projectionLambda);
                return (null, clientSideProjectionDeserializer); // project directly off $$ROOT with no snippets
            }
            else
            {
                var snippetsComputedDocument = CreateSnippetsComputedDocument(snippets);
                var snippetDeserializers = snippets.Select(s => s.Serializer).ToArray();
                var rewrittenSelectorLamdba = RewriteSelector(projectionLambda, snippets);
                var rewrittenSelectorDelegate = rewrittenSelectorLamdba.Compile();
                var clientSideProjectionSnippetsDeserializer = ClientSideProjectionSnippetsDeserializer.Create(projectionLambda.ReturnType, snippetDeserializers, rewrittenSelectorDelegate);
                return (snippetsComputedDocument, clientSideProjectionSnippetsDeserializer);
            }

            static bool IsRoot(AggregationExpression snippet) => snippet.Ast.IsRootVar();
        }

        private static AstComputedDocumentExpression CreateSnippetsComputedDocument(AggregationExpression[] snippets)
        {
            var snippetsArray = AstExpression.ComputedArray(snippets.Select(s => s.Ast));
            var snippetsdField = AstExpression.ComputedField("_snippets", snippetsArray);
            return (AstComputedDocumentExpression)AstExpression.ComputedDocument([snippetsdField]);

        }

        private static LambdaExpression RewriteSelector(LambdaExpression selectorLambda, AggregationExpression[] snippets)
        {
            var rewrittenBody = selectorLambda.Body;
            var snippetsParameter = Expression.Parameter(typeof(object[]), "snippets");

            for (var i = 0; i < snippets.Length; i++)
            {
                var snippet = snippets[i];
                var snippetReference = // (T)_snippets[i]
                    Expression.Convert(
                        Expression.ArrayIndex(snippetsParameter, Expression.Constant(i)),
                        snippet.Expression.Type);
                rewrittenBody = ExpressionReplacer.Replace(rewrittenBody, snippet.Expression, snippetReference);
            }

            return Expression.Lambda(rewrittenBody, snippetsParameter);
        }
    }
}
