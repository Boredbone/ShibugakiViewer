using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;

namespace Database.Search
{

    public struct DatabaseReference
    {
        private readonly string reference;

        public DatabaseReference(string reference)
        {
            this.reference = reference;
        }

        public override string ToString() => this.reference;

        private static string ToEscapedString(object obj)
            => ToEscapedString(obj.ToString());

        private static string ToEscapedString(string obj)
            => obj.Replace("'", "''");

        public static DatabaseReference ToLowerEqualsString(object obj)
            => new DatabaseReference($"'{ToEscapedString(obj).ToLower()}'");

        public static DatabaseReference ToEqualsString(object obj)
            => new DatabaseReference($"'{ToEscapedString(obj)}'");
        public static DatabaseReference ToEqualsString(string obj)
            => new DatabaseReference($"'{ToEscapedString(obj)}'");

        private static string ToGlobReference(string reference)
            => ToEscapedString(reference.ToLower().Replace("[", "[[]"));

        public static DatabaseReference Match(string reference)
            => new DatabaseReference($"GLOB '{ToGlobReference(reference)}'");


        public static DatabaseReference StartsWith(string reference)
            => new DatabaseReference($"GLOB '{ToGlobReference(reference)}*'");


        public static DatabaseReference EndsWith(string reference)
            => new DatabaseReference($"GLOB '*{ToGlobReference(reference)}'");

        public static DatabaseReference Contains(string reference)
            => new DatabaseReference($"GLOB '*{ToGlobReference(reference)}*'");


        public static DatabaseReference DateOffsetReference(DateTimeOffset dateTime)
            => new DatabaseReference(UnixTime.FromDateTime(dateTime.ToLocalTime().Date).ToString());
        public static DatabaseReference DateOffsetReference(DateTimeOffset dateTime, TimeSpan offset)
            => new DatabaseReference(UnixTime.FromDateTime(dateTime.ToOffset(offset).ToDate()).ToString());

        public static DatabaseReference DateTimeOffsetReference(DateTimeOffset dateTime)
            => new DatabaseReference(UnixTime.FromDateTime(dateTime).ToString());
    }
}
