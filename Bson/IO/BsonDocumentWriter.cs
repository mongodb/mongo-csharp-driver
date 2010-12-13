using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO
{
    public class BsonDocumentWriter : BsonWriter
    {
        public BsonDocumentWriter()
        {
            _rootDoc = new BsonDocument();
            _workStack.Push(_rootDoc);
        }
        private Stack<BsonValue> _workStack = new Stack<BsonValue>();
        private BsonWriteState _state = BsonWriteState.Initial;
        private BsonDocument _rootDoc = null;
        private string _pendingElementName = "";

        public BsonDocument Document { get { return _rootDoc; } }

        public override BsonWriteState WriteState
        {
            get
            {
                return _state;
            }
        }

        public override void Close()
        {
            /*do nothing*/
        }

        public override void Dispose()
        {
            /*do nothing*/
        }

        public override void Flush()
        {
            /*do nothing*/
        }

        public override void WriteName(string name)
        {
            this._pendingElementName = name;
        }

        public override void WriteBinaryData(string name, byte[] bytes, MongoDB.Bson.BsonBinarySubType subType)
        {
            WriteName(name);
            WriteBinaryData(bytes, subType);
        }
        public override void WriteBinaryData(byte[] bytes, MongoDB.Bson.BsonBinarySubType subType)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonBinaryData.Create(bytes, subType));
        }

        public override void WriteBoolean(string name, bool value)
        {
            WriteName(name);
            WriteBoolean(value);
        }
        public override void WriteBoolean(bool value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonBoolean.Create(value));
        }

        public override void WriteDateTime(string name, DateTime value)
        {
            WriteName(name);
            WriteDateTime(value);
        }
        public override void WriteDateTime(DateTime value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonDateTime.Create(value));
        }

        public override void WriteDouble(string name, double value)
        {
            WriteName(name);
            WriteDouble(value);
        }
        public override void WriteDouble(double value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonDouble.Create(value));
        }

        public override void WriteEndArray()
        {
            _workStack.Pop();
        }
        public override void WriteEndDocument()
        {
            _workStack.Pop();
        }

        public override void WriteInt32(string name, int value)
        {
            WriteName(name);
            WriteInt32(value);
        }
        public override void WriteInt32(int value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonInt32.Create(value));
        }

        public override void WriteInt64(string name, long value)
        {
            WriteName(name);
            WriteInt64(value);
        }
        public override void WriteInt64(long value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonInt64.Create(value));
        }

        public override void WriteJavaScript(string name, string code)
        {
            WriteName(name);
            WriteJavaScript(code);
        }
        public override void WriteJavaScript(string code)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonJavaScript.Create(code));
        }

        public override void WriteJavaScriptWithScope(string name, string code)
        {
            WriteName(name);
            WriteJavaScriptWithScope(code);
        }
        public override void WriteJavaScriptWithScope(string code)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonJavaScriptWithScope.Create(code));
        }

        public override void WriteMaxKey(string name)
        {
            WriteName(name);
            WriteMaxKey();
        }
        public override void WriteMaxKey()
        {
            /*do nothing*/
        }

        public override void WriteMinKey(string name)
        {
            WriteName(name);
            WriteMinKey();
        }
        public override void WriteMinKey()
        {
            /*do nothing*/
        }

        public override void WriteNull(string name)
        {
            WriteName(name);
            WriteNull();
        }
        public override void WriteNull()
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonNull.Value);
        }

        public override void WriteObjectId(string name, int timestamp, int machine, short pid, int increment)
        {
            WriteName(name);
            WriteObjectId(timestamp, machine, pid, increment);
        }
        public override void WriteObjectId(int timestamp, int machine, short pid, int increment)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonObjectId.Create(timestamp, machine, pid, increment));
        }

        public override void WriteRegularExpression(string name, string pattern, string options)
        {
            WriteName(name);
            WriteRegularExpression(pattern, options);
        }
        public override void WriteRegularExpression(string pattern, string options)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonRegularExpression.Create(pattern, options));
        }

        public override void WriteStartArray(string name)
        {
            WriteName(name);
            WriteStartArray();
        }
        public override void WriteStartArray()
        {
            BsonValue parent = _workStack.Peek();
            BsonValue newValue = new BsonArray();
            AddToCollection(parent, _pendingElementName, newValue);
            _workStack.Push(newValue);
        }

        public override void WriteStartDocument(string name)
        {
            WriteName(name);
            WriteStartDocument();
        }
        public override void WriteStartDocument()
        {
            BsonValue parent = _workStack.Peek();
            BsonValue newValue = new BsonDocument(false);
            AddToCollection(parent, _pendingElementName, newValue);
            _workStack.Push(newValue);
        }

        private void AddToCollection(BsonValue parent, string name, BsonValue newValue)
        {
            if (parent is BsonDocument)
            {
                parent.AsBsonDocument.Add(name, newValue);
            }
            else if (parent is BsonArray)
            {
                _workStack.Peek().AsBsonArray.Add(newValue);
            }
        }

        public override void WriteString(string name, string value)
        {
            WriteName(name);
            WriteString(value);
        }
        public override void WriteString(string value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonString.Create(value));
        }

        public override void WriteSymbol(string name, string value)
        {
            WriteName(name);
            WriteSymbol(value);
        }
        public override void WriteSymbol(string value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonSymbol.Create(value));
        }

        public override void WriteTimestamp(string name, long value)
        {
            WriteName(name);
            WriteTimestamp(value);
        }
        public override void WriteTimestamp(long value)
        {
            AddToCollection(_workStack.Peek(), _pendingElementName, BsonTimestamp.Create(value));
        }

    }
}
