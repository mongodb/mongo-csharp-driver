/* Copyright 2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapSetOrderTests
    {
        [Fact]
        public void SetOrder_not_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Id", "X", "Y");
        }

        [Fact]
        public void SetOrder_Id_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Id", "X", "Y");
        }

        [Fact]
        public void SetOrder_X_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("X").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("X", "Id", "Y");
        }

        [Fact]
        public void SetOrder_Y_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Y").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Y", "Id", "X");
        }

        [Fact]
        public void SetOrder_Id_1_X_2_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(1);
            cm.GetMemberMap("X").SetOrder(2);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Id", "X", "Y");
        }

        [Fact]
        public void SetOrder_Id_1_Y_2_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(1);
            cm.GetMemberMap("Y").SetOrder(2);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Id", "Y", "X");
        }

        [Fact]
        public void SetOrder_Id_2_X_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(2);
            cm.GetMemberMap("X").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("X", "Id", "Y");
        }

        [Fact]
        public void SetOrder_Id_2_Y_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(2);
            cm.GetMemberMap("Y").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Y", "Id", "X");
        }

        [Fact]
        public void SetOrder_X_2_Y_1_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("X").SetOrder(2);
            cm.GetMemberMap("Y").SetOrder(1);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Y", "X", "Id");
        }

        [Fact]
        public void SetOrder_Id_1_X_3_Y_2_called()
        {
            var cm = new BsonClassMap<C>();
            cm.AutoMap();
            cm.GetMemberMap("Id").SetOrder(1);
            cm.GetMemberMap("X").SetOrder(3);
            cm.GetMemberMap("Y").SetOrder(2);
            cm.Freeze();

            cm.AllMemberMaps.Select(m => m.MemberName).Should().Equal("Id", "Y", "X");
        }

        // nested types
        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
