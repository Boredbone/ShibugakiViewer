using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Search
{
    /// <summary>
    /// 比較演算子
    /// </summary>
    public enum CompareMode
    {
        Less,
        LessEqual,
        Equal,
        GreatEqual,
        Great,
        NotEqual
    }

    /// <summary>
    /// 比較演算子のユーティリティ
    /// </summary>
    public static class CompareModeExtensions
    {
        /// <summary>
        /// 記号を取得
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static string ToSymbol(this CompareMode mode)
        {
            switch (mode)
            {
                case CompareMode.Less:
                    return "<";
                case CompareMode.LessEqual:
                    return "<=";
                case CompareMode.Equal:
                    return "==";
                case CompareMode.GreatEqual:
                    return ">=";
                case CompareMode.Great:
                    return ">";
                case CompareMode.NotEqual:
                    return "!=";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 演算子が等価(=)を含むかどうか
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static bool ContainsEqual(this CompareMode mode)
        {
            switch (mode)
            {
                case CompareMode.LessEqual:
                case CompareMode.Equal:
                case CompareMode.GreatEqual:
                    return true;
                default:
                    return false;
            }

        }
    }

}
