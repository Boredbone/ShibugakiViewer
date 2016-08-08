using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ShibugakiViewer.Models
{

    [DataContract]
    public class ApplicationSettings
    {
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public int ThumbNailSize { get; set; }
        [DataMember]
        public bool IsFlipAnimationEnabled { get; set; }
        [DataMember]
        public bool IsViewerPageTopBarFixed { get; set; }
        [DataMember]
        public bool IsViewerPageLeftBarFixed { get; set; }
        [DataMember]
        public bool LastSearchedFavorite { get; set; }

        [DataMember]
        public bool IsFlipReversed { get; set; }
        [DataMember]
        public bool IsOpenNavigationWithSingleTapEnabled { get; set; }
        [DataMember]
        public bool IsCmsEnabled { get; set; }
        [DataMember]
        public bool IsGifAnimationDisabled { get; set; }
        [DataMember]
        public bool UseExtendedMouseButtonsToSwitchImage { get; set; }

        [DataMember]
        public bool RefreshLibraryOnLaunched { get; set; }
        [DataMember]
        public bool IsLibraryRefreshStatusVisible { get; set; }
        [DataMember]
        public bool IsFolderUpdatedNotificationVisible { get; set; }

        [DataMember]
        public int CursorKeyBind { get; set; }

        [DataMember]
        public bool IsProfessionalFolderSettingEnabled { get; set; }

        [DataMember]
        public int SlideshowAnimationTimeMillisec { get; set; }
        [DataMember]
        public int SlideshowFlipTimeMillisec { get; set; }
        [DataMember]
        public bool IsSlideshowResizingAlways { get; set; }
        [DataMember]
        public bool IsSlideshowResizeToFill { get; set; }
        [DataMember]
        public bool IsSlideshowRandom { get; set; }

        [DataMember]
        public bool IsSlideshowFullScreen { get; set; }

        [DataMember]
        public bool IsAutoInformationPaneDisabled { get; set; }

        [DataMember]
        public uint BackgroundColor { get; set; }
        [DataMember]
        public bool IsDarkTheme { get; set; }

        public ApplicationSettings()
        {
            this.IsOpenNavigationWithSingleTapEnabled = true;
            this.UseExtendedMouseButtonsToSwitchImage = true;
            this.IsViewerPageLeftBarFixed = true;
            this.IsViewerPageTopBarFixed = true;
            this.ThumbNailSize = 200;
            this.RefreshLibraryOnLaunched = false;
            this.IsLibraryRefreshStatusVisible = true;
            this.IsProfessionalFolderSettingEnabled = false;
            this.IsCmsEnabled = false;
            this.IsGifAnimationDisabled = false;

            this.CursorKeyBind = 0;

            this.SlideshowAnimationTimeMillisec = 300;
            this.SlideshowFlipTimeMillisec = 5000;
            this.IsSlideshowResizingAlways = false;
            this.IsSlideshowResizeToFill = false;
            this.IsSlideshowRandom = false;


            this.IsSlideshowFullScreen = true;
            this.IsAutoInformationPaneDisabled = false;

            this.BackgroundColor = ~(uint)0;
            this.IsDarkTheme = false;
        }
    }
}
