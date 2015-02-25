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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    /// <summary>
    /// This guy is going to replace expressions like Serialization("G.D") with Serialization("D").
    /// </summary>
    internal class PrefixedFieldRenamer : MongoExpressionVisitor
    {
        public static Expression Rename(Expression node, string prefix)
        {
            var renamer = new PrefixedFieldRenamer(prefix);
            return renamer.Visit(node);
        }

        private string _prefix;

        private PrefixedFieldRenamer(string prefix)
        {
            _prefix = prefix;
        }

        protected override Expression VisitSerialization(SerializationExpression node)
        {
            if (node.SerializationInfo.ElementName.StartsWith(_prefix))
            {
                var name = node.SerializationInfo.ElementName;
                if(name == _prefix)
                {
                    name = "";
                }
                else
                {
                    name = name.Remove(0, _prefix.Length + 1);
                }
                return new SerializationExpression(
                    node.Expression,
                    node.SerializationInfo.WithNewName(name));
            }

            return base.VisitSerialization(node);
        }
    }
}
