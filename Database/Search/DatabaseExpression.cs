using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;

namespace Database.Search
{
    public interface IDatabaseExpression
    {
        string GetSql();
    }

    public class DatabaseExpression : IDatabaseExpression
    {
        private readonly string column;
        private readonly string sqlOperator;
        private readonly DatabaseReference reference;

        private DatabaseExpression(string column, string sqlOperator, DatabaseReference reference)
        {
            this.column = column;
            this.sqlOperator = sqlOperator;
            this.reference = reference;
        }

        private DatabaseExpression(string column, DatabaseReference reference)
        {
            this.column = column;
            this.sqlOperator = null;
            this.reference = reference;
        }

        public string GetSql()
        {
            if (this.sqlOperator == null)
            {
                return $"({column} {reference})";
            }
            return $"({column} {sqlOperator} {reference})";
        }
        public override string ToString() => this.GetSql();


        private class DatabaseNotExpression : IDatabaseExpression
        {
            private readonly DatabaseExpression expression;

            public DatabaseNotExpression(DatabaseExpression expression)
            {
                if (expression.sqlOperator != null)
                {
                    throw new ArgumentException("expression has operator");
                }
                this.expression = expression;
            }

            public string GetSql()
                => $"({this.expression.column} NOT {this.expression.reference})";
            public override string ToString() => this.GetSql();
        }


        private class ComplexExpression : IDatabaseExpression
        {
            private readonly IDatabaseExpression[] children;
            private readonly bool isOr;

            public ComplexExpression(bool isOr, IDatabaseExpression[] children)
            {
                this.isOr = isOr;
                this.children = children;
            }


            public string GetSql()
            {
                if (this.isOr)
                {
                    return $"({children.Select(x => x.GetSql()).Join(" OR ")})";
                }
                else
                {
                    return $"({children.Select(x => x.GetSql()).Join(" AND ")})";
                }
            }
            public override string ToString() => this.GetSql();
        }

        public static IDatabaseExpression Not(DatabaseExpression expression)
            => new DatabaseNotExpression(expression);


        public static IDatabaseExpression And(params IDatabaseExpression[] sqls)
            => new ComplexExpression(false, sqls);

        public static IDatabaseExpression Or(params IDatabaseExpression[] sqls)
            => new ComplexExpression(true, sqls);

        public static DatabaseExpression Is(string column, DatabaseReference reference)
            => new DatabaseExpression(column, reference);

        public static IDatabaseExpression IsNot(string column, DatabaseReference reference)
            => Not(new DatabaseExpression(column, reference));

        public static IDatabaseExpression Compare(string column, CompareMode compare, DatabaseReference reference)
            => new DatabaseExpression(column, compare.ToSymbol(), reference);

        public static IDatabaseExpression AreEqual(string column, DatabaseReference reference)
            => Compare(column, CompareMode.Equal, reference);

        public static IDatabaseExpression AreNotEqual(string column, DatabaseReference reference)
            => Compare(column, CompareMode.NotEqual, reference);

        public static IDatabaseExpression AreEqualWithEscape(string column, string reference)
            => Compare(column, CompareMode.Equal, DatabaseReference.ToEqualsString(reference));


        public static IDatabaseExpression IsTrue(string column)
            => AreNotEqual(column, new DatabaseReference("0"));

        public static IDatabaseExpression IsFalse(string column)
            => AreEqual(column, new DatabaseReference("0"));

        public static DatabaseExpression IsNull(string column)
            => Is($"{column} IS", new DatabaseReference("NULL"));

        public static IDatabaseExpression IsNotNull(string column)
            => Not(IsNull(column));


        public static IDatabaseExpression In(string column, string array)
            => new DatabaseExpression(column, "IN", new DatabaseReference(array));
    }

}
