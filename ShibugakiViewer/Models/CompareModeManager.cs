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
        private static Dictionary<CompareMode, ResourceString> Labels;
        public static void InitializeLabels(Func<string, string> getString)
        {
            if (Labels != null)
            {
                return;
            }
            Labels = new Dictionary<CompareMode, ResourceString>(){
                {CompareMode.Less,new ResourceString("LessThan",getString)},
                {CompareMode.LessEqual,new ResourceString("LessEqual",getString)},
                {CompareMode.Equal,new ResourceString("EqualsTo",getString)},
                {CompareMode.GreatEqual,new ResourceString("GreaterEqual",getString)},
                {CompareMode.Great,new ResourceString("GreaterThan",getString)},
                {CompareMode.NotEqual,new ResourceString("NotEqualsTo",getString)},
            };
        }
        public static string GetLabel(this CompareMode property)
        {
            ResourceString result;
            if (Labels.TryGetValue(property, out result))
            {
                return result.Value;
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
