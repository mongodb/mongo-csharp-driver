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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4079Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Positional_operator_with_negative_one_array_index_should_work_or_throw_depending_on_Linq_provider()
        {
            var collection = GetCollection<C>();

            var negativeOne = -1;
            var update = Builders<C>.Update.Set(x => x.A[negativeOne], 0); // using -1 constant is a compile time error

            var exception = Record.Exception(() => Render(update));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        [Fact]
        public void Positional_operator_with_negative_one_ElementAt_should_work_or_throw_depending_on_Linq_provider()
        {
            var collection = GetCollection<C>();

            var update = Builders<C>.Update.Set(x => x.A.ElementAt(-1), 0);

            var exception = Record.Exception(() => Render(update));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        // the following examples are from the server documentation:
        // https://www.mongodb.com/docs/manual/reference/operator/update/positional/

        [Fact]
        public void Positional_update_operator_update_values_in_an_array_example()
        {
            var update = Builders<Student1>.Update.Set(s => s.Grades.FirstMatchingElement(), 82);

            var rendered = Render(update);
            rendered.Should().Be("{ $set : { 'Grades.$' : 82 } }");
        }

        [Fact]
        public void Positional_update_operator_update_documents_in_an_array_example()
        {
            var update = Builders<Student2>.Update.Set(s => s.Grades.FirstMatchingElement().Std, 6);

            var rendered = Render(update);
            rendered.Should().Be("{ $set : { 'Grades.$.Std' : 6 } }");
        }

        // the following examples are from the server documentation:
        // https://www.mongodb.com/docs/manual/reference/operator/update/positional-all/

        [Fact]
        public void All_positional_update_operator_update_all_elements_in_an_array_example()
        {
            var update = Builders<Student1>.Update.Inc(s => s.Grades.AllElements(), 10) ;

            var rendered = Render(update);
            rendered.Should().Be("{ $inc : { 'Grades.$[]' : 10 } }");
        }

        [Fact]
        public void All_positional_update_operator_update_all_documents_in_an_array_example()
        {
            var update = Builders<Student2>.Update.Inc(s => s.Grades.AllElements().Std, -2);

            var rendered = Render(update);
            rendered.Should().Be("{ $inc : { 'Grades.$[].Std' : -2 } }");
        }

        [Fact]
        public void All_positional_update_operator_update_nested_arrays_in_conjunction_with_filtered_position_operator_example()
        {
            var update = Builders<Student4>.Update.Inc(s => s.Grades.AllElements().Questions.AllMatchingElements("score"), 2);

            var rendered = Render(update);
            rendered.Should().Be("{ $inc : { 'Grades.$[].Questions.$[score]' : 2 } }");
        }

        // the following examples are from the server documentation:
        // https://www.mongodb.com/docs/manual/reference/operator/update/positional-filtered/

        [Fact]
        public void Filtered_positional_update_operator_update_all_array_elements_that_match_array_filter_example()
        {
            var update = Builders<Student1>.Update.Set(s => s.Grades.AllMatchingElements("element"), 100);

            var rendered = Render(update);
            rendered.Should().Be("{ $set : { 'Grades.$[element]' : 100 } }");
        }

        [Fact]
        public void Filtered_positional_update_operator_update_all_documents_that_match_array_filter_in_an_array_example()
        {
            var update = Builders<Student2>.Update.Set(s => s.Grades.AllMatchingElements("elem").Mean, 100);

            var rendered = Render(update);
            rendered.Should().Be("{ $set : { 'Grades.$[elem].Mean' : 100 } }");
        }

        [Fact]
        public void Filtered_positional_update_operator_update_nested_arrays_example()
        {
            var update = Builders<Student4>.Update.Inc(s => s.Grades.AllMatchingElements("t").Questions.AllMatchingElements("score"), 2);

            var rendered = Render(update);
            rendered.Should().Be("{ $inc : { 'Grades.$[t].Questions.$[score]' : 2 } }");
        }

        private BsonValue Render<TDocument>(UpdateDefinition<TDocument> update)
        {
            var documentSerializer = BsonSerializer.LookupSerializer<TDocument>();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return update.Render(new(documentSerializer, serializerRegistry));
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A {  get; set; }
        }

        private class Student1
        {
            public int[] Grades { get; set; }
        }

        private class Student2
        {
            public Grade2[] Grades { get; set; }
        }

        private class Grade2
        {
            public int Grade { get ; set; }
            public int Mean { get; set; }
            public int Std { get; set; }
        }

        private class Student4
        {
            public Grade4[] Grades { get; set; }
        }

        private class Grade4
        {
            public string Type { get; set; }
            public int[] Questions { get; set; }
        }
    }
}
