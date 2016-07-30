using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;

namespace Database.Search
{
    /// <summary>
    /// クラスのプロパティでデータベース内を検索するための情報を定義する
    /// </summary>
    public class PropertySearch
    {
        private readonly string selector;
        private readonly Func<object, string> converter;
        private readonly Func<object, CompareMode, string> fullConverter;

        public bool IsComparable { get; }

        public PropertySearch(string selector, bool isComparable)
        {
            this.selector = selector;
            this.IsComparable = isComparable;
        }
        public PropertySearch(string selector, bool isComparable, Func<object, string> converter)
            : this(selector, isComparable)
        {
            this.converter = converter;
        }
        public PropertySearch(bool isComparable, Func<object, CompareMode, string> fullConverter)
            : this(null, isComparable)
        {
            this.fullConverter = fullConverter;
        }

        /// <summary>
        /// 検索用のSQL文字列を取得
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public string ToSql(CompareMode mode, object reference)
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
                        return $"({DatabaseFunction.IsNull(this.selector)})";

                    case CompareMode.NotEqual:
                    case CompareMode.Less:
                    case CompareMode.Great:
                        return $"({DatabaseFunction.IsNotNull(this.selector)})";
                }

                throw new ArgumentException();
            }


            if (this.converter == null)
            {
                if (this.IsComparable)
                {
                    return $"({this.selector} {mode.ToSymbol()} {reference})";
                }
                switch (mode)
                {
                    case CompareMode.Equal:
                    case CompareMode.LessEqual:
                    case CompareMode.GreatEqual:
                        return $"({DatabaseFunction.AreEqual(this.selector,reference)})";

                    case CompareMode.NotEqual:
                    case CompareMode.Less:
                    case CompareMode.Great:
                        return $"({this.selector} != {reference})";
                }
                throw new ArgumentException();
            }

            var converted = this.converter(reference);

            if (this.IsComparable)
            {
                return $"({this.selector} {mode.ToSymbol()} {converted})";
            }
            switch (mode)
            {
                case CompareMode.Equal:
                case CompareMode.LessEqual:
                case CompareMode.GreatEqual:
                    return $"({this.selector} {converted})";

                case CompareMode.NotEqual:
                case CompareMode.Less:
                case CompareMode.Great:
                    return $"({this.selector} NOT {converted})";
            }
            throw new ArgumentException();
        }
    }
}
