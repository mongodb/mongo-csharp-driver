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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstFacetStageFacet : AstNode
    {
        private readonly string _outputField;
        private readonly AstPipeline _pipeline;

        public AstFacetStageFacet(string outputField, AstPipeline pipeline)
        {
            _outputField = Ensure.IsNotNull(outputField, nameof(outputField));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline));
        }

        public override AstNodeType NodeType => AstNodeType.FacetStageFacet;
        public string OutputField => _outputField;
        public AstPipeline Pipeline => _pipeline;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFacetStageFacet(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument(RenderAsElement());
        }

        public BsonElement RenderAsElement()
        {
            return new BsonElement(_outputField, _pipeline.Render());
        }

        public AstFacetStageFacet Update(AstPipeline pipeline)
        {
            if (pipeline == _pipeline)
            {
                return this;
            }

            return new AstFacetStageFacet(_outputField, pipeline);
        }
    }

    internal sealed class AstFacetStage : AstStage
    {
        private readonly IReadOnlyList<AstFacetStageFacet> _facets;

        public AstFacetStage(IEnumerable<AstFacetStageFacet> facets)
        {
            _facets = Ensure.IsNotNull(facets, nameof(facets)).AsReadOnlyList();
        }

        public IReadOnlyList<AstFacetStageFacet> Facets => _facets;
        public override AstNodeType NodeType => AstNodeType.FacetStage;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFacetStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$facet", new BsonDocument(_facets.Select(f => f.RenderAsElement())));
        }

        public AstFacetStage Update(IEnumerable<AstFacetStageFacet> facets)
        {
            if (facets == _facets)
            {
                return this;
            }

            return new AstFacetStage(facets);
        }
    }
}
