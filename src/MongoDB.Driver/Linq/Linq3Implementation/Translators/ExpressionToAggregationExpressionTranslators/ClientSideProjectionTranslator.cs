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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ClientSideProjectionTranslator
    {
        public static (AstProjectStage, IBsonSerializer) CreateProjectSnippetsStage(
            TranslationContext context,
            LambdaExpression projectionLambda,
            IBsonSerializer sourceSerializer)
        {
            var (snippetsAst, snippetsProjectionDeserializer) = RewriteProjectionUsingSnippets(context, projectionLambda, sourceSerializer);
            if (snippetsAst == null)
            {
                return (null, snippetsProjectionDeserializer);
            }
            else
            {
                var snippetsTranslation = new TranslatedExpression(projectionLambda, snippetsAst, snippetsProjectionDeserializer);
                return ProjectionHelper.CreateProjectStage(snippetsTranslation);
            }
        }

        private static (AstComputedDocumentExpression, IBsonSerializer) RewriteProjectionUsingSnippets(
            TranslationContext context,
            LambdaExpression projectionLambda,
            IBsonSerializer sourceSerializer)
        {
            var (snippets, rewrittenProjectionLamdba) = ClientSideProjectionRewriter.RewriteProjection(context, projectionLambda, sourceSerializer);

            if (snippets.Length == 0 || snippets.Any(IsRoot))
            {
                var clientSideProjectionDeserializer = ClientSideProjectionDeserializer.Create(sourceSerializer, projectionLambda);
                return (null, clientSideProjectionDeserializer); // project directly off $$ROOT with no snippets
            }
            else
            {
                var snippetsComputedDocument = CreateSnippetsComputedDocument(snippets);
                var snippetDeserializers = snippets.Select(s => s.Serializer).ToArray();
                var rewrittenProjectionDelegate = rewrittenProjectionLamdba.Compile();
                var clientSideProjectionSnippetsDeserializer = ClientSideProjectionSnippetsDeserializer.Create(projectionLambda.ReturnType, snippetDeserializers, rewrittenProjectionDelegate);
                return (snippetsComputedDocument, clientSideProjectionSnippetsDeserializer);
            }

            static bool IsRoot(TranslatedExpression snippet) => snippet.Ast.IsRootVar();
        }

        private static AstComputedDocumentExpression CreateSnippetsComputedDocument(TranslatedExpression[] snippets)
        {
            var snippetsArray = AstExpression.ComputedArray(snippets.Select(s => s.Ast));
            var snippetsField = AstExpression.ComputedField("_snippets", snippetsArray);
            return (AstComputedDocumentExpression)AstExpression.ComputedDocument([snippetsField]);
        }
    }
}
