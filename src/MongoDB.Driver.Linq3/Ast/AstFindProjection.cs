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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast
{
    public class AstFindProjection<TProjection> : AstNode
    {
        private readonly IReadOnlyList<AstFindProjectionField> _fields;
        private readonly IBsonSerializer<TProjection> _projectionSerializer;

        public AstFindProjection(
            IEnumerable<AstFindProjectionField> fields,
            IBsonSerializer<TProjection> projectionSerializer)
        {
            _fields = Ensure.IsNotNull(fields, nameof(fields)).ToList().AsReadOnly();
            _projectionSerializer = Ensure.IsNotNull(projectionSerializer, nameof(projectionSerializer));
        }

        public IReadOnlyList<AstFindProjectionField> Fields => _fields;
        public override AstNodeType NodeType => AstNodeType.FindProjection;
        public IBsonSerializer<TProjection> ProjectionSerializer => _projectionSerializer;

        public override BsonValue Render()
        {
            return new BsonDocument(_fields.Select(f => f.Render()));
        }
    }

    public abstract class AstFindProjectionField
    {
        public abstract BsonElement Render();
    }

    public class AstFindProjectionExcludeField : AstFindProjectionField
    {
        private readonly string _path;

        public AstFindProjectionExcludeField(string path)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
        }

        public override BsonElement Render()
        {
            return new BsonElement(_path, 0);
        }
    }

    public class AstFindProjectionIncludeField : AstFindProjectionField
    {
        private readonly string _path;

        public AstFindProjectionIncludeField(string path)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
        }

        public override BsonElement Render()
        {
            return new BsonElement(_path, 1);
        }
    }

    public class AstFindProjectionSerializer<TOutput, TProjection> : SerializerBase<TProjection>
    {
        private readonly Expression<Func<TOutput, TProjection>> _clientSideProjectorExpression;
        private readonly Func<TOutput, TProjection> _clientSideProjectorFunc;
        private readonly IBsonSerializer<TOutput> _outputSerializer;

        public AstFindProjectionSerializer(
            IBsonSerializer<TOutput> outputSerializer,
            Expression<Func<TOutput, TProjection>> clientSideProjectorExpression)
        {
            _outputSerializer = Ensure.IsNotNull(outputSerializer, nameof(outputSerializer));
            _clientSideProjectorExpression = Ensure.IsNotNull(clientSideProjectorExpression, nameof(clientSideProjectorExpression));
            _clientSideProjectorFunc = clientSideProjectorExpression.Compile();
        }

        public Expression<Func<TOutput, TProjection>> ClientSideProjectorExpression => _clientSideProjectorExpression;
        public Func<TOutput, TProjection> ClientSideProjectorFunc => _clientSideProjectorFunc;
        public IBsonSerializer<TOutput> OutputSerializer => _outputSerializer;

        public override TProjection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var outputValue = _outputSerializer.Deserialize(context, args);
            return _clientSideProjectorFunc(outputValue);
        }
    }
}
