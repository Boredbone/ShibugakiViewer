using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.File;
using ImageLibrary.Search;

namespace ShibugakiViewer.Models
{
    public class ViewState : IEquatable<ViewState>
    {
        public SearchInformation Search { get; set; }
        public string GroupKey { get; set; }
        public long CatalogIndex { get; set; }
        public long ViewerIndex { get; set; }
        public PageType Type { get; set; }

        public ViewState Clone()
        {
            return new ViewState()
            {
                Search = this.Search,
                GroupKey = this.GroupKey,
                CatalogIndex = this.CatalogIndex,
                ViewerIndex = this.ViewerIndex,
                Type = this.Type,
            };

        }

        public bool Equals(ViewState other)
        {
            if (other == null)
            {
                return false;
            }

            if((this.GroupKey==null && other.GroupKey!=null)
                || (this.GroupKey != null && other.GroupKey == null)
                || (this.Search == null && other.Search != null)
                || (this.Search != null && other.Search == null))
            {
                return false;
            }


            return this.CatalogIndex == other.CatalogIndex
                && this.ViewerIndex == other.ViewerIndex
                && this.Type == other.Type
                && ((this.GroupKey == other.GroupKey) || this.GroupKey.Equals(other.GroupKey))
                && ((this.Search == other.Search) || this.Search.HasSameSearch(other.Search));
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ViewState))
            {
                return false;
            }

            return this.Equals((ViewState)obj);
        }

        public override int GetHashCode()
        {
            return this.Search.GetHashCode()
                ^ this.GroupKey.GetHashCode()
                ^ (int)this.ViewerIndex
                ^ (int)this.CatalogIndex
                ^ (int)this.Type;
        }
    }

    /// <summary>
    /// ページの種類
    /// </summary>
    public enum PageType
    {
        None,
        Search,
        Catalog,
        Viewer,
    }
}
