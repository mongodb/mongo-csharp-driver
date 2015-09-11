/* Copyright 2015 MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal sealed class PipelineExpression : ExtensionExpression, ISourcedExpression
    {
        private readonly Expression _source;
        private readonly SerializationExpression _projector;
        private readonly ResultOperator _resultOperator;
        private readonly Type _type;

        public PipelineExpression(Expression source, SerializationExpression projector)
            : this(source, projector, (ResultOperator)null)
        {
        }

        public PipelineExpression(Expression source, SerializationExpression projector, ResultOperator resultOperator)
        {
            _source = Ensure.IsNotNull(source, nameof(source));
            _projector = Ensure.IsNotNull(projector, nameof(projector));
            _resultOperator = resultOperator;

            _type = _resultOperator == null ?
                typeof(IEnumerable<>).MakeGenericType(_projector.Type) :
                _resultOperator.Type;
        }

        public SerializationExpression Projector
        {
            get { return _projector; }
        }

        public ResultOperator ResultOperator
        {
            get { return _resultOperator; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.Pipeline; }
        }

        public override Type Type
        {
            get { return _type; }
        }

        public override string ToString()
        {
            var result = _source.ToString();
            if (_resultOperator != null)
            {
                return _resultOperator.Name + "(" + result + ")";
            }

            return result;
        }

        public PipelineExpression Update(Expression source, SerializationExpression projector, ResultOperator resultOperator)
        {
            if (source != _source ||
                projector != _projector ||
                resultOperator != _resultOperator)
            {
                return new PipelineExpression(source, projector, resultOperator);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitPipeline(this);
        }
    }
}
