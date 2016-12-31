using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;

namespace Database.Search
{
    /// <summary>
    /// Define informations to search a database with a property of the class
    /// </summary>
    public class PropertySearch
    {
        private readonly string selector;
        private readonly Func<object, DatabaseReference> converter;
        private readonly Func<object, CompareMode, IDatabaseExpression> fullConverter;

        public bool IsComparable { get; }

        public PropertySearch(string selector, bool isComparable)
        {
            this.selector = selector;
            this.IsComparable = isComparable;
        }
        public PropertySearch(string selector, bool isComparable, Func<object, DatabaseReference> converter)
            : this(selector, isComparable)
        {
            this.converter = converter;
        }
        public PropertySearch(bool isComparable, Func<object, CompareMode, IDatabaseExpression> fullConverter)
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
        public IDatabaseExpression ToSql(CompareMode mode, object reference)
        {
            if (this.fullConverter != null)
            {
                return this.fullConverter(reference, mode);
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
