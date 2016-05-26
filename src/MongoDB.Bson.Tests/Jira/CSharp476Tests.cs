/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;
using System;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp476Tests
    {
        // make sure class maps are registered before tests run
        static CSharp476Tests()
        {
            TDelegate.RegisterClassMap();
            TExpressionCallingConstructor.RegisterClassMap();
            TExpressionCallingFactoryMethod.RegisterClassMap();
            TExpressionCallingArbitraryCode.RegisterClassMap();
        }

        public class TBsonConstructor
        {
            private int _chosen;
            private int _x;
            private int _y;

            [BsonConstructor]
            public TBsonConstructor()
            {
                _chosen = 0;
            }

            [BsonConstructor] // let convention find the matching properties
            public TBsonConstructor(int x)
            {
                _chosen = 1;
                _x = x;
            }

            [BsonConstructor] // let convention find the matching properties
            public TBsonConstructor(int x, int y)
            {
                _chosen = 2;
                _x = x;
                _y = y;
            }

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }
        }

        [Fact]
        public void TestTBsonConstructor()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TBsonConstructor>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TBsonConstructor>(json);
            Assert.Equal(2, r.Chosen); // passed default value to constructor
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TBsonConstructor>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TBsonFactoryMethod
        {
            internal int _chosen;
            internal int _x;
            internal int _y;

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }

            [BsonFactoryMethod]
            public static TBsonFactoryMethod FactoryMethod()
            {
                var instance = new TBsonFactoryMethod();
                instance._chosen = 0;
                return instance;
            }

            [BsonFactoryMethod("X")]
            public static TBsonFactoryMethod FactoryMethod(int x)
            {
                var instance = new TBsonFactoryMethod();
                instance._chosen = 1;
                instance._x = x;
                return instance;
            }

            [BsonFactoryMethod("X", "Y")]
            public static TBsonFactoryMethod FactoryMethod(int x, int y)
            {
                var instance = new TBsonFactoryMethod();
                instance._chosen = 2;
                instance._x = x;
                instance._y = y;
                return instance;
            }
        }

        [Fact]
        public void TestTBsonFactoryMethod()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TBsonFactoryMethod>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TBsonFactoryMethod>(json);
            Assert.Equal(2, r.Chosen); // passed default value to factory method
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TBsonFactoryMethod>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TDelegate
        {
            private int _chosen;
            private int _x;
            private int _y;

            public TDelegate()
            {
                _chosen = 0;
            }

            public TDelegate(int x)
            {
                _chosen = 1;
                _x = x;
            }

            public TDelegate(int x, int y)
            {
                _chosen = 2;
                _x = x;
                _y = y;
            }

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }

            public static void RegisterClassMap()
            {
                BsonClassMap.RegisterClassMap<TDelegate>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator((Func<TDelegate>)(() => new TDelegate()));
                    cm.MapCreator((Func<int, TDelegate>)((int x) => new TDelegate(x)), "X");
                    cm.MapCreator((Func<int, int, TDelegate>)((int x, int y) => new TDelegate(x, y)), "X", "Y");
                });
            }
        }

        [Fact]
        public void TestTDelegate()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TDelegate>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TDelegate>(json);
            Assert.Equal(2, r.Chosen); // passed default value to delegate
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TDelegate>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TExpressionCallingConstructor
        {
            private int _chosen;
            private int _x;
            private int _y;

            public TExpressionCallingConstructor()
            {
                _chosen = 0;
            }

            public TExpressionCallingConstructor(int x)
            {
                _chosen = 1;
                _x = x;
            }

            public TExpressionCallingConstructor(int x, int y)
            {
                _chosen = 2;
                _x = x;
                _y = y;
            }

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }

            public static void RegisterClassMap()
            {
                BsonClassMap.RegisterClassMap<TExpressionCallingConstructor>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(c => new TExpressionCallingConstructor());
                    cm.MapCreator(c => new TExpressionCallingConstructor(c.X));
                    cm.MapCreator(c => new TExpressionCallingConstructor(c.X, c.Y));
                });
            }
        }

        [Fact]
        public void TestTExpressionCallingConstructor()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TExpressionCallingConstructor>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TExpressionCallingConstructor>(json);
            Assert.Equal(2, r.Chosen); // passed default value to delegate
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TExpressionCallingConstructor>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TExpressionCallingFactoryMethod
        {
            internal int _chosen;
            internal int _x;
            internal int _y;

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }

            public static TExpressionCallingFactoryMethod FactoryMethod()
            {
                var instance = new TExpressionCallingFactoryMethod();
                instance._chosen = 0;
                return instance;
            }

            public static TExpressionCallingFactoryMethod FactoryMethod(int x)
            {
                var instance = new TExpressionCallingFactoryMethod();
                instance._chosen = 1;
                instance._x = x;
                return instance;
            }

            public static TExpressionCallingFactoryMethod FactoryMethod(int x, int y)
            {
                var instance = new TExpressionCallingFactoryMethod();
                instance._chosen = 2;
                instance._x = x;
                instance._y = y;
                return instance;
            }

            public static void RegisterClassMap()
            {
                BsonClassMap.RegisterClassMap<TExpressionCallingFactoryMethod>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(c => TExpressionCallingFactoryMethod.FactoryMethod());
                    cm.MapCreator(c => TExpressionCallingFactoryMethod.FactoryMethod(c.X));
                    cm.MapCreator(c => TExpressionCallingFactoryMethod.FactoryMethod(c.X, c.Y));
                });
            }
        }

        [Fact]
        public void TestTExpressionCallingFactoryMethod()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TExpressionCallingFactoryMethod>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TExpressionCallingFactoryMethod>(json);
            Assert.Equal(2, r.Chosen); // passed default value to factory method
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TExpressionCallingFactoryMethod>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TExpressionCallingArbitraryCode
        {
            internal int _chosen;
            internal int _x;
            internal int _y;

            public TExpressionCallingArbitraryCode()
            {
                _chosen = 0;
            }

            public TExpressionCallingArbitraryCode(int x)
            {
                _chosen = 1;
                _x = x;
            }

            public TExpressionCallingArbitraryCode(int x, int y)
            {
                _chosen = 2;
                _x = x;
                _y = y;
            }

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }

            public static TExpressionCallingArbitraryCode FactoryMethod(int x)
            {
                var instance = new TExpressionCallingArbitraryCode();
                instance._chosen = -1;
                instance._x = x;
                return instance;
            }

            public static TExpressionCallingArbitraryCode FactoryMethod(int x, int y)
            {
                var instance = new TExpressionCallingArbitraryCode();
                instance._chosen = -2;
                instance._x = x;
                instance._y = y;
                return instance;
            }

            public static void RegisterClassMap()
            {
                BsonClassMap.RegisterClassMap<TExpressionCallingArbitraryCode>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(c => (c.X >= 0) ? new TExpressionCallingArbitraryCode(c.X) : TExpressionCallingArbitraryCode.FactoryMethod(c.X));
                    cm.MapCreator(c => (c.X >= 0) ? new TExpressionCallingArbitraryCode(c.X, c.Y) : TExpressionCallingArbitraryCode.FactoryMethod(c.X, c.Y));
                });
            }
        }

        [Fact]
        public void TestTExpressionCallingArbitraryCode()
        {
            var json = "{ X : 1 }";
            var r = BsonSerializer.Deserialize<TExpressionCallingArbitraryCode>(json);
            Assert.Equal(2, r.Chosen); // passed default value to factory method
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TExpressionCallingArbitraryCode>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);

            json = "{ X : -1 }";
            r = BsonSerializer.Deserialize<TExpressionCallingArbitraryCode>(json);
            Assert.Equal(-2, r.Chosen); // passed default value to factory method
            Assert.Equal(-1, r.X);
            Assert.Equal(2, r.Y);

            json = "{ X : -1, Y : 3 }";
            r = BsonSerializer.Deserialize<TExpressionCallingArbitraryCode>(json);
            Assert.Equal(-2, r.Chosen);
            Assert.Equal(-1, r.X);
            Assert.Equal(3, r.Y);
        }

        public class TBaseClass
        {
            protected int _chosen;
            private int _x;

            [BsonConstructor]
            public TBaseClass()
            {
                _chosen = 0;
            }

            [BsonConstructor("X")]
            public TBaseClass(int x)
            {
                _chosen = 1;
                _x = x;
            }

            [BsonIgnore]
            public int Chosen { get { return _chosen; } }
            [BsonElement]
            public int X { get { return _x; } }
        }

        public class TDerivedClass : TBaseClass
        {
            private int _y;
            private int _z;

            [BsonConstructor]
            public TDerivedClass() : base()
            {
            }

            [BsonConstructor("X")] // note: "X" is defined in the base class
            public TDerivedClass(int x) : base(x)
            {
            }

            [BsonConstructor] // let convention find the matching properties
            public TDerivedClass(int x, int y) : base(x)
            {
                _chosen = 2;
                _y = y;
            }

            [BsonElement]
            [BsonDefaultValue(2)]
            public int Y { get { return _y; } }
            [BsonDefaultValue(3)]
            public int Z { get { return _z; } set { _z = value; } }
        }

        [Fact]
        public void TestTDerivedClass()
        {
            var json = "{ }";
            var r = BsonSerializer.Deserialize<TDerivedClass>(json);
            Assert.Equal(0, r.Chosen);
            Assert.Equal(0, r.X);
            Assert.Equal(0, r.Y); // note: unable to apply default value
            Assert.Equal(3, r.Z);

            json = "{ X : 1 }";
            r = BsonSerializer.Deserialize<TDerivedClass>(json);
            Assert.Equal(2, r.Chosen); // passed default value to constructor
            Assert.Equal(1, r.X);
            Assert.Equal(2, r.Y);
            Assert.Equal(3, r.Z);

            json = "{ X : 1, Y : 3 }";
            r = BsonSerializer.Deserialize<TDerivedClass>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
            Assert.Equal(3, r.Z);

            json = "{ X : 1, Y : 3, Z : 4 }";
            r = BsonSerializer.Deserialize<TDerivedClass>(json);
            Assert.Equal(2, r.Chosen);
            Assert.Equal(1, r.X);
            Assert.Equal(3, r.Y);
            Assert.Equal(4, r.Z);
        }
    }
}
