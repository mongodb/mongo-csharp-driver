/* Copyright 2010-2012 10gen Inc.
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
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a mapping from a set of UTF8 encoded strings to a set of elementName/value pairs, implemented as a trie.
    /// </summary>
    public class BsonTrie<TValue>
    {
        // private static fields
        private static readonly UTF8Encoding __utf8Encoding = new UTF8Encoding(false, true); // throw on invalid bytes

        // private fields
        private readonly BsonTrieNode<TValue> _root;
        private bool _isFrozen;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonTrie class.
        /// </summary>
        public BsonTrie()
        {
            _root = new BsonTrieNode<TValue>(0);
        }

        // public properties
        /// <summary>
        /// Gets the root node.
        /// </summary>
        public BsonTrieNode<TValue> Root
        {
            get
            {
                return _root;
            }
        }

        // public methods
        /// <summary>
        /// Adds the specified elementName (after encoding as a UTF8 byte sequence) and value to the trie.
        /// </summary>
        /// <param name="elementName">The element name to add.</param>
        /// <param name="value">The value to add. The value can be null for reference types.</param>
        public void Add(string elementName, TValue value)
        {
            if (_isFrozen) { throw new InvalidOperationException("BsonTrie is frozen."); }
            var keyBytes = __utf8Encoding.GetBytes(elementName);

            var node = _root;
            foreach (var keyByte in keyBytes)
            {
                var child = node.GetChild(keyByte);
                if (child == null)
                {
                    child = new BsonTrieNode<TValue>(keyByte);
                    node.AddChild(child);
                }
                node = child;
            }

            node.SetValue(elementName, value);
        }

        /// <summary>
        /// Freezes the BsonTrie and optimizes the nodes included so far for faster retrieval.
        /// </summary>
        public void Freeze()
        {
            if (!_isFrozen)
            {
                _root.Freeze();
                _isFrozen = true;
            }
        }

        /// <summary>
        /// Gets the value associated with the specified element name.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified element name, if the key is found;
        /// otherwise, the default value for the type of the value parameter. This parameter is passed unitialized.
        /// </param>
        /// <returns></returns>
        public bool TryGetValue(string elementName, out TValue value)
        {
            var keyBytes = __utf8Encoding.GetBytes(elementName);

            var node = _root;
            for (var i = 0; i < keyBytes.Length; i++)
            {
                node = node.GetChild(keyBytes[i]);
                if (node == null)
                {
                    value = default(TValue);
                    return false;
                }
            }

            if (!node.HasValue)
            {
                value = default(TValue);
                return false;
            }

            value = node.Value;
            return true;
        }
    }

    /// <summary>
    /// Represents a node in a BsonTrie.
    /// </summary>
    public sealed class BsonTrieNode<TValue>
    {
        // private fields
        private readonly byte _keyByte;
        private string _elementName;
        private TValue _value;
        private BsonTrieNode<TValue> _onlyChild; // used when there is only one child
        private BsonTrieNode<TValue>[] _children; // used when there are two or more children

        // private fields set when node is frozen
        private bool _isFrozen;
        private byte _minKeyByte;
        private byte[] _keyByteIndexes; // maps key bytes into indexes into the _children list

        // constructors
        internal BsonTrieNode(byte keyByte)
        {
            _keyByte = keyByte;
        }

        /// <summary>
        /// Gets whether this node has a value.
        /// </summary>
        public bool HasValue
        {
            get
            {
                return _elementName != null;
            }
        }

        /// <summary>
        /// Gets the element name for this node.
        /// </summary>
        public string ElementName
        {
            get
            {
                if (_elementName == null)
                {
                    throw new InvalidOperationException("BsonTrieNode doesn't have a value.");
                }

                return _elementName;
            }
        }

        /// <summary>
        /// Gets the value for this node.
        /// </summary>
        public TValue Value
        {
            get
            {
                if (_elementName == null)
                {
                    throw new InvalidOperationException("BsonTrieNode doesn't have a value.");
                }

                return _value;
            }
        }

        // public methods
        /// <summary>
        /// Gets the child of this node for a given key byte.
        /// </summary>
        /// <param name="keyByte">The key byte.</param>
        /// <returns>The child node if it exists; otherwise, null.</returns>
        public BsonTrieNode<TValue> GetChild(byte keyByte)
        {
            if (_onlyChild != null)
            {
                // optimization for nodes that have only one child
                if (_onlyChild._keyByte == keyByte)
                {
                    return _onlyChild;
                }
            }
            else if (_children != null)
            {
                var index = (uint)((int)keyByte - _minKeyByte);
                // enable the .Net CLR to eliminate an array bounds check on _keyByteIndexes
                var keyByteIndexes = _keyByteIndexes;
                if (index < keyByteIndexes.Length)
                {
                    index = keyByteIndexes[index];
                    // enable the .Net CLR to eliminate an array bounds check on _children
                    var children = _children;
                    if (index < children.Length)
                    {
                        return children[index];
                    }
                }
            }
            return null;
        }

        // internal methods
        internal void AddChild(BsonTrieNode<TValue> child)
        {
            if (_isFrozen) { throw new InvalidOperationException("BsonTrieNode is frozen."); }
            if (_children != null)
            {
                if (_children[child._keyByte] != null)
                {
                    throw new ArgumentException("BsonTrieNode already contains a child with the same keyByte.");
                }

                _children[child._keyByte] = child;
            }
            else if (_onlyChild != null)
            {
                if (_onlyChild._keyByte == child._keyByte)
                {
                    throw new ArgumentException("BsonTrieNode already contains a child with the same keyByte.");
                }

                var children = new BsonTrieNode<TValue>[256];
                children[_onlyChild._keyByte] = _onlyChild;
                children[child._keyByte] = child;

                var keyByteIndexes = new byte[256];
                for (var i = 0; i < keyByteIndexes.Length; i++)
                {
                    keyByteIndexes[i] = (byte)i;
                }

                _keyByteIndexes = keyByteIndexes;
                _onlyChild = null;
                _children = children;
            }
            else
            {
                _onlyChild = child;
            }
        }

        internal void Freeze()
        {
            if (!_isFrozen)
            {
                if (_onlyChild != null)
                {
                    _onlyChild.Freeze();
                }
                else if (_children != null)
                {
                    var i = 0;

                    // _children is guaranteed to have at least one element that isn't null
                    for (; _children[i] == null; i++);

                    var minKeyByte = (byte)i;
                    var maxKeyByte = minKeyByte;

                    byte childIndex = 0;

                    for (i++; i < _children.Length; i++)
                    {
                        if (_children[i] != null)
                        {
                            maxKeyByte = (byte)i;
                            childIndex++;
                        }
                    }

                    var keyByteIndexes = new byte[(int)maxKeyByte - minKeyByte + 1];
                    for (i = 0; i < keyByteIndexes.Length; i++)
                    {
                        keyByteIndexes[i] = 255; // make sure unused entries can be identified
                    }

                    var children = new BsonTrieNode<TValue>[(int)childIndex + 1];
                    childIndex = 0;
                    for (i = 0; i < _children.Length; i++)
                    {
                        var child = _children[i];
                        if (child != null)
                        {
                            keyByteIndexes[child._keyByte - minKeyByte] = childIndex;
                            children[childIndex] = child;
                            child.Freeze();
                            childIndex++;
                        }
                    }

                    _minKeyByte = minKeyByte;
                    _keyByteIndexes = keyByteIndexes;
                    _children = children;
                }
                _isFrozen = true;
            }
        }

        internal void SetValue(string elementName, TValue value)
        {
            if (elementName == null)
            {
                throw new ArgumentNullException("elementName");
            }
            if (_elementName != null)
            {
                throw new InvalidOperationException("BsonTrieNode already has a value.");
            }
            if (_isFrozen) { throw new InvalidOperationException("BsonTrieNode is frozen."); }

            _elementName = elementName;
            _value = value;
        }
    }
}
