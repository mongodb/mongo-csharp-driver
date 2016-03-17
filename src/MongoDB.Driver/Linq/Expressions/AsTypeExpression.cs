using System;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class AsTypeExpression : SerializationExpression, IAsTypeExpression
    {
        private readonly Expression _document;
        private readonly Expression _original;
        private readonly IBsonSerializer _serializer;

        public AsTypeExpression(IBsonSerializer serializer)
            : this(null, serializer, null)
        {
        }

        public AsTypeExpression(Expression document, IBsonSerializer serializer)
            : this(document, serializer, null)
        {
        }

        public AsTypeExpression(IBsonSerializer serializer, Expression original)
            : this(null, serializer, original)
        {
        }

        public AsTypeExpression(Expression document, IBsonSerializer serializer, Expression original)
        {
            _document = document;
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
            _original = original;
        }

        public Expression Document
        {
            get { return _document; }
        }
        
        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.TypeAs; }
        }

        public Expression Original
        {
            get { return _original; }
        }

        public override IBsonSerializer Serializer
        {
            get { return _serializer; }
        }

        public override Type Type
        {
            get { return _serializer.ValueType; }
        }
        
        public AsTypeExpression Update(Expression document, Expression original)
        {
            if (document != _document || original != _original)
            {
                return new AsTypeExpression(document, _serializer, original);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitAsType(this);
        }
    }
}
