using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.XamlTools;
using Database.Search;
using ImageLibrary.SearchProperty;

namespace ShibugakiViewer.Models
{

    public static class CompareModeManager
    {
        private static Dictionary<CompareMode, string> symbols
            = new Dictionary<CompareMode, string>(){
                {CompareMode.Less,"<"},
                {CompareMode.LessEqual,"<="},
                {CompareMode.Equal,"=="},
                {CompareMode.GreatEqual,">="},
                {CompareMode.Great,">"},
                {CompareMode.NotEqual,"!="},
            };
        private static Dictionary<CompareMode, string> Labels;
        public static void InitializeLabels(Func<string, string> GetResource)
        {
            if (Labels != null)
            {
                return;
            }
            Labels = new Dictionary<CompareMode, string>(){
                {CompareMode.Less,GetResource("LessThan")},
                {CompareMode.LessEqual,GetResource("LessEqual")},
                {CompareMode.Equal,GetResource("EqualsTo")},
                {CompareMode.GreatEqual,GetResource("GreaterEqual")},
                {CompareMode.Great,GetResource("GreaterThan")},
                {CompareMode.NotEqual,GetResource("NotEqualsTo")},
            };
        }
        public static string GetLabel(this CompareMode property)
        {
            string result;
            if (Labels.TryGetValue(property, out result))
            {
                return result;
            }
            return "";
        }
        public static string GetSymbol(this CompareMode property)
        {
            string result;
            if (symbols.TryGetValue(property, out result))
            {
                return result;
            }
            return "";
        }


        public static string GetCompareLabel(this FileProperty property, CompareMode mode)
        {
            if (property.IsComperable())
            {
                return mode.GetLabel();
            }
            else
            {
                return property.GetEqualityLabel(!mode.ContainsEqual());
            }
        }
    }
}
