using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal static class MongoExpressionExtensions
    {
        public static bool HasSelectAllField(this IEnumerable<FieldDeclaration> fields)
        {
            return fields == null || fields.Any(f => f.Name == "*");
        }

        public static SelectExpression AddField(this SelectExpression select, FieldDeclaration field)
        {
            var fields = new List<FieldDeclaration>(select.Fields) {field};
            return select.SetFields(fields);
        }

        public static string GetAvailableFieldName(this SelectExpression select, string baseName)
        {
            var name = baseName;
            var n = 0;
            while(!IsUniqueName(select, name))
                name = baseName + (n++);
            return name;
        }

        public static SelectExpression RemoveField(this SelectExpression select, FieldDeclaration field)
        {
            var fields = new List<FieldDeclaration>(select.Fields);
            fields.Remove(field);
            return select.SetFields(fields);
        }

        public static SelectExpression SetFields(this SelectExpression select, IEnumerable<FieldDeclaration> fields)
        {
            return new SelectExpression(select.Alias,
                fields.OrderBy(f => f.Name),
                select.From,
                select.Where,
                select.OrderBy,
                select.GroupBy,
                select.IsDistinct,
                select.Skip,
                select.Take);
        }

        public static SelectExpression SetWhere(this SelectExpression select, Expression where)
        {
            return new SelectExpression(select.Alias,
                select.Fields,
                select.From,
                where,
                select.OrderBy,
                select.GroupBy,
                select.IsDistinct,
                select.Skip,
                select.Take);
        }

        private static bool IsUniqueName(SelectExpression select, string name)
        {
            return select.Fields.All(field => field.Name != name);
        }
    }
}