/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class CommandMessageSectionEqualityComparer : IEqualityComparer<CommandMessageSection>
    {
        public static readonly CommandMessageSectionEqualityComparer Instance = new CommandMessageSectionEqualityComparer();

        public bool Equals(CommandMessageSection x, CommandMessageSection y)
        {
            if (x == y)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x.PayloadType != y.PayloadType)
            {
                return false;
            }
            if (x.PayloadType == PayloadType.Type0)
            {
                return Type0SectionEquals((Type0CommandMessageSection)x, (Type0CommandMessageSection)y);
            }
            if (x.PayloadType == PayloadType.Type1)
            {
                return Type1SectionEquals((Type1CommandMessageSection)x, (Type1CommandMessageSection)y);
            }
            return false;
        }

        public int GetHashCode(CommandMessageSection obj)
        {
            throw new NotImplementedException();
        }

        private bool Type0SectionEquals(Type0CommandMessageSection x, Type0CommandMessageSection y)
        {
            return x.Document.Equals(y.Document);
        }

        private bool Type1SectionEquals(Type1CommandMessageSection x, Type1CommandMessageSection y)
        {
            return
                x.Identifier == y.Identifier &&
                BatchEquals(x.Documents, y.Documents);
        }

        private bool BatchEquals(IBatchableSource<object> x, IBatchableSource<object> y)
        {
            return
                x.Count == y.Count &&
                x.Items.Skip(x.Offset).Take(x.Count).SequenceEqual(y.Items.Skip(y.Offset).Take(y.Count));
        }
    }
}
