using Database.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Search
{
    /// <summary>
    /// Define informations to search a database with a property of the class
    /// </summary>
    public class SearchSql
    {
        private readonly string? selector;
        private readonly Func<SearchReferences, DatabaseReference>? converter;
        private readonly Func<SearchReferences, CompareMode, IDatabaseExpression>? fullConverter;

        public bool IsComparable { get; }

        public SearchSql(string? selector, bool isComparable)
        {
            this.selector = selector;
            this.IsComparable = isComparable;
        }
        public SearchSql(string? selector, bool isComparable,
            Func<SearchReferences, DatabaseReference> converter)
            : this(selector, isComparable)
        {
            this.converter = converter;
        }
        public SearchSql(bool isComparable, 
            Func<SearchReferences, CompareMode, IDatabaseExpression> fullConverter)
            : this(null, isComparable)
        {
            this.fullConverter = fullConverter;
        }

        /// <summary>
        /// Get SQL string to search
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public IDatabaseExpression ToSql(CompareMode mode, SearchReferences reference)
        {
            if (this.fullConverter != null)
            {
                return this.fullConverter(reference, mode);
            }
            if (this.selector == null)
            {
                throw new ArgumentException();
            }

            if (reference == null)
            {
                switch (mode)
                {
                    case CompareMode.Equal:
                    case CompareMode.LessEqual:
                    case CompareMode.GreatEqual:
                        return DatabaseExpression.IsNull(this.selector);

                    case CompareMode.NotEqual:
                    case CompareMode.Less:
                    case CompareMode.Great:
                        return DatabaseExpression.IsNotNull(this.selector);
                }

                throw new ArgumentException();
            }


            if (this.converter == null)
            {
                if (this.IsComparable)
                {
                    return DatabaseExpression.Compare
                        (this.selector, mode, new DatabaseReference(reference.ToString()));
                }
                switch (mode)
                {
                    case CompareMode.Equal:
                    case CompareMode.LessEqual:
                    case CompareMode.GreatEqual:
                        return DatabaseExpression.AreEqual
                            (this.selector, new DatabaseReference(reference.ToString()));

                    case CompareMode.NotEqual:
                    case CompareMode.Less:
                    case CompareMode.Great:
                        return DatabaseExpression.AreNotEqual
                            (this.selector, new DatabaseReference(reference.ToString()));
                }
                throw new ArgumentException();
            }

            var converted = this.converter(reference);

            if (this.IsComparable)
            {
                return DatabaseExpression.Compare(this.selector, mode, converted);
            }
            switch (mode)
            {
                case CompareMode.Equal:
                case CompareMode.LessEqual:
                case CompareMode.GreatEqual:
                    return DatabaseExpression.Is(this.selector, converted);

                case CompareMode.NotEqual:
                case CompareMode.Less:
                case CompareMode.Great:
                    return DatabaseExpression.IsNot(this.selector, converted);
            }
            throw new ArgumentException();
        }
    }
}
