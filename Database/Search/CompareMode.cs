using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Search
{
    /// <summary>
    /// Operators to compare
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
    /// Utilities for comparison operators
    /// </summary>
    public static class CompareModeExtensions
    {
        /// <summary>
        /// Get symbol
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
        /// Whether operator contains equality(=)
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
