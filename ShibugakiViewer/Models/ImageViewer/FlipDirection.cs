using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.File;

namespace ShibugakiViewer.Models.ImageViewer
{
    public enum FlipDirection
    {
        Default = 0,
        LeftToRight = 1,
        RightToLeft = 2,
    }

    public static class RecordExtensions
    {
        public static FlipDirection GetFlipDirection(this Record record)
        {
            return (FlipDirection)record.FlipDirection;
        }
    }
}
